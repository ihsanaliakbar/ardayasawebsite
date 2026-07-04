namespace Ardayasa.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public required string SigningKey { get; set; }
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}

public class GoogleOptions
{
    public const string SectionName = "Google";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId);
}

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@ardayasa.local";
    public string FromName { get; set; } = "Ardayasa Wellbeing and Growth Center";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}

public class FonnteOptions
{
    public const string SectionName = "Fonnte";

    public string? ApiToken { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiToken);
}

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = "storage";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
}

public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Public origin of the Angular app; used for CORS and links embedded in emails.</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:4200";
}
