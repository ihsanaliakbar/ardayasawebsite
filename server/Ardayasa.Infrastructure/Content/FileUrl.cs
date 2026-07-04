namespace Ardayasa.Infrastructure.Content;

public static class FileUrl
{
    /// <summary>Public URL path for a stored file key (served by FilesController).</summary>
    public static string? From(string? storageKey)
        => string.IsNullOrEmpty(storageKey) ? null : $"/api/files/{storageKey}";
}
