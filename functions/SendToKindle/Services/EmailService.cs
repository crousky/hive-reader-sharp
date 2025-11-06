using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SendToKindle.Services;

public class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService()
    {
        _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
        _smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
        _smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "";
        _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
        _fromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL") ?? _smtpUsername;
        _fromName = Environment.GetEnvironmentVariable("FROM_NAME") ?? "Send to Kindle";
    }

    public async Task SendToKindle(byte[] epubData, string fileName, string recipientEmail)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(new MailboxAddress("", recipientEmail));
        message.Subject = "Your Article from Send to Kindle";

        var builder = new BodyBuilder
        {
            TextBody = "Please find your requested article attached as an EPUB file."
        };

        // Attach the EPUB file
        builder.Attachments.Add(fileName, epubData, new ContentType("application", "epub+zip"));

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        // For development/testing, you might want to disable certificate validation
        // client.ServerCertificateValidationCallback = (s, c, h, e) => true;

        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
