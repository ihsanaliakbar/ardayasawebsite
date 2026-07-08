using Ardayasa.Domain.Entities;

namespace Ardayasa.Application.Scheduling;

/// <summary>One weekly window; times are wall-clock WIB ("HH:mm:ss" over the API).</summary>
public record AvailabilityRuleDto(Guid Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public record AvailabilityExceptionDto(
    Guid Id,
    DateOnly Date,
    AvailabilityExceptionKind Kind,
    TimeOnly? StartTime,
    TimeOnly? EndTime);

/// <summary>The full jadwal praktik of one psychologist: weekly rules + upcoming dated exceptions.</summary>
public record AvailabilityViewDto(
    IReadOnlyList<AvailabilityRuleDto> Rules,
    IReadOnlyList<AvailabilityExceptionDto> Exceptions);

public record AvailabilityRuleInput(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

/// <summary>Full replacement of a psychologist's weekly rules (the admin editor saves the whole week).</summary>
public record ReplaceAvailabilityRequest(IReadOnlyList<AvailabilityRuleInput> Rules);

public record AddAvailabilityExceptionRequest(
    DateOnly Date,
    AvailabilityExceptionKind Kind,
    TimeOnly? StartTime,
    TimeOnly? EndTime);

/// <summary>A bookable service row in the admin psychologist↔service mapping editor.</summary>
public record PsychologistServiceMapDto(
    Guid ServiceId,
    string Name,
    string CategoryName,
    int? DurationMinutes,
    decimal? OfflinePrice,
    decimal? OnlinePrice,
    bool Enabled);

public record ReplacePsychologistServicesRequest(IReadOnlyList<Guid> ServiceIds);

public record ClinicSettingsDto(int SlotBufferMinutes);

public interface IAvailabilityService
{
    /// <summary>Read-only view for the psychologist's own dashboard; null when the caller has no psychologist record.</summary>
    Task<AvailabilityViewDto?> GetOwnAsync(Guid psychologistUserId, CancellationToken ct = default);

    Task<AvailabilityViewDto?> GetByPsychologistIdAsync(Guid psychologistId, CancellationToken ct = default);

    Task<Common.Result<AvailabilityViewDto>> ReplaceRulesAsync(
        Guid psychologistId, ReplaceAvailabilityRequest request, Guid actorUserId, CancellationToken ct = default);

    Task<Common.Result<AvailabilityExceptionDto>> AddExceptionAsync(
        Guid psychologistId, AddAvailabilityExceptionRequest request, Guid actorUserId, CancellationToken ct = default);

    Task<Common.Result> RemoveExceptionAsync(
        Guid psychologistId, Guid exceptionId, Guid actorUserId, CancellationToken ct = default);
}

public interface IPsychologistServiceMapping
{
    /// <summary>All bookable catalog services with an enabled flag for the given psychologist (admin editor).</summary>
    Task<IReadOnlyList<PsychologistServiceMapDto>?> GetForPsychologistAsync(Guid psychologistId, CancellationToken ct = default);

    Task<Common.Result> ReplaceAsync(
        Guid psychologistId, ReplacePsychologistServicesRequest request, Guid actorUserId, CancellationToken ct = default);
}

public interface IClinicSettingsService
{
    Task<ClinicSettingsDto> GetAsync(CancellationToken ct = default);

    Task<int> GetSlotBufferMinutesAsync(CancellationToken ct = default);

    Task<Common.Result<ClinicSettingsDto>> UpdateAsync(ClinicSettingsDto settings, Guid actorUserId, CancellationToken ct = default);
}
