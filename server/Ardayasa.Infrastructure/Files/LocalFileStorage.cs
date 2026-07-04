using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Ardayasa.Infrastructure.Files;

/// <summary>
/// v1 file storage: local disk (a Docker volume in compose). Storage keys are
/// relative paths like "2026/07/{guid}.jpg" — no user-supplied names ever touch disk.
/// </summary>
public class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;

    public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File type '{extension}' is not allowed.");
        }

        if (content.CanSeek && content.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("File exceeds the maximum allowed size.");
        }

        var now = DateTime.UtcNow;
        var key = Path.Combine(now.ToString("yyyy"), now.ToString("MM"), $"{Guid.NewGuid():N}{extension}");
        var fullPath = GetFullPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await CopyLimitedAsync(content, file, _options.MaxFileSizeBytes, ct);
        return key.Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(storageKey);
        return Task.FromResult<Stream?>(File.Exists(fullPath) ? File.OpenRead(fullPath) : null);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string GetFullPath(string storageKey)
    {
        var root = Path.GetFullPath(_options.RootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, storageKey));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage key resolves outside the storage root.");
        }

        return fullPath;
    }

    private static async Task CopyLimitedAsync(Stream source, Stream destination, long maxBytes, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                throw new InvalidOperationException("File exceeds the maximum allowed size.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), ct);
        }
    }
}
