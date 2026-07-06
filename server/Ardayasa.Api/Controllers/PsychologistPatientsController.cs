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
public class PsychologistPatientsController(
    IPatientAssignmentService assignments,
    ILogbookService logbook) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await assignments.ListForPsychologistAsync(ActorId(), ct));

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetDetail(Guid userId, CancellationToken ct)
        => await assignments.GetPatientDetailForPsychologistAsync(ActorId(), userId, ct) is { } dto
            ? Ok(dto)
            : NotFound();

    // Logbook: read for every assigned psychologist, edit for the author only,
    // no delete route by design. Admin and patients have no logbook endpoints.

    [HttpGet("{userId:guid}/logbook")]
    public async Task<IActionResult> ListLogbook(Guid userId, CancellationToken ct)
        => await logbook.ListAsync(ActorId(), userId, ct) is { } entries
            ? Ok(entries)
            : NotFound();

    [HttpPost("{userId:guid}/logbook")]
    public async Task<IActionResult> CreateLogbookEntry(Guid userId, SaveLogbookEntryRequest request, CancellationToken ct)
        => ToResponse(await logbook.CreateAsync(ActorId(), userId, request, ct));

    [HttpPut("{userId:guid}/logbook/{entryId:guid}")]
    public async Task<IActionResult> UpdateLogbookEntry(Guid userId, Guid entryId, SaveLogbookEntryRequest request, CancellationToken ct)
        => ToResponse(await logbook.UpdateAsync(ActorId(), userId, entryId, request, ct));

    private IActionResult ToResponse(Application.Common.Result<LogbookEntryDto> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        if (result.Errors.Any(e => e.Code is "logbook.not_assigned" or "logbook.entry_not_found"))
        {
            return NotFound(new { errors = result.Errors });
        }

        return result.Errors.Any(e => e.Code == "logbook.not_author")
            ? StatusCode(StatusCodes.Status403Forbidden, new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
