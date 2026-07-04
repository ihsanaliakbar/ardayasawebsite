namespace Ardayasa.Application.Psychologists;

/// <summary>
/// Editable profile fields. Psychologists may edit their own profile; admins may edit
/// anyone's. <see cref="DisplayOrder"/> and <see cref="IsActive"/> are admin-only and
/// ignored on the self-service endpoint.
/// </summary>
public record UpdatePsychologistProfileRequest(
    string DisplayName,
    string? Title,
    string? Specialization,
    IReadOnlyList<string> Education,
    IReadOnlyList<string> Expertise,
    string? Bio,
    IReadOnlyList<string> ScheduleLines,
    int? DisplayOrder,
    bool? IsActive);

public record PsychologistProfileDto(
    Guid Id,
    string DisplayName,
    string? Title,
    string? Slug,
    string? Specialization,
    IReadOnlyList<string> Education,
    IReadOnlyList<string> Expertise,
    string? Bio,
    string? PhotoUrl,
    IReadOnlyList<string> ScheduleLines,
    int DisplayOrder,
    bool IsActive);
