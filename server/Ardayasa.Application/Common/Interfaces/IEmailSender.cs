namespace Ardayasa.Application.Common.Interfaces;

/// <summary>
/// Transactional email. v1 implementation: SMTP via MailKit.
/// When SMTP is not configured, a logging stub is registered instead.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken ct = default);
}
