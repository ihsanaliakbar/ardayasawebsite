using Ardayasa.Application.Common;
using Ardayasa.Application.Patients;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Content;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ardayasa.Infrastructure.Patients;

public class PatientProfileService(AppDbContext db) : IPatientProfileService
{
    public async Task<PatientProfileDto?> GetOwnAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await db.PatientProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return profile is null ? null : ToDto(profile);
    }

    public async Task<Result<PatientProfileDto>> UpsertOwnAsync(Guid userId, UpdatePatientProfileRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return Result<PatientProfileDto>.Failure(PatientErrors.NameRequired);
        }

        if (request.HasPriorDiagnosis == true && string.IsNullOrWhiteSpace(request.PriorDiagnosis))
        {
            return Result<PatientProfileDto>.Failure(PatientErrors.DiagnosisRequired);
        }

        var now = DateTime.UtcNow;
        var profile = await db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
        {
            profile = new PatientProfile { UserId = userId, FullName = request.FullName.Trim(), CreatedAtUtc = now };
            db.PatientProfiles.Add(profile);
        }

        profile.FullName = request.FullName.Trim();
        profile.BirthPlace = Clean(request.BirthPlace);
        profile.BirthDate = request.BirthDate;
        profile.Gender = request.Gender;
        profile.DomicileAddress = Clean(request.DomicileAddress);
        profile.MaritalStatus = request.MaritalStatus;
        profile.LastEducation = request.LastEducation;
        profile.Occupation = Clean(request.Occupation);
        profile.HasAccessedPsychologyServices = request.HasAccessedPsychologyServices;
        profile.HasPriorDiagnosis = request.HasPriorDiagnosis;
        // A cleared "prior diagnosis" answer must not silently retain the old description.
        profile.PriorDiagnosis = request.HasPriorDiagnosis == true ? Clean(request.PriorDiagnosis) : null;
        profile.ConsultationConcerns = Clean(request.ConsultationConcerns);
        profile.CounselingExpectations = Clean(request.CounselingExpectations);
        profile.UpdatedAtUtc = now;

        await db.SaveChangesAsync(ct);
        return Result<PatientProfileDto>.Success(ToDto(profile));
    }

    public async Task<IReadOnlyList<AssignedPsychologistDto>> GetAssignedPsychologistsAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await db.PatientAssignments.AsNoTracking()
            .Where(a => a.PatientUserId == userId)
            .OrderBy(a => a.AssignedAtUtc)
            .Select(a => new
            {
                a.PsychologistId,
                a.Psychologist!.DisplayName,
                a.Psychologist.Title,
                a.Psychologist.Specialization,
                a.Psychologist.Slug,
                a.Psychologist.PhotoKey,
                a.AssignedAtUtc,
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new AssignedPsychologistDto(
                r.PsychologistId, r.DisplayName, r.Title, r.Specialization,
                r.Slug, FileUrl.From(r.PhotoKey), r.AssignedAtUtc))
            .ToList();
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static PatientProfileDto ToDto(PatientProfile p) => new(
        p.FullName, p.BirthPlace, p.BirthDate, p.Gender, p.DomicileAddress,
        p.MaritalStatus, p.LastEducation, p.Occupation,
        p.HasAccessedPsychologyServices, p.HasPriorDiagnosis, p.PriorDiagnosis,
        p.ConsultationConcerns, p.CounselingExpectations,
        p.IsComplete(), p.UpdatedAtUtc);
}
