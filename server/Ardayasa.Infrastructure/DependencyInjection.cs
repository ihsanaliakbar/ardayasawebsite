using Ardayasa.Application.Auth;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Application.Psychologists;
using Ardayasa.Infrastructure.Auth;
using Ardayasa.Infrastructure.Email;
using Ardayasa.Infrastructure.Files;
using Ardayasa.Infrastructure.Identity;
using Ardayasa.Infrastructure.Options;
using Ardayasa.Infrastructure.Persistence;
using Ardayasa.Infrastructure.Psychologists;
using Ardayasa.Infrastructure.WhatsApp;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ardayasa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<FonnteOptions>(configuration.GetSection(FonnteOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));

        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(configuration.GetConnectionString("Default")));

        services
            .AddIdentityCore<ApplicationUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.SignIn.RequireConfirmedEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPsychologistAdminService, PsychologistAdminService>();
        services.AddScoped<IAuditLogger, EfAuditLogger>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // Real senders only when configured; otherwise logging stubs (dev).
        var smtpConfigured = !string.IsNullOrWhiteSpace(configuration[$"{SmtpOptions.SectionName}:Host"]);
        if (smtpConfigured)
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }

        var fonnteConfigured = !string.IsNullOrWhiteSpace(configuration[$"{FonnteOptions.SectionName}:ApiToken"]);
        if (fonnteConfigured)
        {
            services.AddHttpClient<IWhatsAppSender, FonnteWhatsAppSender>();
        }
        else
        {
            services.AddScoped<IWhatsAppSender, LoggingWhatsAppSender>();
        }

        return services;
    }
}
