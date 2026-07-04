using Ardayasa.Application.Content;
using Ardayasa.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ardayasa.Api.Controllers;

/// <summary>Admin CRUD for FAQ, testimonials, and the service catalog.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminContentController(IContentAdminService content) : ControllerBase
{
    // --- FAQ ---

    [HttpGet("faq")]
    public async Task<IActionResult> ListFaq(CancellationToken ct)
        => Ok(await content.ListFaqAsync(ct));

    [HttpPost("faq")]
    public async Task<IActionResult> CreateFaq(SaveFaqItemRequest request, CancellationToken ct)
        => FromResult(await content.CreateFaqAsync(request, ActorId(), ct));

    [HttpPut("faq/{id:guid}")]
    public async Task<IActionResult> UpdateFaq(Guid id, SaveFaqItemRequest request, CancellationToken ct)
        => FromResult(await content.UpdateFaqAsync(id, request, ActorId(), ct));

    [HttpDelete("faq/{id:guid}")]
    public async Task<IActionResult> DeleteFaq(Guid id, CancellationToken ct)
        => FromResult(await content.DeleteFaqAsync(id, ActorId(), ct));

    // --- Testimonials ---

    [HttpGet("testimonials")]
    public async Task<IActionResult> ListTestimonials(CancellationToken ct)
        => Ok(await content.ListTestimonialsAsync(ct));

    [HttpPost("testimonials")]
    public async Task<IActionResult> CreateTestimonial(SaveTestimonialRequest request, CancellationToken ct)
        => FromResult(await content.CreateTestimonialAsync(request, ActorId(), ct));

    [HttpPut("testimonials/{id:guid}")]
    public async Task<IActionResult> UpdateTestimonial(Guid id, SaveTestimonialRequest request, CancellationToken ct)
        => FromResult(await content.UpdateTestimonialAsync(id, request, ActorId(), ct));

    [HttpDelete("testimonials/{id:guid}")]
    public async Task<IActionResult> DeleteTestimonial(Guid id, CancellationToken ct)
        => FromResult(await content.DeleteTestimonialAsync(id, ActorId(), ct));

    // --- Service catalog ---

    [HttpGet("services")]
    public async Task<IActionResult> ListServiceCatalog(CancellationToken ct)
        => Ok(await content.ListServiceCatalogAsync(ct));

    [HttpPost("services/categories")]
    public async Task<IActionResult> CreateServiceCategory(SaveServiceCategoryRequest request, CancellationToken ct)
        => FromResult(await content.CreateServiceCategoryAsync(request, ActorId(), ct));

    [HttpPut("services/categories/{id:guid}")]
    public async Task<IActionResult> UpdateServiceCategory(Guid id, SaveServiceCategoryRequest request, CancellationToken ct)
        => FromResult(await content.UpdateServiceCategoryAsync(id, request, ActorId(), ct));

    [HttpDelete("services/categories/{id:guid}")]
    public async Task<IActionResult> DeleteServiceCategory(Guid id, CancellationToken ct)
        => FromResult(await content.DeleteServiceCategoryAsync(id, ActorId(), ct));

    [HttpPost("services")]
    public async Task<IActionResult> CreateService(SaveServiceRequest request, CancellationToken ct)
        => FromResult(await content.CreateServiceAsync(request, ActorId(), ct));

    [HttpPut("services/{id:guid}")]
    public async Task<IActionResult> UpdateService(Guid id, SaveServiceRequest request, CancellationToken ct)
        => FromResult(await content.UpdateServiceAsync(id, request, ActorId(), ct));

    [HttpDelete("services/{id:guid}")]
    public async Task<IActionResult> DeleteService(Guid id, CancellationToken ct)
        => FromResult(await content.DeleteServiceAsync(id, ActorId(), ct));

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);

    private IActionResult FromResult(Ardayasa.Application.Common.Result result)
        => result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });

    private IActionResult FromResult<T>(Ardayasa.Application.Common.Result<T> result)
        => result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
}
