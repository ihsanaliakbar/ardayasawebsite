using Ardayasa.Application.Psychologists;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// Read-only view of the psychologist's own profile. Profile editing is
/// admin-only (clinic decision 2026-07-05) via AdminPsychologistsController.
/// </summary>
[ApiController]
[Route("api/psychologist/profile")]
[Authorize(Roles = Roles.Psychologist)]
public class PsychologistProfileController(IPsychologistProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => await profiles.GetOwnAsync(ActorId(), ct) is { } dto ? Ok(dto) : NotFound();

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
