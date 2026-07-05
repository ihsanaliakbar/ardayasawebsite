using Ardayasa.Application.Patients;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// Admin patient management: list/search patients and manage psychologist
/// assignments. Intake answers are intentionally NOT reachable from here —
/// they are visible only to the patient and their assigned psychologists.
/// </summary>
[ApiController]
[Route("api/admin/patients")]
[Authorize(Roles = Roles.Admin)]
public class AdminPatientsController(IPatientAssignmentService assignments) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await assignments.ListPatientsAsync(search, page, pageSize, ct));

    [HttpPost("{userId:guid}/assignments")]
    public async Task<IActionResult> Assign(Guid userId, AssignPatientRequest request, CancellationToken ct)
    {
        var result = await assignments.AssignAsync(userId, request.PsychologistId, ActorId(), ct);
        if (result.Succeeded)
        {
            return NoContent();
        }

        return result.Errors.Any(e => e.Code == "patients.already_assigned")
            ? Conflict(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{userId:guid}/assignments/{psychologistId:guid}")]
    public async Task<IActionResult> Unassign(Guid userId, Guid psychologistId, CancellationToken ct)
    {
        var result = await assignments.UnassignAsync(userId, psychologistId, ActorId(), ct);
        return result.Succeeded ? NoContent() : NotFound(new { errors = result.Errors });
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
