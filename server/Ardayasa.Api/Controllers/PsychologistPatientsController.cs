using Ardayasa.Application.Patients;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// A psychologist's view of their assigned patients. The intake detail endpoint
/// is gated on an assignment row; without one it returns 404 (not 403) so it
/// doesn't leak whether a given patient exists.
/// </summary>
[ApiController]
[Route("api/psychologist/patients")]
[Authorize(Roles = Roles.Psychologist)]
public class PsychologistPatientsController(IPatientAssignmentService assignments) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await assignments.ListForPsychologistAsync(ActorId(), ct));

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetDetail(Guid userId, CancellationToken ct)
        => await assignments.GetPatientDetailForPsychologistAsync(ActorId(), userId, ct) is { } dto
            ? Ok(dto)
            : NotFound();

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
