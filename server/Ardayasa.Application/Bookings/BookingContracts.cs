using Ardayasa.Application.Common;
using Ardayasa.Domain.Entities;

namespace Ardayasa.Application.Bookings;

/// <summary>A service offered by a psychologist that can be booked online (single session, has duration + mode price).</summary>
public record BookableServiceDto(
    Guid Id,
    string Name,
    string CategoryName,
    int DurationMinutes,
    decimal? OfflinePrice,
    decimal? OnlinePrice,
    string? Notes);

/// <summary>A psychologist offering a given service, for the wizard's "pilih psikolog" step.</summary>
public record ServicePsychologistDto(
    Guid PsychologistId,
    string DisplayName,
    string? Title,
    string? Specialization,
    string? Slug,
    string? PhotoUrl);

public record DaySlotsDto(DateOnly Date, IReadOnlyList<SlotDto> Slots);

/// <summary>
/// One bookable time with every psychologist free at it. Single-element list when
/// the query was filtered to one psychologist; multi-element for "tanpa preferensi",
/// where the client picks one of the ids at booking time.
/// </summary>
public record SlotDto(DateTime StartUtc, DateTime EndUtc, IReadOnlyList<Guid> PsychologistIds);

public record CreateBookingRequest(
    Guid PsychologistId,
    Guid ServiceId,
    BookingMode Mode,
    DateTime StartUtc);

/// <summary>The patient's own view. ZoomLink is populated only once the booking is Confirmed.</summary>
public record PatientBookingDto(
    Guid Id,
    Guid PsychologistId,
    string PsychologistName,
    string? PsychologistSlug,
    string ServiceName,
    BookingMode Mode,
    DateTime StartUtc,
    DateTime EndUtc,
    int DurationMinutes,
    decimal PriceIdr,
    BookingStatus Status,
    string? ZoomLink,
    DateTime? PaymentDueAtUtc,
    DateTime CreatedAtUtc);

/// <summary>Staff view (psychologist's own sessions, or admin's list) — includes the patient's account basics, never intake content.</summary>
public record StaffBookingDto(
    Guid Id,
    Guid PsychologistId,
    string PsychologistName,
    string ServiceName,
    BookingMode Mode,
    DateTime StartUtc,
    DateTime EndUtc,
    int DurationMinutes,
    decimal PriceIdr,
    BookingStatus Status,
    string? ZoomLink,
    string PatientName,
    string? PatientWhatsApp,
    DateTime CreatedAtUtc);

public record SetZoomLinkRequest(string ZoomLink);

public interface IBookingService
{
    /// <summary>The service-first wizard catalog: bookable services offered by at least one publicly listed psychologist.</summary>
    Task<IReadOnlyList<BookableServiceDto>> GetBookableCatalogAsync(CancellationToken ct = default);

    /// <summary>Publicly listed psychologists offering the service; null when the service isn't bookable.</summary>
    Task<IReadOnlyList<ServicePsychologistDto>?> GetPsychologistsForServiceAsync(Guid serviceId, CancellationToken ct = default);

    /// <summary>
    /// Available slots (UTC) over a WIB date range — for one psychologist when
    /// <paramref name="psychologistId"/> is given, otherwise aggregated across every
    /// psychologist offering the service ("tanpa preferensi").
    /// </summary>
    Task<Result<IReadOnlyList<DaySlotsDto>>> GetSlotsAsync(
        Guid serviceId, Guid? psychologistId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default);

    Task<Result<PatientBookingDto>> CreateAsync(Guid patientUserId, CreateBookingRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PatientBookingDto>> ListForPatientAsync(Guid patientUserId, CancellationToken ct = default);

    Task<PatientBookingDto?> GetForPatientAsync(Guid patientUserId, Guid bookingId, CancellationToken ct = default);

    Task<IReadOnlyList<StaffBookingDto>> ListForPsychologistAsync(Guid psychologistUserId, CancellationToken ct = default);

    /// <summary>Psychologist sets the meeting link on their own booking; 404-style null when the booking isn't theirs.</summary>
    Task<Result<StaffBookingDto>> SetZoomLinkAsOwnerAsync(
        Guid psychologistUserId, Guid bookingId, SetZoomLinkRequest request, CancellationToken ct = default);

    Task<PagedResult<StaffBookingDto>> ListForAdminAsync(
        BookingStatus? status, Guid? psychologistId, int page, int pageSize, CancellationToken ct = default);

    Task<Result<StaffBookingDto>> SetZoomLinkAsAdminAsync(
        Guid bookingId, SetZoomLinkRequest request, Guid actorUserId, CancellationToken ct = default);
}
