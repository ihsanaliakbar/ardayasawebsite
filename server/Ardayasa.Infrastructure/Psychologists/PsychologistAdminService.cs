using Ardayasa.Application.Common;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Application.Psychologists;
using Ardayasa.Domain;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Auth;
using Ardayasa.Infrastructure.Email;
using Ardayasa.Infrastructure.Identity;
using Ardayasa.Infrastructure.Options;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ardayasa.Infrastructure.Psychologists;

public class PsychologistAdminService(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IEmailSender emailSender,
    IAuditLogger auditLogger,
    IOptions<AppOptions> appOptions) : IPsychologistAdminService
{
    private readonly AppOptions _app = appOptions.Value;

    public async Task<Result<PsychologistDto>> InviteAsync(
        InvitePsychologistRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Result<PsychologistDto>.Failure(AuthErrors.EmailAlreadyRegistered);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        // No password: the psychologist sets one via the invitation link.
        var created = await userManager.CreateAsync(user);
        if (!created.Succeeded)
        {
            return Result<PsychologistDto>.Failure(
                [.. created.Errors.Select(e => AuthErrors.IdentityError(e.Code, e.Description))]);
        }

        await userManager.AddToRoleAsync(user, Roles.Psychologist);

        var psychologist = new Psychologist
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DisplayName = request.FullName.Trim(),
            Title = request.Title?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Psychologists.Add(psychologist);
        await db.SaveChangesAsync(ct);

        await SendInvitationAsync(user, ct);
        await auditLogger.LogAsync(
            actorUserId, "psychologist.invited", nameof(Psychologist), psychologist.Id.ToString(),
            new { user.Email, psychologist.DisplayName }, ct);

        return Result<PsychologistDto>.Success(ToDto(psychologist, user, invitationAccepted: false));
    }

    public async Task<Result> ResendInvitationAsync(Guid psychologistId, Guid actorUserId, CancellationToken ct = default)
    {
        var psychologist = await db.Psychologists.SingleOrDefaultAsync(p => p.Id == psychologistId, ct);
        if (psychologist is null)
        {
            return Result.Failure(AuthErrors.UserNotFound);
        }

        var user = await userManager.FindByIdAsync(psychologist.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(AuthErrors.UserNotFound);
        }

        if (await userManager.HasPasswordAsync(user))
        {
            return Result.Failure(AuthErrors.InvitationAlreadyAccepted);
        }

        await SendInvitationAsync(user, ct);
        await auditLogger.LogAsync(
            actorUserId, "psychologist.invitation_resent", nameof(Psychologist), psychologist.Id.ToString(),
            new { user.Email }, ct);

        return Result.Success();
    }

    public async Task<IReadOnlyList<PsychologistDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = await db.Psychologists
            .Join(db.Users, p => p.UserId, u => u.Id, (p, u) => new { p, u })
            .OrderBy(x => x.p.DisplayName)
            .ToListAsync(ct);

        return [.. rows.Select(x => ToDto(x.p, x.u, invitationAccepted: x.u.PasswordHash != null))];
    }

    private async Task SendInvitationAsync(ApplicationUser user, CancellationToken ct)
    {
        // Invitations reuse the password-reset token purpose (single-use, expiring).
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var url = $"{_app.WebBaseUrl.TrimEnd('/')}/terima-undangan" +
                  $"?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
        var (subject, body) = EmailTemplates.PsychologistInvitation(user.FullName, url);
        await emailSender.SendAsync(user.Email!, subject, body, ct);
    }

    private static PsychologistDto ToDto(Psychologist p, ApplicationUser u, bool invitationAccepted)
        => new(p.Id, p.UserId, p.DisplayName, p.Title, u.Email!, p.IsActive, invitationAccepted);
}
