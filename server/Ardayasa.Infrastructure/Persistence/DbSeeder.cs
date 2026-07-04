using Ardayasa.Domain;
using Ardayasa.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ardayasa.Infrastructure.Persistence;

/// <summary>Seeds roles and the default admin account (credentials from environment).</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var adminEmail = configuration["ADMIN_EMAIL"];
        var adminPassword = configuration["ADMIN_PASSWORD"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("ADMIN_EMAIL/ADMIN_PASSWORD not set — skipping admin seed");
            return;
        }

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = configuration["ADMIN_FULL_NAME"] ?? "Admin",
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Failed to seed admin account: {errors}");
        }

        await userManager.AddToRoleAsync(admin, Roles.Admin);
        logger.LogInformation("Seeded default admin account {Email}", adminEmail);
    }
}
