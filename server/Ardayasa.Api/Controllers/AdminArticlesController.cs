using Ardayasa.Application.Content;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

[ApiController]
[Route("api/admin/articles")]
[Authorize(Roles = Roles.Admin)]
public class AdminArticlesController(IContentAdminService content) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await content.ListArticlesAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => await content.GetArticleAsync(id, ct) is { } dto ? Ok(dto) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(SaveArticleRequest request, CancellationToken ct)
    {
        var result = await content.CreateArticleAsync(request, ActorId(), ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveArticleRequest request, CancellationToken ct)
    {
        var result = await content.UpdateArticleAsync(id, request, ActorId(), ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var result = await content.SetArticleStatusAsync(id, publish: true, ActorId(), ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
    {
        var result = await content.SetArticleStatusAsync(id, publish: false, ActorId(), ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await content.DeleteArticleAsync(id, ActorId(), ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(SaveArticleCategoryRequest request, CancellationToken ct)
    {
        var result = await content.CreateArticleCategoryAsync(request, ActorId(), ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        var result = await content.DeleteArticleCategoryAsync(id, ActorId(), ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
