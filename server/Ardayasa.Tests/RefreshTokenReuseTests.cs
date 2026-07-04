using System.Net;
using System.Net.Http.Json;
using Ardayasa.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ardayasa.Tests;

public class RefreshTokenReuseTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task ReusedRefreshToken_IsRejected_AndAllSessionsRevoked()
    {
        // Cookies are handled manually here so the old (rotated-out) token can be replayed.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
        });

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestWebApplicationFactory.AdminEmail,
            password = TestWebApplicationFactory.AdminPassword,
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var firstCookie = ExtractRefreshCookie(login);

        // First refresh: rotates the token
        var firstRefresh = await RefreshWithCookie(client, firstCookie);
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
        var secondCookie = ExtractRefreshCookie(firstRefresh);

        // Replaying the rotated-out token must be rejected (reuse detection)...
        var replay = await RefreshWithCookie(client, firstCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // ...and must revoke the successor too (all sessions killed).
        var successorAfterReuse = await RefreshWithCookie(client, secondCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, successorAfterReuse.StatusCode);
    }

    private static Task<HttpResponseMessage> RefreshWithCookie(HttpClient client, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }

    private static string ExtractRefreshCookie(HttpResponseMessage response)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("ardayasa_refresh=", StringComparison.Ordinal));
        return setCookie.Split(';')[0];
    }
}
