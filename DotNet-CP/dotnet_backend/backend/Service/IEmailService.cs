namespace ComputerSeekho.Service;

/// <summary>
/// Outbound email — the .NET counterpart of the Java EmailService.
///
/// Note what every method here has in common: none of them throw. That is
/// the entire design, and it mirrors Java's sendSafely.
///
/// A registration that succeeded but whose confirmation email bounced is
/// still a successful registration. Rolling back a student, an enrolment and
/// a payment because a mail server was briefly unreachable would be far worse
/// than a missing email — and because RegistrationService runs inside a
/// transaction, an exception raised here would do exactly that.
///
/// So failures are logged and reported as a bool. The caller decides what to
/// tell the user; nothing is ever lost because SMTP had a bad day.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// True when a username is configured and sending is enabled. Exposed so
    /// callers can say "not sent" honestly rather than claiming success.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Sends a plain-text message. Returns false if it could not be sent —
    /// no recipient, not configured, or the server refused it.
    /// </summary>
    Task<bool> SendAsync(string? to, string subject, string body, CancellationToken ct = default);

    /// <summary>
    /// Sends a message with one file attached — the receipt PDF, in practice.
    /// </summary>
    Task<bool> SendWithAttachmentAsync(
        string? to,
        string subject,
        string body,
        string filename,
        byte[] attachment,
        string contentType = "application/pdf",
        CancellationToken ct = default);
}
