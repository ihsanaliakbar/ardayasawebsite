using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Infrastructure.Identity;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ardayasa.Tests.Support;

/// <summary>
/// Boots the real API pipeline with SQLite in-memory instead of Postgres,
/// a capturing email sender, and seeded roles + admin.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin12345!";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CapturingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting (not ConfigureAppConfiguration): with minimal hosting these values
        // must be present before Program.cs reads configuration at startup.
        builder.UseSetting("Jwt:Issuer", "ardayasa-test");
        builder.UseSetting("Jwt:Audience", "ardayasa-test-web");
        builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-0123456789-0123456789");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100");
        builder.UseSetting("ADMIN_EMAIL", AdminEmail);
        builder.UseSetting("ADMIN_PASSWORD", AdminPassword);
        builder.UseSetting("ADMIN_FULL_NAME", "Test Admin");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            // EF Core 9+ also stores the provider setup separately; without this the
            // Npgsql configuration survives and clashes with Sqlite.
            services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<AppDbContext>));
            _connection.Open();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        DbSeeder.SeedAsync(
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                scope.ServiceProvider.GetRequiredService<IConfiguration>(),
                NullLogger.Instance)
            .GetAwaiter()
            .GetResult();

        return host;
    }

    /// <summary>Client with cookies enabled and an https base address so the Secure refresh cookie round-trips.</summary>
    public HttpClient CreateApiClient()
        => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
