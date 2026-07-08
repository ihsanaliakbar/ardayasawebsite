using Ardayasa.Application.Bookings;
using Ardayasa.Application.Common;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Application.Scheduling;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Persistence;
using Ardayasa.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace Ardayasa.Infrastructure.Bookings;

public class BookingService(AppDbContext db, IClinicSettingsService settings, IAuditLogger audit) : IBookingService
{
    /// <summary>Payment window per SPEC §6.3; the Hangfire auto-expiry job lands in Phase 3.</summary>
    private static readonly TimeSpan PaymentWindow = TimeSpan.FromMinutes(30);

    private const int MaxRangeDays = 31;
    private const int DefaultRangeDays = 14;

    public async Task<IReadOnlyList<BookableServiceDto>> GetBookableCatalogAsync(CancellationToken ct = default)
        => await PsychologistServiceMappingService.BookableServices(db)
            .Where(s => db.PsychologistServices.Any(m =>
                m.ServiceId == s.Id && m.Psychologist!.IsActive && m.Psychologist!.Slug != null))
            .OrderBy(s => s.Category!.SortOrder)
            .ThenBy(s => s.SortOrder)
            .Select(s => new BookableServiceDto(
                s.Id, s.Name, s.Category!.Name, s.DurationMinutes!.Value, s.OfflinePrice, s.OnlinePrice, s.Notes))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServicePsychologistDto>?> GetPsychologistsForServiceAsync(
        Guid serviceId, CancellationToken ct = default)
    {
        var bookable = await PsychologistServiceMappingService.BookableServices(db)
            .AnyAsync(s => s.Id == serviceId, ct);
        if (!bookable)
        {
            return null;
        }

        var rows = await OfferingPsychologists(serviceId)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.DisplayName)
            .Select(p => new { p.Id, p.DisplayName, p.Title, p.Specialization, p.Slug, p.PhotoKey })
            .ToListAsync(ct);
        return rows
            .Select(p => new ServicePsychologistDto(
                p.Id, p.DisplayName, p.Title, p.Specialization, p.Slug, Content.FileUrl.From(p.PhotoKey)))
            .ToList();
    }

    public async Task<Result<IReadOnlyList<DaySlotsDto>>> GetSlotsAsync(
        Guid serviceId, Guid? psychologistId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default)
    {
        var service = await PsychologistServiceMappingService.BookableServices(db)
            .Where(s => s.Id == serviceId)
            .Select(s => new { Duration = s.DurationMinutes!.Value })
            .FirstOrDefaultAsync(ct);
        if (service is null)
        {
            return Result<IReadOnlyList<DaySlotsDto>>.Failure(BookingErrors.ServiceNotBookable);
        }

        List<Guid> candidates;
        if (psychologistId is not null)
        {
            var offered = await OfferingPsychologists(serviceId).AnyAsync(p => p.Id == psychologistId, ct);
            if (!offered)
            {
                return Result<IReadOnlyList<DaySlotsDto>>.Failure(BookingErrors.ServiceNotOffered);
            }

            candidates = [psychologistId.Value];
        }
        else
        {
            // "Tanpa preferensi": aggregate across everyone offering the service.
            candidates = await OfferingPsychologists(serviceId).Select(p => p.Id).ToListAsync(ct);
        }

        var nowUtc = DateTime.UtcNow;
        var today = Wib.Today(nowUtc);
        var from = fromDate ?? today;
        var to = toDate ?? from.AddDays(DefaultRangeDays - 1);
        if (from < today)
        {
            from = today;
        }

        if (to < from || to.DayNumber - from.DayNumber >= MaxRangeDays)
        {
            return Result<IReadOnlyList<DaySlotsDto>>.Failure(BookingErrors.InvalidDateRange);
        }

        var buffer = await settings.GetSlotBufferMinutesAsync(ct);
        var merged = new Dictionary<(DateTime Start, DateTime End), List<Guid>>();
        foreach (var candidate in candidates)
        {
            foreach (var slot in await GenerateSlotsAsync(candidate, service.Duration, from, to, nowUtc, buffer, ct))
            {
                if (!merged.TryGetValue((slot.StartUtc, slot.EndUtc), out var ids))
                {
                    merged[(slot.StartUtc, slot.EndUtc)] = ids = [];
                }

                ids.Add(candidate);
            }
        }

        var byDay = merged
            .Select(kv => new SlotDto(kv.Key.Start, kv.Key.End, kv.Value))
            .OrderBy(s => s.StartUtc)
            .GroupBy(s => Wib.ToWibDate(s.StartUtc))
            .OrderBy(g => g.Key)
            .Select(g => new DaySlotsDto(g.Key, g.ToList()))
            .ToList();
        return Result<IReadOnlyList<DaySlotsDto>>.Success(byDay);
    }

    public async Task<Result<PatientBookingDto>> CreateAsync(
        Guid patientUserId, CreateBookingRequest request, CancellationToken ct = default)
    {
        // Intake gate (clinic decision 2026-07-07): no booking until Data Pribadi is complete.
        var profile = await db.PatientProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == patientUserId, ct);
        if (profile is null || !profile.IsComplete())
        {
            return Result<PatientBookingDto>.Failure(BookingErrors.IntakeIncomplete);
        }

        var psychologist = await db.Psychologists.AsNoTracking()
            .Where(p => p.Id == request.PsychologistId && p.IsActive)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(ct);
        if (psychologist is null)
        {
            return Result<PatientBookingDto>.Failure(BookingErrors.PsychologistNotFound);
        }

        var service = await PsychologistServiceMappingService.BookableServices(db)
            .Where(s => s.Id == request.ServiceId)
            .Select(s => new { s.Id, Duration = s.DurationMinutes!.Value, s.OfflinePrice, s.OnlinePrice })
            .FirstOrDefaultAsync(ct);
        if (service is null)
        {
            return Result<PatientBookingDto>.Failure(BookingErrors.ServiceNotBookable);
        }

        var offered = await db.PsychologistServices.AsNoTracking()
            .AnyAsync(m => m.PsychologistId == request.PsychologistId && m.ServiceId == request.ServiceId, ct);
        if (!offered)
        {
            return Result<PatientBookingDto>.Failure(BookingErrors.ServiceNotOffered);
        }

        var price = request.Mode == BookingMode.Offline ? service.OfflinePrice : service.OnlinePrice;
        if (price is null)
        {
            return Result<PatientBookingDto>.Failure(BookingErrors.ModeUnavailable);
        }

        var nowUtc = DateTime.UtcNow;
        var startUtc = NormalizeUtc(request.StartUtc);

        // The requested start must be one of the currently generatable slots — this
        // enforces alignment, availability, and (app-level) overlap in one check.
        var slotDate = Wib.ToWibDate(startUtc);
        var buffer = await settings.GetSlotBufferMinutesAsync(ct);
        var candidateSlots = await GenerateSlotsAsync(psychologist.Id, service.Duration, slotDate, slotDate, nowUtc, buffer, ct);
        if (!candidateSlots.Any(s => s.StartUtc == startUtc))
        {
            return Result<PatientBookingDto>.Failure(BookingErrors.SlotUnavailable);
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            PatientUserId = patientUserId,
            PsychologistId = psychologist.Id,
            ServiceId = service.Id,
            Mode = request.Mode,
            StartUtc = startUtc,
            EndUtc = startUtc.AddMinutes(service.Duration),
            DurationMinutes = service.Duration,
            PriceIdr = price.Value,
            Status = BookingStatus.PendingPayment,
            CreatedAtUtc = nowUtc,
            PaymentDueAtUtc = nowUtc.Add(PaymentWindow),
        };
        db.Bookings.Add(booking);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The partial unique index (or the Postgres range-exclusion constraint)
            // rejected a concurrent hold on the same slot.
            return Result<PatientBookingDto>.Failure(BookingErrors.SlotTaken);
        }

        return Result<PatientBookingDto>.Success((await GetForPatientAsync(patientUserId, booking.Id, ct))!);
    }

    public async Task<IReadOnlyList<PatientBookingDto>> ListForPatientAsync(Guid patientUserId, CancellationToken ct = default)
        => await db.Bookings.AsNoTracking()
            .Where(b => b.PatientUserId == patientUserId)
            .OrderByDescending(b => b.StartUtc)
            .Select(PatientProjection)
            .ToListAsync(ct);

    public async Task<PatientBookingDto?> GetForPatientAsync(Guid patientUserId, Guid bookingId, CancellationToken ct = default)
        => await db.Bookings.AsNoTracking()
            .Where(b => b.PatientUserId == patientUserId && b.Id == bookingId)
            .Select(PatientProjection)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<StaffBookingDto>> ListForPsychologistAsync(Guid psychologistUserId, CancellationToken ct = default)
        => await db.Bookings.AsNoTracking()
            .Where(b => b.Psychologist!.UserId == psychologistUserId)
            .OrderByDescending(b => b.StartUtc)
            .Select(StaffProjection(db))
            .ToListAsync(ct);

    public async Task<Result<StaffBookingDto>> SetZoomLinkAsOwnerAsync(
        Guid psychologistUserId, Guid bookingId, SetZoomLinkRequest request, CancellationToken ct = default)
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.Psychologist!.UserId == psychologistUserId, ct);
        return await SetZoomLinkAsync(booking, request, actorUserId: null, ct);
    }

    public async Task<PagedResult<StaffBookingDto>> ListForAdminAsync(
        BookingStatus? status, Guid? psychologistId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Bookings.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(b => b.Status == status);
        }

        if (psychologistId is not null)
        {
            query = query.Where(b => b.PsychologistId == psychologistId);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(b => b.StartUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(StaffProjection(db))
            .ToListAsync(ct);
        return new PagedResult<StaffBookingDto>(items, total, page, pageSize);
    }

    public async Task<Result<StaffBookingDto>> SetZoomLinkAsAdminAsync(
        Guid bookingId, SetZoomLinkRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        return await SetZoomLinkAsync(booking, request, actorUserId, ct);
    }

    private async Task<Result<StaffBookingDto>> SetZoomLinkAsync(
        Booking? booking, SetZoomLinkRequest request, Guid? actorUserId, CancellationToken ct)
    {
        if (booking is null)
        {
            return Result<StaffBookingDto>.Failure(BookingErrors.BookingNotFound);
        }

        if (booking.Mode != BookingMode.Online)
        {
            return Result<StaffBookingDto>.Failure(BookingErrors.ZoomLinkOfflineBooking);
        }

        var link = request.ZoomLink.Trim();
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || link.Length > 500)
        {
            return Result<StaffBookingDto>.Failure(BookingErrors.ZoomLinkInvalid);
        }

        booking.ZoomLink = link;
        await db.SaveChangesAsync(ct);

        // Only admin actions are audited (SPEC §9); the link itself is not logged.
        if (actorUserId is not null)
        {
            await audit.LogAsync(actorUserId, "booking.zoom_link_set", "Booking", booking.Id.ToString(), null, ct);
        }

        var dto = await db.Bookings.AsNoTracking()
            .Where(b => b.Id == booking.Id)
            .Select(StaffProjection(db))
            .SingleAsync(ct);
        return Result<StaffBookingDto>.Success(dto);
    }

    private async Task<List<Slot>> GenerateSlotsAsync(
        Guid psychologistId, int durationMinutes, DateOnly from, DateOnly to, DateTime nowUtc, int bufferMinutes, CancellationToken ct)
    {
        var rules = await db.AvailabilityRules.AsNoTracking()
            .Where(r => r.PsychologistId == psychologistId)
            .ToListAsync(ct);
        var exceptions = await db.AvailabilityExceptions.AsNoTracking()
            .Where(x => x.PsychologistId == psychologistId && x.Date >= from && x.Date <= to)
            .ToListAsync(ct);

        // Any active booking overlapping the range blocks slots; WIB days start/end
        // off the UTC date boundary, so pad the window by a day on each side.
        var rangeStartUtc = Wib.ToUtc(from, TimeOnly.MinValue).AddDays(-1);
        var rangeEndUtc = Wib.ToUtc(to, TimeOnly.MinValue).AddDays(2);
        var activeStatuses = BookingStateMachine.ActiveStatuses;
        var bookings = await db.Bookings.AsNoTracking()
            .Where(b => b.PsychologistId == psychologistId
                        && activeStatuses.Contains(b.Status)
                        && b.StartUtc < rangeEndUtc
                        && b.EndUtc > rangeStartUtc)
            .Select(b => new Slot(b.StartUtc, b.EndUtc))
            .ToListAsync(ct);

        return SlotGenerator.Generate(from, to, durationMinutes, bufferMinutes, rules, exceptions, bookings, nowUtc);
    }

    /// <summary>Publicly bookable psychologists for a service: active, publicly listed (has a slug), and mapped to it.</summary>
    private IQueryable<Psychologist> OfferingPsychologists(Guid serviceId)
        => db.Psychologists.AsNoTracking()
            .Where(p => p.IsActive
                        && p.Slug != null
                        && db.PsychologistServices.Any(m => m.PsychologistId == p.Id && m.ServiceId == serviceId));

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    private static readonly System.Linq.Expressions.Expression<Func<Booking, PatientBookingDto>> PatientProjection =
        b => new PatientBookingDto(
            b.Id, b.PsychologistId, b.Psychologist!.DisplayName, b.Psychologist!.Slug,
            b.Service!.Name, b.Mode, b.StartUtc, b.EndUtc, b.DurationMinutes, b.PriceIdr, b.Status,
            b.Status == BookingStatus.Confirmed ? b.ZoomLink : null,
            b.Status == BookingStatus.PendingPayment ? b.PaymentDueAtUtc : null,
            b.CreatedAtUtc);

    private static System.Linq.Expressions.Expression<Func<Booking, StaffBookingDto>> StaffProjection(AppDbContext db)
        => b => new StaffBookingDto(
            b.Id, b.PsychologistId, b.Psychologist!.DisplayName,
            b.Service!.Name, b.Mode, b.StartUtc, b.EndUtc, b.DurationMinutes, b.PriceIdr, b.Status, b.ZoomLink,
            db.Users.Where(u => u.Id == b.PatientUserId).Select(u => u.FullName).First(),
            db.Users.Where(u => u.Id == b.PatientUserId).Select(u => u.PhoneNumber).First(),
            b.CreatedAtUtc);
}
