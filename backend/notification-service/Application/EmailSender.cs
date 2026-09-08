using Microsoft.Extensions.Options;

namespace HRM.NotificationService.Application;

public class EmailSettings
{
    public const string SectionName = "Email";
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@hrm.local";
    public string FromName { get; set; } = "HRM System";
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

/// <summary>
/// In development Enabled=false, so emails are logged instead of sent. Swap this
/// implementation for SMTP/SendGrid without touching any calling code (DIP).
/// </summary>
public class SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("[EMAIL SIMULATED] to={To} subject={Subject}", to, subject);
            return;
        }

        using var client = new System.Net.Mail.SmtpClient(settings.Host, settings.Port)
        {
            Credentials = new System.Net.NetworkCredential(settings.User, settings.Password),
            EnableSsl = true
        };
        var message = new System.Net.Mail.MailMessage(
            new System.Net.Mail.MailAddress(settings.FromAddress, settings.FromName),
            new System.Net.Mail.MailAddress(to))
        { Subject = subject, Body = body, IsBodyHtml = true };

        await client.SendMailAsync(message, ct);
        logger.LogInformation("Email sent to {To}", to);
    }
}
