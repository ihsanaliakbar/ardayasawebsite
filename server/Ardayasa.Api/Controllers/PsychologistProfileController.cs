using Ardayasa.Application.Psychologists;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>Self-service profile management for psychologists (own record only).</summary>
[ApiController]
[Route("api/psychologist/profile")]
[Authorize(Roles = Roles.Psychologist)]
public class PsychologistProfileController(IPsychologistProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => await profiles.GetOwnAsync(ActorId(), ct) is { } dto ? Ok(dto) : NotFound();

    [HttpPut]
    public async Task<IActionResult> Update(UpdatePsychologistProfileRequest request, CancellationToken ct)
    {
        var result = await profiles.UpdateOwnAsync(ActorId(), request, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("photo")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await profiles.SetOwnPhotoAsync(ActorId(), stream, file.FileName, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
