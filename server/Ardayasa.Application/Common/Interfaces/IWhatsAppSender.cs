namespace Ardayasa.Application.Common.Interfaces;

/// <summary>
/// WhatsApp notifications. v1 implementation: Fonnte.
/// Implementations must never throw out of SendAsync in a way that breaks the
/// calling flow — failures are logged and recorded, not propagated.
/// </summary>
public interface IWhatsAppSender
{
    /// <returns>true if accepted by the gateway; false on failure (already logged).</returns>
    Task<bool> SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}
