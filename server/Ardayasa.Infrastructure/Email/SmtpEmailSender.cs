using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Ardayasa.Infrastructure.Email;

public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _smtp = options.Value;

    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTlsWhenAvailable, ct);
        if (!string.IsNullOrEmpty(_smtp.Username))
        {
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password ?? string.Empty, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
        logger.LogInformation("Email sent to {To}: {Subject}", toAddress, subject);
    }
}

/// <summary>Dev fallback when SMTP is not configured: logs instead of sending.</summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation(
            "EMAIL (stub — SMTP not configured)\nTo: {To}\nSubject: {Subject}\nBody:\n{Body}",
            toAddress, subject, htmlBody);
        return Task.CompletedTask;
    }
}
