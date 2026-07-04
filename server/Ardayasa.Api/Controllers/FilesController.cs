using Ardayasa.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// Serves stored public images (psychologist photos, article images). Keys are
/// randomized server-generated paths; IFileStorage rejects traversal outside its root.
/// </summary>
[ApiController]
[Route("api/files")]
public class FilesController(IFileStorage files) : ControllerBase
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
    };

    [HttpGet("{**key}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        if (!ContentTypes.TryGetValue(Path.GetExtension(key), out var contentType))
        {
            return NotFound();
        }

        var stream = await files.OpenReadAsync(key, ct);
        return stream is null ? NotFound() : File(stream, contentType);
    }
}
