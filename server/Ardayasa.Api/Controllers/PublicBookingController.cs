using Ardayasa.Application.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// Anonymous, read-only booking data for the public wizard: which services a
/// psychologist can be booked for, and the available slots. Creating a booking
/// requires a patient account (BookingsController).
/// </summary>
[ApiController]
[Route("api/psychologists/{slug}")]
[AllowAnonymous]
public class PublicBookingController(IBookingService bookings) : ControllerBase
{
    [HttpGet("services")]
    public async Task<IActionResult> GetBookableServices(string slug, CancellationToken ct)
        => await bookings.GetBookableServicesAsync(slug, ct) is { } services
            ? Ok(services)
            : NotFound();

    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots(
        string slug,
        [FromQuery] Guid serviceId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var result = await bookings.GetSlotsAsync(slug, serviceId, from, to, ct);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.Errors.Any(e => e.Code is "booking.psychologist_not_found" or "booking.service_not_offered")
            ? NotFound(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }
}
