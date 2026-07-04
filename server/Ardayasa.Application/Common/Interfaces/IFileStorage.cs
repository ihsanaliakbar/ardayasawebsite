namespace Ardayasa.Application.Common.Interfaces;

/// <summary>
/// Abstraction over file/image storage (psychologist photos, article images).
/// v1 implementation: local disk on a Docker volume. Swappable to S3-compatible later.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Stores the stream under a randomized name and returns the storage key
    /// (relative path) to persist. Validates size and extension.
    /// </summary>
    Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default);

    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
