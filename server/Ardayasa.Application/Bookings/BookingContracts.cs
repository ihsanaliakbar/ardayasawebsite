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

public record DaySlotsDto(DateOnly Date, IReadOnlyList<SlotDto> Slots);

public record SlotDto(DateTime StartUtc, DateTime EndUtc);

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
    /// <summary>Bookable services for the public booking wizard; null when the slug doesn't resolve to an active psychologist.</summary>
    Task<IReadOnlyList<BookableServiceDto>?> GetBookableServicesAsync(string psychologistSlug, CancellationToken ct = default);

    /// <summary>Available slots (UTC) for a psychologist + service over a WIB date range.</summary>
    Task<Result<IReadOnlyList<DaySlotsDto>>> GetSlotsAsync(
        string psychologistSlug, Guid serviceId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default);

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
