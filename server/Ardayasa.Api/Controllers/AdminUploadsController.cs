using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>Image uploads for admin-authored content (article featured images, editor images).</summary>
[ApiController]
[Route("api/admin/uploads")]
[Authorize(Roles = Roles.Admin)]
public class AdminUploadsController(IFileStorage files) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var key = await files.SaveAsync(stream, file.FileName, ct);
            return Ok(new { key, url = $"/api/files/{key}" });
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new { errors = new[] { new { code = "content.invalid_file", description = "The uploaded file is not an allowed image type or exceeds the size limit." } } });
        }
    }
}
