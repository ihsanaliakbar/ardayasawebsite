using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ardayasa.Infrastructure.WhatsApp;

/// <summary>
/// Fonnte gateway (https://fonnte.com). Per SPEC §8, failures never propagate to
/// the calling flow — this class catches everything, logs, and returns false.
/// </summary>
public class FonnteWhatsAppSender(
    HttpClient httpClient,
    IOptions<FonnteOptions> options,
    ILogger<FonnteWhatsAppSender> logger) : IWhatsAppSender
{
    private readonly FonnteOptions _fonnte = options.Value;

    public async Task<bool> SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.fonnte.com/send");
            request.Headers.TryAddWithoutValidation("Authorization", _fonnte.ApiToken);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["target"] = phoneNumber,
                ["message"] = message,
            });

            var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Fonnte send to {Phone} failed with status {Status}", phoneNumber, response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fonnte send to {Phone} threw", phoneNumber);
            return false;
        }
    }
}

/// <summary>Dev fallback when Fonnte is not configured: logs instead of sending.</summary>
public class LoggingWhatsAppSender(ILogger<LoggingWhatsAppSender> logger) : IWhatsAppSender
{
    public Task<bool> SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        logger.LogInformation(
            "WHATSAPP (stub — Fonnte not configured)\nTo: {Phone}\nMessage: {Message}", phoneNumber, message);
        return Task.FromResult(true);
    }
}
