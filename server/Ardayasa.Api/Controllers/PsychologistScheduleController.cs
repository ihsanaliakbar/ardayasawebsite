using Ardayasa.Application.Bookings;
using Ardayasa.Application.Scheduling;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// The psychologist's own schedule surface. Availability is READ-ONLY here —
/// editing is admin-only (clinic decision 2026-07-07), mirroring the
/// admin-managed profile. Bookings: list own sessions and attach the meeting
/// link to own online bookings.
/// </summary>
[ApiController]
[Route("api/psychologist")]
[Authorize(Roles = Roles.Psychologist)]
public class PsychologistScheduleController(
    IAvailabilityService availability,
    IBookingService bookings) : ControllerBase
{
    [HttpGet("availability")]
    public async Task<IActionResult> GetOwnAvailability(CancellationToken ct)
        => await availability.GetOwnAsync(ActorId(), ct) is { } dto ? Ok(dto) : NotFound();

    [HttpGet("bookings")]
    public async Task<IActionResult> ListOwnBookings(CancellationToken ct)
        => Ok(await bookings.ListForPsychologistAsync(ActorId(), ct));

    [HttpPut("bookings/{id:guid}/zoom-link")]
    public async Task<IActionResult> SetZoomLink(Guid id, SetZoomLinkRequest request, CancellationToken ct)
    {
        var result = await bookings.SetZoomLinkAsOwnerAsync(ActorId(), id, request, ct);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        // Not the caller's booking → 404, existence not leaked.
        return result.Errors.Any(e => e.Code == "booking.not_found")
            ? NotFound(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
