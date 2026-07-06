using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ardayasa.Tests.Support;

namespace Ardayasa.Tests;

/// <summary>
/// Phase 1.6: counseling logbook. The guarantees under test: entries are
/// readable by every psychologist assigned to the patient (including each
/// other's), editable only by their author, never deletable, and completely
/// invisible to admin and to the patient.
/// </summary>
public class LogbookTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private record TokenResponseBody(UserBody User, string AccessToken, int ExpiresInSeconds);

    private record UserBody(Guid Id, string FullName, string Email, string? WhatsAppNumber, string[] Roles);

    private record PsychologistBody(Guid Id, Guid UserId, string DisplayName, string Email);

    private record EntryBody(
        Guid Id, string SessionDate, int SessionNumber, string CaseSummary, string SessionActivities,
        string? Homework, string? NextSessionPlan, bool FollowUpNeeded,
        Guid AuthorPsychologistId, string AuthorDisplayName, bool IsOwn,
        DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

    private static object EntryPayload(string summary = "Klien menunjukkan kemajuan dalam mengelola kecemasan.") => new
    {
        sessionDate = "2026-07-01",
        sessionNumber = 1,
        caseSummary = summary,
        sessionActivities = "Eksplorasi pemicu kecemasan dan latihan pernapasan.",
        homework = "Latihan pernapasan 10 menit setiap pagi.",
        nextSessionPlan = "Evaluasi latihan dan mulai restrukturisasi kognitif.",
        followUpNeeded = true,
    };

    [Fact]
    public async Task Logbook_SharedReading_AuthorOnlyEditing_NoDeleting()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAsync(client, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

        var patientToken = await RegisterPatientAsync(client, "logbook1@test.local");
        var patientId = await GetUserIdAsync(client, patientToken);

        var (psychAId, psychAToken) = await InvitePsychologistAsync(client, adminToken, "logbook.psy.a@test.local", "Psikolog Logbook A");
        var (psychBId, psychBToken) = await InvitePsychologistAsync(client, adminToken, "logbook.psy.b@test.local", "Psikolog Logbook B");
        var (_, psychCToken) = await InvitePsychologistAsync(client, adminToken, "logbook.psy.c@test.local", "Psikolog Logbook C");
        await AssignAsync(client, adminToken, patientId, psychAId);
        await AssignAsync(client, adminToken, patientId, psychBId);

        // A creates an entry; invalid payloads are rejected
        var create = await SendAsync(client, HttpMethod.Post, $"/api/psychologist/patients/{patientId}/logbook", psychAToken, EntryPayload());
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<EntryBody>();
        Assert.NotNull(created);
        Assert.True(created.IsOwn);
        Assert.Null(created.UpdatedAtUtc);

        var invalid = await SendAsync(client, HttpMethod.Post, $"/api/psychologist/patients/{patientId}/logbook", psychAToken, new
        {
            sessionDate = "2026-07-01",
            sessionNumber = 0,
            caseSummary = "",
            sessionActivities = "",
            followUpNeeded = false,
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        // B (also assigned) reads A's entry, attributed and not editable by B
        var listB = await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}/logbook", psychBToken);
        Assert.Equal(HttpStatusCode.OK, listB.StatusCode);
        var entryForB = Assert.Single((await listB.Content.ReadFromJsonAsync<EntryBody[]>())!);
        Assert.Equal("Psikolog Logbook A", entryForB.AuthorDisplayName);
        Assert.False(entryForB.IsOwn);

        var editByB = await SendAsync(client, HttpMethod.Put, $"/api/psychologist/patients/{patientId}/logbook/{created.Id}", psychBToken,
            EntryPayload("Disunting oleh psikolog lain."));
        Assert.Equal(HttpStatusCode.Forbidden, editByB.StatusCode);

        // The author edits their own entry; the edit is marked
        var editByA = await SendAsync(client, HttpMethod.Put, $"/api/psychologist/patients/{patientId}/logbook/{created.Id}", psychAToken,
            EntryPayload("Ringkasan diperbarui setelah supervisi."));
        Assert.Equal(HttpStatusCode.OK, editByA.StatusCode);
        var edited = await editByA.Content.ReadFromJsonAsync<EntryBody>();
        Assert.NotNull(edited);
        Assert.Equal("Ringkasan diperbarui setelah supervisi.", edited.CaseSummary);
        Assert.NotNull(edited.UpdatedAtUtc);

        // No delete route exists at all
        var delete = await SendAsync(client, HttpMethod.Delete, $"/api/psychologist/patients/{patientId}/logbook/{created.Id}", psychAToken);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, delete.StatusCode);

        // Unassigned psychologist C: list, create, and edit are all 404 (existence not leaked)
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}/logbook", psychCToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(client, HttpMethod.Post, $"/api/psychologist/patients/{patientId}/logbook", psychCToken, EntryPayload())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(client, HttpMethod.Put, $"/api/psychologist/patients/{patientId}/logbook/{created.Id}", psychCToken, EntryPayload())).StatusCode);

        // Unassigning the author cuts off their access; the colleague still reads the entry
        var unassign = await SendAsync(client, HttpMethod.Delete, $"/api/admin/patients/{patientId}/assignments/{psychAId}", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, unassign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}/logbook", psychAToken)).StatusCode);
        var listAfterUnassign = await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}/logbook", psychBToken);
        Assert.Equal(HttpStatusCode.OK, listAfterUnassign.StatusCode);
        Assert.Single((await listAfterUnassign.Content.ReadFromJsonAsync<EntryBody[]>())!);
    }

    [Fact]
    public async Task Logbook_InvisibleToAdminAndPatient()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAsync(client, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

        var patientToken = await RegisterPatientAsync(client, "logbook2@test.local");
        var patientId = await GetUserIdAsync(client, patientToken);
        var (psychId, psychToken) = await InvitePsychologistAsync(client, adminToken, "logbook.psy.d@test.local", "Psikolog Logbook D");
        await AssignAsync(client, adminToken, patientId, psychId);

        var create = await SendAsync(client, HttpMethod.Post, $"/api/psychologist/patients/{patientId}/logbook", psychToken, EntryPayload());
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        // Admin: the psychologist-role gate rejects every logbook route
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}/logbook", adminToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Post, $"/api/psychologist/patients/{patientId}/logbook", adminToken, EntryPayload())).StatusCode);

        // Admin's patient list carries no trace of logbook content or existence
        var adminList = await SendAsync(client, HttpMethod.Get, "/api/admin/patients?search=logbook2", adminToken);
        Assert.Equal(HttpStatusCode.OK, adminList.StatusCode);
        var raw = await adminList.Content.ReadAsStringAsync();
        // Words from the entry's case summary and activities must never surface here.
        Assert.DoesNotContain("kemajuan", raw);
        Assert.DoesNotContain("pernapasan", raw);
        Assert.DoesNotContain("sessionDate", raw, StringComparison.OrdinalIgnoreCase);

        // The patient has no route to their own logbook
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}/logbook", patientToken)).StatusCode);

        // Anonymous: 401
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync($"/api/psychologist/patients/{patientId}/logbook")).StatusCode);
    }

    private async Task<string> RegisterPatientAsync(HttpClient client, string email)
    {
        const string password = "Sandi12345!";
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Pasien Logbook",
            email,
            whatsAppNumber = "+6281234567890",
            password,
        });
        await client.PostAsJsonAsync("/api/auth/verify-email", new { email, token = factory.Emails.ExtractToken(email) });
        return await LoginAsync(client, email, password);
    }

    private async Task<(Guid PsychologistId, string Token)> InvitePsychologistAsync(
        HttpClient client, string adminToken, string email, string fullName)
    {
        var invite = await SendAsync(client, HttpMethod.Post, "/api/admin/psychologists", adminToken,
            new { fullName, email, title = "M.Psi., Psikolog" });
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
        var created = await invite.Content.ReadFromJsonAsync<PsychologistBody>();
        Assert.NotNull(created);

        const string password = "SandiPsikolog1!";
        var accept = await client.PostAsJsonAsync("/api/auth/accept-invitation", new
        {
            email,
            token = factory.Emails.ExtractToken(email),
            password,
        });
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
        return (created.Id, await LoginAsync(client, email, password));
    }

    private static async Task AssignAsync(HttpClient client, string adminToken, Guid patientId, Guid psychologistId)
    {
        var assign = await SendAsync(client, HttpMethod.Post, $"/api/admin/patients/{patientId}/assignments", adminToken,
            new { psychologistId });
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);
    }

    private static async Task<Guid> GetUserIdAsync(HttpClient client, string accessToken)
    {
        var response = await SendAsync(client, HttpMethod.Get, "/api/auth/me", accessToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<UserBody>();
        return body!.Id;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string url, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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
