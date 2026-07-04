using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ardayasa.Tests.Support;

namespace Ardayasa.Tests;

/// <summary>
/// Phase 1 content: public read endpoints are anonymous, admin CRUD is role-guarded,
/// draft/publish rules hold, HTML is sanitized, and psychologists can edit only
/// their own profile.
/// </summary>
public class ContentTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private record TokenResponseBody(UserBody User, string AccessToken, int ExpiresInSeconds);

    private record UserBody(Guid Id, string FullName, string Email, string? WhatsAppNumber, string[] Roles);

    private record ArticleBody(Guid Id, string Title, string Slug, string? Excerpt, string ContentHtml, string Status);

    private record ArticleListItemBody(string Title, string Slug);

    private record PagedArticlesBody(ArticleListItemBody[] Items, int TotalCount, int Page, int PageSize);

    private record ArticleDetailBody(string Title, string Slug, string ContentHtml);

    private record FaqBody(Guid Id, string Question, string AnswerHtml);

    private record ProfileBody(
        Guid Id, string DisplayName, string? Title, string? Slug, string? Specialization,
        string[] Education, string[] Expertise, string? Bio, string? PhotoUrl,
        string[] ScheduleLines, int DisplayOrder, bool IsActive);

    private record PublicPsychologistBody(Guid Id, string DisplayName, string? Slug);

    private record ErrorsBody(ErrorBody[] Errors);

    private record ErrorBody(string Code, string? Description);

    [Fact]
    public async Task PublicContentEndpoints_AreAnonymous()
    {
        var client = factory.CreateApiClient();
        foreach (var path in new[] { "/api/psychologists", "/api/services", "/api/articles", "/api/articles/categories", "/api/faq", "/api/testimonials" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task AdminContentEndpoints_RequireAdminRole()
    {
        var client = factory.CreateApiClient();

        // Anonymous → 401
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/articles")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/faq")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/services")).StatusCode);

        // Patient → 403
        var patientToken = await RegisterPatientAsync(client, "patient-content@test.local");
        foreach (var path in new[] { "/api/admin/articles", "/api/admin/faq", "/api/admin/testimonials", "/api/admin/services" })
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", patientToken);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(request)).StatusCode);
        }
    }

    [Fact]
    public async Task ArticleFlow_DraftInvisible_PublishedVisible_HtmlSanitized()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAsync(client, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

        // Create a draft with hostile HTML and an Indonesian title
        var create = await SendJsonAsync(client, HttpMethod.Post, "/api/admin/articles", adminToken, new
        {
            title = "Mengelola Stres Sehari-hari",
            excerpt = "Tips singkat.",
            contentHtml = "<p>Aman.</p><script>alert('xss')</script><p onclick=\"evil()\">Klik</p>",
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var article = await create.Content.ReadFromJsonAsync<ArticleBody>();
        Assert.NotNull(article);
        Assert.Equal("mengelola-stres-sehari-hari", article.Slug);
        Assert.Equal("Draft", article.Status);
        Assert.DoesNotContain("<script", article.ContentHtml);
        Assert.DoesNotContain("onclick", article.ContentHtml);
        Assert.Contains("<p>Aman.</p>", article.ContentHtml);

        // Draft is not publicly visible
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/articles/{article.Slug}")).StatusCode);

        // Publish → publicly visible in list and detail
        var publish = await SendJsonAsync(client, HttpMethod.Post, $"/api/admin/articles/{article.Id}/publish", adminToken, null);
        Assert.Equal(HttpStatusCode.NoContent, publish.StatusCode);

        var detail = await client.GetFromJsonAsync<ArticleDetailBody>($"/api/articles/{article.Slug}");
        Assert.NotNull(detail);
        Assert.Equal(article.Title, detail.Title);

        var list = await client.GetFromJsonAsync<PagedArticlesBody>("/api/articles");
        Assert.NotNull(list);
        Assert.Contains(list.Items, i => i.Slug == article.Slug);

        // Duplicate title → stable machine error code
        var duplicate = await SendJsonAsync(client, HttpMethod.Post, "/api/admin/articles", adminToken, new
        {
            title = "Mengelola Stres Sehari-hari",
            contentHtml = "<p>Lain.</p>",
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        var errors = await duplicate.Content.ReadFromJsonAsync<ErrorsBody>();
        Assert.NotNull(errors);
        Assert.Contains(errors.Errors, e => e.Code == "content.slug_taken");

        // Unpublish hides it again
        await SendJsonAsync(client, HttpMethod.Post, $"/api/admin/articles/{article.Id}/unpublish", adminToken, null);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/articles/{article.Slug}")).StatusCode);
    }

    [Fact]
    public async Task Faq_OnlyPublishedItemsArePublic()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAsync(client, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

        var published = await SendJsonAsync(client, HttpMethod.Post, "/api/admin/faq", adminToken, new
        {
            question = "Apakah konseling bisa online?",
            answerHtml = "<p><strong>Bisa.</strong></p>",
            sortOrder = 1,
            isPublished = true,
        });
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);

        var hidden = await SendJsonAsync(client, HttpMethod.Post, "/api/admin/faq", adminToken, new
        {
            question = "Pertanyaan tersembunyi?",
            answerHtml = "<p>Belum tayang.</p>",
            sortOrder = 2,
            isPublished = false,
        });
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);

        var publicFaq = await client.GetFromJsonAsync<FaqBody[]>("/api/faq");
        Assert.NotNull(publicFaq);
        Assert.Contains(publicFaq, f => f.Question == "Apakah konseling bisa online?");
        Assert.DoesNotContain(publicFaq, f => f.Question == "Pertanyaan tersembunyi?");
    }

    [Fact]
    public async Task Testimonial_RejectsInvalidRating()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAsync(client, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

        var response = await SendJsonAsync(client, HttpMethod.Post, "/api/admin/testimonials", adminToken, new
        {
            authorName = "Klien A.",
            content = "Sangat membantu.",
            rating = 6,
            isPublished = true,
            sortOrder = 1,
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<ErrorsBody>();
        Assert.NotNull(errors);
        Assert.Contains(errors.Errors, e => e.Code == "content.invalid_rating");
    }

    [Fact]
    public async Task PsychologistProfile_OwnEditOnly_AndPublicAfterProfileSave()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAsync(client, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

        // Admin invites a psychologist, who accepts
        const string email = "psikolog-profil@test.local";
        var invite = await SendJsonAsync(client, HttpMethod.Post, "/api/admin/psychologists", adminToken, new
        {
            fullName = "Dewi Lestari",
            email,
            title = "M.Psi., Psikolog",
        });
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);

        const string password = "SandiPsikolog2!";
        var accept = await client.PostAsJsonAsync("/api/auth/accept-invitation", new
        {
            email,
            token = factory.Emails.ExtractToken(email),
            password,
        });
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
        var psychologistToken = await LoginAsync(client, email, password);

        // Not yet public: no slug until the profile is saved
        var before = await client.GetFromJsonAsync<PublicPsychologistBody[]>("/api/psychologists");
        Assert.NotNull(before);
        Assert.DoesNotContain(before, p => p.DisplayName == "Dewi Lestari");

        // Patient cannot access the psychologist profile endpoint
        var patientToken = await RegisterPatientAsync(client, "patient-profile@test.local");
        var forbidden = new HttpRequestMessage(HttpMethod.Get, "/api/psychologist/profile");
        forbidden.Headers.Authorization = new AuthenticationHeaderValue("Bearer", patientToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(forbidden)).StatusCode);

        // Psychologist updates own profile; admin-only fields are ignored
        var update = await SendJsonAsync(client, HttpMethod.Put, "/api/psychologist/profile", psychologistToken, new
        {
            displayName = "Dewi Lestari",
            title = "M.Psi., Psikolog",
            specialization = "Psikolog Klinis Dewasa",
            education = new[] { "Sarjana Psikologi, Universitas Indonesia" },
            expertise = new[] { "Kecemasan", "Depresi" },
            bio = "Berpengalaman mendampingi klien dewasa.",
            scheduleLines = new[] { "Senin 09.00–13.00 WIB" },
            displayOrder = 99,
            isActive = false, // must be ignored on the self-service endpoint
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var profile = await update.Content.ReadFromJsonAsync<ProfileBody>();
        Assert.NotNull(profile);
        Assert.Equal("dewi-lestari", profile.Slug);
        Assert.True(profile.IsActive);
        Assert.Equal(0, profile.DisplayOrder);

        // Now publicly listed with a working detail page
        var after = await client.GetFromJsonAsync<PublicPsychologistBody[]>("/api/psychologists");
        Assert.NotNull(after);
        Assert.Contains(after, p => p.Slug == "dewi-lestari");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/psychologists/dewi-lestari")).StatusCode);

        // Admin can edit the same profile on the psychologist's behalf, including admin-only fields
        var adminUpdate = await SendJsonAsync(client, HttpMethod.Put, $"/api/admin/psychologists/{profile.Id}/profile", adminToken, new
        {
            displayName = "Dewi Lestari",
            title = "M.Psi., Psikolog",
            specialization = "Psikolog Klinis Dewasa",
            education = new[] { "Sarjana Psikologi, Universitas Indonesia" },
            expertise = new[] { "Kecemasan", "Depresi", "Trauma" },
            bio = "Berpengalaman mendampingi klien dewasa.",
            scheduleLines = new[] { "Senin 09.00–13.00 WIB" },
            displayOrder = 2,
            isActive = true,
        });
        Assert.Equal(HttpStatusCode.OK, adminUpdate.StatusCode);
        var adminProfile = await adminUpdate.Content.ReadFromJsonAsync<ProfileBody>();
        Assert.NotNull(adminProfile);
        Assert.Equal(2, adminProfile.DisplayOrder);
        Assert.Equal(3, adminProfile.Expertise.Length);
    }

    private async Task<string> RegisterPatientAsync(HttpClient client, string email)
    {
        const string password = "SandiPasien1!";
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Pasien Konten",
            email,
            whatsAppNumber = "+6281234567899",
            password,
        });
        await client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            email,
            token = factory.Emails.ExtractToken(email),
        });
        return await LoginAsync(client, email, password);
    }

    private static async Task<HttpResponseMessage> SendJsonAsync(HttpClient client, HttpMethod method, string path, string token, object? body)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenResponseBody>();
        return body!.AccessToken;
    }
}
