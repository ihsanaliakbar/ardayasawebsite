using Ardayasa.Application.Common;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Application.Patients;
using Ardayasa.Domain;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Identity;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ardayasa.Infrastructure.Patients;

public class PatientAssignmentService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IAuditLogger audit) : IPatientAssignmentService
{
    public async Task<PagedResult<AdminPatientListItemDto>> ListPatientsAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var patientRoleId = await db.Roles
            .Where(r => r.Name == Roles.Patient)
            .Select(r => r.Id)
            .SingleAsync(ct);

        var query = db.Users.AsNoTracking()
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == patientRoleId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Portable ToLower().Contains — the test suite runs the real pipeline on SQLite.
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) || u.Email!.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var users = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.FullName, u.Email, u.PhoneNumber, u.CreatedAtUtc })
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();

        var assignments = await db.PatientAssignments.AsNoTracking()
            .Where(a => userIds.Contains(a.PatientUserId))
            .Select(a => new { a.PatientUserId, a.PsychologistId, a.Psychologist!.DisplayName })
            .ToListAsync(ct);

        // Deliberately only the completeness flag — admin never sees intake answers.
        var completedIds = await db.PatientProfiles.AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .ToListAsync(ct);
        var completedSet = completedIds.Where(p => p.IsComplete()).Select(p => p.UserId).ToHashSet();

        var items = users
            .Select(u => new AdminPatientListItemDto(
                u.Id, u.FullName, u.Email!, u.PhoneNumber, u.CreatedAtUtc,
                completedSet.Contains(u.Id),
                assignments
                    .Where(a => a.PatientUserId == u.Id)
                    .Select(a => new AssignmentSummaryDto(a.PsychologistId, a.DisplayName))
                    .ToList()))
            .ToList();

        return new PagedResult<AdminPatientListItemDto>(items, total, page, pageSize);
    }

    public async Task<Result> AssignAsync(Guid patientUserId, Guid psychologistId, Guid actorUserId, CancellationToken ct = default)
    {
        var patient = await userManager.FindByIdAsync(patientUserId.ToString());
        if (patient is null || !await userManager.IsInRoleAsync(patient, Roles.Patient))
        {
            return Result.Failure(PatientErrors.PatientNotFound);
        }

        if (!await db.Psychologists.AnyAsync(p => p.Id == psychologistId, ct))
        {
            return Result.Failure(PatientErrors.PsychologistNotFound);
        }

        if (await db.PatientAssignments.AnyAsync(
                a => a.PatientUserId == patientUserId && a.PsychologistId == psychologistId, ct))
        {
            return Result.Failure(PatientErrors.AlreadyAssigned);
        }

        db.PatientAssignments.Add(new PatientAssignment
        {
            PatientUserId = patientUserId,
            PsychologistId = psychologistId,
            AssignedAtUtc = DateTime.UtcNow,
            AssignedByUserId = actorUserId,
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index (PatientUserId, PsychologistId) — concurrent duplicate assign.
            return Result.Failure(PatientErrors.AlreadyAssigned);
        }

        await audit.LogAsync(actorUserId, "patient.assigned", nameof(PatientAssignment), patientUserId.ToString(),
            new { patientUserId, psychologistId }, ct);
        return Result.Success();
    }

    public async Task<Result> UnassignAsync(Guid patientUserId, Guid psychologistId, Guid actorUserId, CancellationToken ct = default)
    {
        var assignment = await db.PatientAssignments.FirstOrDefaultAsync(
            a => a.PatientUserId == patientUserId && a.PsychologistId == psychologistId, ct);
        if (assignment is null)
        {
            return Result.Failure(PatientErrors.AssignmentNotFound);
        }

        db.PatientAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "patient.unassigned", nameof(PatientAssignment), patientUserId.ToString(),
            new { patientUserId, psychologistId }, ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<PsychologistPatientListItemDto>> ListForPsychologistAsync(Guid psychologistUserId, CancellationToken ct = default)
    {
        var rows = await db.PatientAssignments.AsNoTracking()
            .Where(a => a.Psychologist!.UserId == psychologistUserId)
            .Join(db.Users, a => a.PatientUserId, u => u.Id,
                (a, u) => new { u.Id, u.FullName, u.PhoneNumber, a.AssignedAtUtc })
            .OrderBy(x => x.FullName)
            .ToListAsync(ct);

        var patientIds = rows.Select(r => r.Id).ToList();
        var profiles = await db.PatientProfiles.AsNoTracking()
            .Where(p => patientIds.Contains(p.UserId))
            .ToListAsync(ct);
        var completedSet = profiles.Where(p => p.IsComplete()).Select(p => p.UserId).ToHashSet();

        return rows
            .Select(r => new PsychologistPatientListItemDto(
                r.Id, r.FullName, r.PhoneNumber, r.AssignedAtUtc, completedSet.Contains(r.Id)))
            .ToList();
    }

    public async Task<PsychologistPatientDetailDto?> GetPatientDetailForPsychologistAsync(Guid psychologistUserId, Guid patientUserId, CancellationToken ct = default)
    {
        // The assignment check IS the authorization: no row, no access — and the
        // caller's 404 doesn't reveal whether the patient exists at all.
        var assignment = await db.PatientAssignments.AsNoTracking()
            .Where(a => a.PatientUserId == patientUserId && a.Psychologist!.UserId == psychologistUserId)
            .Select(a => new { a.AssignedAtUtc })
            .FirstOrDefaultAsync(ct);
        if (assignment is null)
        {
            return null;
        }

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == patientUserId)
            .Select(u => new { u.FullName, u.Email, u.PhoneNumber })
            .SingleAsync(ct);

        var profile = await db.PatientProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == patientUserId, ct);

        return new PsychologistPatientDetailDto(
            patientUserId, user.FullName, user.Email!, user.PhoneNumber, assignment.AssignedAtUtc,
            profile is null ? null : PatientProfileService.ToDto(profile));
    }
}
