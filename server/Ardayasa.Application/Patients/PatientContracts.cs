using Ardayasa.Domain.Entities;

namespace Ardayasa.Application.Patients;

/// <summary>
/// Patient intake upsert. All questions except <see cref="FullName"/> may be left
/// unanswered (null) and completed later; <see cref="PatientProfileDto.IsComplete"/>
/// tells the client whether to keep showing the reminder banner.
/// </summary>
public record UpdatePatientProfileRequest(
    string FullName,
    string? BirthPlace,
    DateOnly? BirthDate,
    Gender? Gender,
    string? DomicileAddress,
    MaritalStatus? MaritalStatus,
    EducationLevel? LastEducation,
    string? Occupation,
    bool? HasAccessedPsychologyServices,
    bool? HasPriorDiagnosis,
    string? PriorDiagnosis,
    string? ConsultationConcerns,
    string? CounselingExpectations);

public record PatientProfileDto(
    string FullName,
    string? BirthPlace,
    DateOnly? BirthDate,
    Gender? Gender,
    string? DomicileAddress,
    MaritalStatus? MaritalStatus,
    EducationLevel? LastEducation,
    string? Occupation,
    bool? HasAccessedPsychologyServices,
    bool? HasPriorDiagnosis,
    string? PriorDiagnosis,
    string? ConsultationConcerns,
    string? CounselingExpectations,
    bool IsComplete,
    DateTime UpdatedAtUtc);

/// <summary>Public-profile subset shown to the patient on "Psikolog Saya".</summary>
public record AssignedPsychologistDto(
    Guid PsychologistId,
    string DisplayName,
    string? Title,
    string? Specialization,
    string? Slug,
    string? PhotoUrl,
    DateTime AssignedAtUtc);

/// <summary>Admin patient list row — account basics only, never intake answers.</summary>
public record AdminPatientListItemDto(
    Guid UserId,
    string FullName,
    string Email,
    string? WhatsAppNumber,
    DateTime RegisteredAtUtc,
    bool ProfileCompleted,
    IReadOnlyList<AssignmentSummaryDto> Assignments);

public record AssignmentSummaryDto(Guid PsychologistId, string DisplayName);

public record AssignPatientRequest(Guid PsychologistId);

/// <summary>Row in a psychologist's "Pasien Saya" list.</summary>
public record PsychologistPatientListItemDto(
    Guid PatientUserId,
    string FullName,
    string? WhatsAppNumber,
    DateTime AssignedAtUtc,
    bool ProfileCompleted);

/// <summary>Full intake detail — only ever returned to an assigned psychologist.</summary>
public record PsychologistPatientDetailDto(
    Guid PatientUserId,
    string AccountName,
    string Email,
    string? WhatsAppNumber,
    DateTime AssignedAtUtc,
    PatientProfileDto? Profile);
