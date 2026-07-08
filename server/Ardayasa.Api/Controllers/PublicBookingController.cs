using Ardayasa.Application.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// Anonymous, read-only data for the service-first booking wizard: the bookable
/// catalog, the psychologists offering a service, and available slots (per
/// psychologist, or aggregated for "tanpa preferensi"). Creating a booking
/// requires a patient account (BookingsController).
/// </summary>
[ApiController]
[Route("api/booking")]
[AllowAnonymous]
public class PublicBookingController(IBookingService bookings) : ControllerBase
{
    [HttpGet("services")]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
        => Ok(await bookings.GetBookableCatalogAsync(ct));

    [HttpGet("services/{serviceId:guid}/psychologists")]
    public async Task<IActionResult> GetPsychologists(Guid serviceId, CancellationToken ct)
        => await bookings.GetPsychologistsForServiceAsync(serviceId, ct) is { } psychologists
            ? Ok(psychologists)
            : NotFound();

    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots(
        [FromQuery] Guid serviceId,
        [FromQuery] Guid? psychologistId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var result = await bookings.GetSlotsAsync(serviceId, psychologistId, from, to, ct);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.Errors.Any(e => e.Code is "booking.service_not_bookable" or "booking.service_not_offered")
            ? NotFound(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }
}
