using Ardayasa.Application.Psychologists;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

[ApiController]
[Route("api/admin/psychologists")]
[Authorize(Roles = Roles.Admin)]
public class AdminPsychologistsController(IPsychologistAdminService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await service.ListAsync(ct));

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
