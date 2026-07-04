using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ardayasa.Tests.Support;

namespace Ardayasa.Tests;

public class AuthorizationTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private record TokenResponseBody(UserBody User, string AccessToken, int ExpiresInSeconds);

    private record UserBody(Guid Id, string FullName, string Email, string? WhatsAppNumber, string[] Roles);

    private record PsychologistBody(
        Guid Id, Guid UserId, string DisplayName, string? Title, string Email, bool IsActive, bool InvitationAccepted);

    [Fact]
    public async Task AdminEndpoints_RequireAuthentication()
    {
        var client = factory.CreateApiClient();
        var response = await client.GetAsync("/api/admin/psychologists");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_ForbidNonAdmins()
    {
        var client = factory.CreateApiClient();
        const string email = "patient2@test.local";
        const string password = "Sandi12345!";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Pasien Dua",
            email,
            whatsAppNumber = "+6281234567891",
            password,
        });
        await client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            email,
            token = factory.Emails.ExtractToken(email),
        });
        var accessToken = await LoginAsync(client, email, password);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/psychologists");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvitationFlow_AdminInvites_PsychologistAcceptsAndLogsIn()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAsync(
            client, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);
        const string email = "psikolog1@test.local";

        // Admin invites
        var invite = new HttpRequestMessage(HttpMethod.Post, "/api/admin/psychologists")
        {
            Content = JsonContent.Create(new { fullName = "Psikolog Satu", email, title = "M.Psi., Psikolog" }),
        };
        invite.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var inviteResponse = await client.SendAsync(invite);
        Assert.Equal(HttpStatusCode.Created, inviteResponse.StatusCode);
        var created = await inviteResponse.Content.ReadFromJsonAsync<PsychologistBody>();
        Assert.NotNull(created);
        Assert.False(created.InvitationAccepted);

        // Psychologist accepts via the emailed one-time token
        const string password = "SandiPsikolog1!";
        var accept = await client.PostAsJsonAsync("/api/auth/accept-invitation", new
        {
            email,
            token = factory.Emails.ExtractToken(email),
            password,
        });
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);

        // Token is single-use
        var acceptAgain = await client.PostAsJsonAsync("/api/auth/accept-invitation", new
        {
            email,
            token = factory.Emails.ExtractToken(email),
            password = "Lain12345!",
        });
        Assert.Equal(HttpStatusCode.BadRequest, acceptAgain.StatusCode);

        // Psychologist can log in and has the right role
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<TokenResponseBody>();
        Assert.NotNull(tokens);
        Assert.Contains("Psychologist", tokens.User.Roles);

        // ...but still cannot access admin endpoints
        var adminAttempt = new HttpRequestMessage(HttpMethod.Get, "/api/admin/psychologists");
        adminAttempt.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var adminAttemptResponse = await client.SendAsync(adminAttempt);
        Assert.Equal(HttpStatusCode.Forbidden, adminAttemptResponse.StatusCode);

        // Admin sees the accepted invitation in the list
        var list = new HttpRequestMessage(HttpMethod.Get, "/api/admin/psychologists");
        list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var listResponse = await client.SendAsync(list);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var all = await listResponse.Content.ReadFromJsonAsync<PsychologistBody[]>();
        Assert.NotNull(all);
        Assert.True(all.Single(p => p.Email == email).InvitationAccepted);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenResponseBody>();
        return body!.AccessToken;
    }
}
