using System.Net;
using System.Net.Http.Json;
using Ardayasa.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Ardayasa.Tests;

public class RateLimitingTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task AuthEndpoints_Return429_WhenLimitExceeded()
    {
        using var limitedFactory = factory.WithWebHostBuilder(b =>
            b.UseSetting("RateLimiting:AuthPermitLimit", "3"));
        var client = limitedFactory.CreateClient();

        var payload = new { email = "nobody@test.local", password = "wrong-password" };
        for (var i = 0; i < 3; i++)
        {
            var allowed = await client.PostAsJsonAsync("/api/auth/login", payload);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        var limited = await client.PostAsJsonAsync("/api/auth/login", payload);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}
