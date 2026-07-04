using Ardayasa.Application.Psychologists;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

[ApiController]
[Route("api/admin/psychologists")]
[Authorize(Roles = Roles.Admin)]
public class AdminPsychologistsController(
    IPsychologistAdminService service,
    IPsychologistProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await service.ListAsync(ct));

    [HttpGet("{id:guid}/profile")]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken ct)
        => await profiles.GetByIdAsync(id, ct) is { } dto ? Ok(dto) : NotFound();

    [HttpPut("{id:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid id, UpdatePsychologistProfileRequest request, CancellationToken ct)
    {
        var result = await profiles.UpdateAsync(id, request, ActorId(), ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/profile/photo")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfilePhoto(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await profiles.SetPhotoAsync(id, stream, file.FileName, ActorId(), ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost]
    public async Task<IActionResult> Invite(InvitePsychologistRequest request, CancellationToken ct)
    {
        var result = await service.InviteAsync(request, ActorId(), ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(List), result.Value)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/resend-invitation")]
    public async Task<IActionResult> ResendInvitation(Guid id, CancellationToken ct)
    {
        var result = await service.ResendInvitationAsync(id, ActorId(), ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
