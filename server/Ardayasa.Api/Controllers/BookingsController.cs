using Ardayasa.Application.Bookings;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// Patient booking: create a booking (slot hold, PendingPayment) and view own
/// bookings. The intake form must be complete before the first booking
/// (clinic decision 2026-07-07) — creation fails with booking.intake_incomplete
/// and the client redirects to the Data Pribadi form.
/// </summary>
[ApiController]
[Route("api")]
[Authorize(Roles = Roles.Patient)]
public class BookingsController(IBookingService bookings) : ControllerBase
{
    [HttpPost("bookings")]
    public async Task<IActionResult> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var result = await bookings.CreateAsync(ActorId(), request, ct);
        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(GetOwn), new { id = result.Value!.Id }, result.Value);
        }

        return result.Errors[0].Code switch
        {
            "booking.slot_taken" => Conflict(new { errors = result.Errors }),
            "booking.psychologist_not_found" => NotFound(new { errors = result.Errors }),
            _ => BadRequest(new { errors = result.Errors }),
        };
    }

    [HttpGet("me/bookings")]
    public async Task<IActionResult> ListOwn(CancellationToken ct)
        => Ok(await bookings.ListForPatientAsync(ActorId(), ct));

    [HttpGet("me/bookings/{id:guid}")]
    public async Task<IActionResult> GetOwn(Guid id, CancellationToken ct)
        => await bookings.GetForPatientAsync(ActorId(), id, ct) is { } dto ? Ok(dto) : NotFound();

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
