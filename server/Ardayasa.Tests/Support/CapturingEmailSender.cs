using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Ardayasa.Application.Common.Interfaces;

namespace Ardayasa.Tests.Support;

public record CapturedEmail(string To, string Subject, string HtmlBody);

/// <summary>Test double that records outgoing emails so tests can extract one-time tokens.</summary>
public partial class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<CapturedEmail> _emails = new();

    public Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken ct = default)
    {
        _emails.Enqueue(new CapturedEmail(toAddress, subject, htmlBody));
        return Task.CompletedTask;
    }

    public CapturedEmail LastTo(string address)
        => _emails.LastOrDefault(e => e.To.Equals(address, StringComparison.OrdinalIgnoreCase))
           ?? throw new InvalidOperationException($"No email captured for {address}.");

    /// <summary>Extracts the ?token= query value from the first link in the email body.</summary>
    public string ExtractToken(string address)
    {
        var match = TokenRegex().Match(LastTo(address).HtmlBody);
        if (!match.Success)
        {
            throw new InvalidOperationException($"No token link found in email to {address}.");
        }

        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    [GeneratedRegex("""token=([^"&\s<]+)""")]
    private static partial Regex TokenRegex();
}
