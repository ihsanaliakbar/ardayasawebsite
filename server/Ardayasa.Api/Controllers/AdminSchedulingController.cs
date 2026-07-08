using Ardayasa.Application.Bookings;
using Ardayasa.Application.Scheduling;
using Ardayasa.Domain;
using Ardayasa.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// Admin scheduling surface: availability (the ONLY mutation path — psychologists
/// are read-only per clinic decision 2026-07-07), the psychologist↔service
/// mapping, the bookings list, and clinic settings. All mutations are audited.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminSchedulingController(
    IAvailabilityService availability,
    IPsychologistServiceMapping mapping,
    IBookingService bookings,
    IClinicSettingsService settings) : ControllerBase
{
    // --- Availability ---

    [HttpGet("psychologists/{id:guid}/availability")]
    public async Task<IActionResult> GetAvailability(Guid id, CancellationToken ct)
        => await availability.GetByPsychologistIdAsync(id, ct) is { } dto ? Ok(dto) : NotFound();

    [HttpPut("psychologists/{id:guid}/availability")]
    public async Task<IActionResult> ReplaceRules(Guid id, ReplaceAvailabilityRequest request, CancellationToken ct)
        => ToResponse(await availability.ReplaceRulesAsync(id, request, ActorId(), ct));

    [HttpPost("psychologists/{id:guid}/availability/exceptions")]
    public async Task<IActionResult> AddException(Guid id, AddAvailabilityExceptionRequest request, CancellationToken ct)
        => ToResponse(await availability.AddExceptionAsync(id, request, ActorId(), ct));

    [HttpDelete("psychologists/{id:guid}/availability/exceptions/{exceptionId:guid}")]
    public async Task<IActionResult> RemoveException(Guid id, Guid exceptionId, CancellationToken ct)
    {
        var result = await availability.RemoveExceptionAsync(id, exceptionId, ActorId(), ct);
        return result.Succeeded ? NoContent() : NotFound(new { errors = result.Errors });
    }

    // --- Psychologist ↔ service mapping ---

    [HttpGet("psychologists/{id:guid}/services")]
    public async Task<IActionResult> GetServices(Guid id, CancellationToken ct)
        => await mapping.GetForPsychologistAsync(id, ct) is { } dto ? Ok(dto) : NotFound();

    [HttpPut("psychologists/{id:guid}/services")]
    public async Task<IActionResult> ReplaceServices(Guid id, ReplacePsychologistServicesRequest request, CancellationToken ct)
    {
        var result = await mapping.ReplaceAsync(id, request, ActorId(), ct);
        if (result.Succeeded)
        {
            return NoContent();
        }

        return result.Errors.Any(e => e.Code == "scheduling.psychologist_not_found")
            ? NotFound(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }

    // --- Bookings ---

    [HttpGet("bookings")]
    public async Task<IActionResult> ListBookings(
        [FromQuery] BookingStatus? status,
        [FromQuery] Guid? psychologistId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await bookings.ListForAdminAsync(status, psychologistId, page, pageSize, ct));

    [HttpPut("bookings/{id:guid}/zoom-link")]
    public async Task<IActionResult> SetZoomLink(Guid id, SetZoomLinkRequest request, CancellationToken ct)
    {
        var result = await bookings.SetZoomLinkAsAdminAsync(id, request, ActorId(), ct);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.Errors.Any(e => e.Code == "booking.not_found")
            ? NotFound(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }

    // --- Settings ---

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
        => Ok(await settings.GetAsync(ct));

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(ClinicSettingsDto request, CancellationToken ct)
    {
        var result = await settings.UpdateAsync(request, ActorId(), ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    private IActionResult ToResponse<T>(Application.Common.Result<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.Errors.Any(e => e.Code is "scheduling.psychologist_not_found" or "scheduling.exception_not_found")
            ? NotFound(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
