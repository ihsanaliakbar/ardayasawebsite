using Ardayasa.Application.Patients;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// Self-service endpoints for the logged-in patient: the intake form
/// ("Data Pribadi") and the list of psychologists assigned to them.
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize(Roles = Roles.Patient)]
public class PatientMeController(IPatientProfileService profiles) : ControllerBase
{
    [HttpGet("patient-profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
        => await profiles.GetOwnAsync(ActorId(), ct) is { } dto ? Ok(dto) : NotFound();

    [HttpPut("patient-profile")]
    public async Task<IActionResult> UpsertProfile(UpdatePatientProfileRequest request, CancellationToken ct)
    {
        var result = await profiles.UpsertOwnAsync(ActorId(), request, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpGet("psychologists")]
    public async Task<IActionResult> GetAssignedPsychologists(CancellationToken ct)
        => Ok(await profiles.GetAssignedPsychologistsAsync(ActorId(), ct));

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
