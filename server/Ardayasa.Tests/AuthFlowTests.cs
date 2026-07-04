using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ardayasa.Tests.Support;

namespace Ardayasa.Tests;

public class AuthFlowTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private record TokenResponseBody(UserBody User, string AccessToken, int ExpiresInSeconds);

    private record UserBody(Guid Id, string FullName, string Email, string? WhatsAppNumber, string[] Roles);

    [Fact]
    public async Task Register_Verify_Login_Refresh_Logout_HappyPath()
    {
        var client = factory.CreateApiClient();
        const string email = "patient1@test.local";
        const string password = "Sandi12345!";

        // Register
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Pasien Satu",
            email,
            whatsAppNumber = "+6281234567890",
            password,
        });
        Assert.Equal(HttpStatusCode.NoContent, register.StatusCode);

        // Login before verification must fail
        var earlyLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.BadRequest, earlyLogin.StatusCode);

        // Verify email using the token from the captured email
        var verify = await client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            email,
            token = factory.Emails.ExtractToken(email),
        });
        Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode);

        // Login
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<TokenResponseBody>();
        Assert.NotNull(tokens);
        Assert.Contains("Patient", tokens.User.Roles);

        // Access token works on a protected endpoint
        using (var me = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me"))
        {
            me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            var meResponse = await client.SendAsync(me);
            Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        }

        // Refresh rotates the cookie and returns a fresh access token
        var refresh = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var refreshed = await refresh.Content.ReadFromJsonAsync<TokenResponseBody>();
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrEmpty(refreshed.AccessToken));

        // Logout revokes the current refresh token and clears the cookie
        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // Refresh after logout must fail (no cookie / revoked)
        var afterLogout = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_IssuesWorkingToken()
    {
        var client = factory.CreateApiClient();
        const string email = "patient-resend@test.local";
        const string password = "Sandi12345!";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Pasien Kirim Ulang",
            email,
            whatsAppNumber = "+6281234567899",
            password,
        });

        // Resend replaces the original token; the new one must verify.
        var resend = await client.PostAsJsonAsync("/api/auth/resend-verification", new { email });
        Assert.Equal(HttpStatusCode.NoContent, resend.StatusCode);

        var verify = await client.PostAsJsonAsync("/api/auth/verify-email", new
        {
            email,
            token = factory.Emails.ExtractToken(email),
        });
        Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Unknown addresses still get 204 (no account enumeration).
        var unknown = await client.PostAsJsonAsync("/api/auth/resend-verification", new { email = "ghost@test.local" });
        Assert.Equal(HttpStatusCode.NoContent, unknown.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        var client = factory.CreateApiClient();
        var response = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = factory.CreateApiClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestWebApplicationFactory.AdminEmail,
            password = "definitely-wrong",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
