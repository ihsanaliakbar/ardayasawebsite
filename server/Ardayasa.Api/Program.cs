using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Ardayasa.Api.Infrastructure;
using Ardayasa.Infrastructure;
using Ardayasa.Infrastructure.Identity;
using Ardayasa.Infrastructure.Options;
using Ardayasa.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    // Enums cross the API as their names ("Male", "Married"), never numbers;
    // the client maps the names to Indonesian labels in id.json.
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks();

// --- JWT bearer authentication ---
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing Jwt configuration section.");
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be set and at least 32 characters long.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
    });

// Every endpoint requires auth unless explicitly [AllowAnonymous] (SPEC §9).
builder.Services.AddAuthorization(o =>
{
    o.FallbackPolicy = o.DefaultPolicy;
});

// --- Rate limiting on auth endpoints (SPEC §9) ---
var authPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:AuthPermitLimit") ?? 10;
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// --- CORS for the Angular app ---
var webBaseUrl = builder.Configuration[$"{AppOptions.SectionName}:WebBaseUrl"] ?? "http://localhost:4200";
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(webBaseUrl.TrimEnd('/'))
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// --- Hangfire (Postgres storage; skipped under test, where SQLite replaces Postgres) ---
var isTesting = builder.Environment.IsEnvironment("Testing");
if (!isTesting)
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:Default.");
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));
    builder.Services.AddHangfireServer();
}

var app = builder.Build();

// Apply migrations + seed roles/admin (automatic in Development; opt-in via AUTO_MIGRATE elsewhere).
// The test factory creates the schema and seeds by itself.
if (!isTesting && (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("AUTO_MIGRATE")))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
        app.Configuration,
        app.Logger);
    await ContentSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
        scope.ServiceProvider.GetRequiredService<Ardayasa.Application.Common.Interfaces.IFileStorage>(),
        Path.Combine(app.Environment.ContentRootPath, "SeedAssets"),
        app.Logger);
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
if (!isTesting)
{
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireDashboardAuthorizationFilter(app.Environment.IsDevelopment())],
    })
    .AllowAnonymous(); // real gate lives in the filter: Admin role, or local requests in Development
}

app.Run();

public partial class Program;
