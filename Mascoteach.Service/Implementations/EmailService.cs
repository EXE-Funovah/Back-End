using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendPasswordResetEmailAsync(string email, string fullName, string resetLink)
    {
        var host = _config["Email:SmtpHost"] ?? throw new InvalidOperationException("Email:SmtpHost is missing.");
        var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];
        var fromEmail = _config["Email:FromEmail"] ?? username ?? throw new InvalidOperationException("Email:FromEmail is missing.");
        var fromName = _config["Email:FromName"] ?? "Mascoteach";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(fullName, email));
        message.Subject = "Reset your Mascoteach password";
        message.Body = new TextPart("plain")
        {
            Text = $"Hi {fullName},\n\nUse this link to reset your Mascoteach password:\n{resetLink}\n\nThis link will expire soon. If you did not request this, you can ignore this email."
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            await client.AuthenticateAsync(username, password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
