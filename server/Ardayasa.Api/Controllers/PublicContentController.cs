using Ardayasa.Application.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>
/// Read-only content for the public marketing site. Anonymous by design (SPEC §4);
/// only published/active content is returned by the service layer.
/// </summary>
[ApiController]
[Route("api")]
[AllowAnonymous]
public class PublicContentController(IPublicContentService content) : ControllerBase
{
    [HttpGet("psychologists")]
    public async Task<IActionResult> Psychologists(CancellationToken ct)
        => Ok(await content.GetPsychologistsAsync(ct));

    [HttpGet("psychologists/{slug}")]
    public async Task<IActionResult> Psychologist(string slug, CancellationToken ct)
        => await content.GetPsychologistAsync(slug, ct) is { } dto ? Ok(dto) : NotFound();

    [HttpGet("services")]
    public async Task<IActionResult> Services(CancellationToken ct)
        => Ok(await content.GetServiceCatalogAsync(ct));

    [HttpGet("articles")]
    public async Task<IActionResult> Articles([FromQuery] string? category, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 9, CancellationToken ct = default)
        => Ok(await content.GetArticlesAsync(new ArticleQuery(category, search, page, pageSize), ct));

    [HttpGet("articles/categories")]
    public async Task<IActionResult> ArticleCategories(CancellationToken ct)
        => Ok(await content.GetArticleCategoriesAsync(ct));

    [HttpGet("articles/{slug}")]
    public async Task<IActionResult> Article(string slug, CancellationToken ct)
        => await content.GetArticleAsync(slug, ct) is { } dto ? Ok(dto) : NotFound();

    [HttpGet("faq")]
    public async Task<IActionResult> Faq(CancellationToken ct)
        => Ok(await content.GetFaqAsync(ct));

    [HttpGet("testimonials")]
    public async Task<IActionResult> Testimonials(CancellationToken ct)
        => Ok(await content.GetTestimonialsAsync(ct));
}
