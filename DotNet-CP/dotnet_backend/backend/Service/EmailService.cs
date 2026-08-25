using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ComputerSeekho.Service;

/// <summary>
/// SMTP sender built on MailKit.
///
/// MailKit rather than System.Net.Mail.SmtpClient, which Microsoft's own
/// documentation marks as obsolete for new development: it does not handle
/// modern STARTTLS negotiation reliably and has no async story worth using.
/// MailKit is what the .NET team points at instead, and it is what Gmail's
/// requirements need.
///
/// Configuration mirrors the Java backend's spring.mail.* properties, so the
/// same Gmail app password works for both without being reissued.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    private readonly bool _enabled;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _from;
    private readonly int _timeoutMs;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;

        _enabled = configuration.GetValue("Mail:Enabled", true);
        _host = configuration["Mail:Host"] ?? "smtp.gmail.com";
        _port = configuration.GetValue("Mail:Port", 587);
        _username = configuration["Mail:Username"] ?? string.Empty;
        _password = configuration["Mail:Password"] ?? string.Empty;

        // Gmail rewrites the From header to the authenticating account
        // anyway, so defaulting to the username avoids a silent mismatch
        // between what we claim to be and what actually arrives.
        _from = string.IsNullOrWhiteSpace(configuration["Mail:From"])
            ? _username
            : configuration["Mail:From"]!;

        _timeoutMs = configuration.GetValue("Mail:TimeoutMs", 5000);

        // Said once at startup rather than discovered when the first
        // registration reports that nothing was emailed.
        if (IsConfigured)
        {
            _logger.LogInformation("Email configured — sending as '{From}' via {Host}:{Port}",
                _from, _host, _port);
        }
        else
        {
            _logger.LogWarning(
                "Email is NOT configured (Mail:Username is blank or Mail:Enabled=false). " +
                "Outbound messages will be skipped and logged instead of sent.");
        }
    }

    public bool IsConfigured => _enabled && !string.IsNullOrWhiteSpace(_username);

    public Task<bool> SendAsync(string? to, string subject, string body, CancellationToken ct = default) =>
        SendCoreAsync(to, subject, body, attachment: null, filename: null, contentType: null, ct);

    public Task<bool> SendWithAttachmentAsync(
        string? to,
        string subject,
        string body,
        string filename,
        byte[] attachment,
        string contentType = "application/pdf",
        CancellationToken ct = default) =>
        SendCoreAsync(to, subject, body, attachment, filename, contentType, ct);

    // ------------------------------------------------------------ internals

    private async Task<bool> SendCoreAsync(
        string? to,
        string subject,
        string body,
        byte[]? attachment,
        string? filename,
        string? contentType,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("Skipping email '{Subject}' — no recipient address", subject);
            return false;
        }

        if (!IsConfigured)
        {
            // Logged at Information, not Warning: running without mail is a
            // legitimate way to run this app, and a warning per enquiry would
            // train people to ignore warnings.
            _logger.LogInformation("Email not configured — would have sent '{Subject}' to {To}",
                subject, to);
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder { TextBody = body };

            if (attachment is { Length: > 0 } && filename is not null)
            {
                builder.Attachments.Add(
                    filename, attachment, ContentType.Parse(contentType ?? "application/octet-stream"));
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient { Timeout = _timeoutMs };

            // StartTlsWhenAvailable rather than a hard StartTls: port 465 is
            // implicit TLS and port 587 is STARTTLS, and letting MailKit pick
            // means changing the port is the only change needed to move
            // between them.
            await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTlsWhenAvailable, ct);
            await client.AuthenticateAsync(_username, _password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            if (filename is null)
            {
                _logger.LogInformation("Email '{Subject}' sent to {To}", subject, to);
            }
            else
            {
                _logger.LogInformation("Email '{Subject}' sent to {To} with attachment {File}",
                    subject, to, filename);
            }

            return true;
        }
        catch (Exception ex)
        {
            // Deliberately catching Exception rather than a list of SMTP
            // types. The contract of this class is that it never throws, and
            // the caller is inside a database transaction — an unexpected
            // exception type escaping here would roll back a completed
            // registration, which is the one outcome worth avoiding at any
            // cost. It is logged in full, so nothing is hidden.
            _logger.LogWarning(ex, "Email '{Subject}' to {To} failed: {Message}",
                subject, to, ex.Message);
            return false;
        }
    }
}
