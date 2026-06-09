using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Mascoteach.Service.Interfaces;
using System.Net;

namespace Mascoteach.Service.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailVerificationAsync(string email, string fullName, string verificationLink)
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
        message.Subject = "Verify your Mascoteach email";

        var safeFullName = WebUtility.HtmlEncode(fullName);
        var safeVerificationLink = WebUtility.HtmlEncode(verificationLink);
        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <!doctype html>
                <html lang="en">
                <body style="margin:0;padding:0;background:#f6f8fb;font-family:Arial,sans-serif;color:#111827;">
                  <div style="max-width:560px;margin:0 auto;padding:32px 20px;">
                    <div style="background:#ffffff;border:1px solid #e5e7eb;border-radius:12px;padding:28px;">
                      <h2 style="margin:0 0 16px;color:#111827;font-size:22px;">Verify your Mascoteach email</h2>
                      <p style="margin:0 0 12px;line-height:1.6;">Hello {safeFullName},</p>
                      <p style="margin:0 0 20px;line-height:1.6;">Please verify your email address to activate your Mascoteach account.</p>
                      <p style="margin:0 0 24px;">
                        <a href="{safeVerificationLink}" style="display:inline-block;padding:12px 20px;background:#1f2937;color:#ffffff;text-decoration:none;border-radius:8px;font-weight:700;">Verify email</a>
                      </p>
                      <p style="margin:0 0 12px;line-height:1.6;color:#4b5563;">This link expires after a limited time.</p>
                      <p style="margin:0;line-height:1.6;color:#4b5563;">If you did not create a Mascoteach account, you can ignore this email.</p>
                    </div>
                  </div>
                </body>
                </html>
                """,
            TextBody = $"Hello {fullName},\n\nPlease verify your email address to activate your Mascoteach account.\nOpen this link: {verificationLink}\n\nThis link expires after a limited time. If you did not create a Mascoteach account, you can ignore this email."
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            await client.AuthenticateAsync(username, password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
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
        message.Subject = "Đặt lại mật khẩu Mascoteach";

        var safeFullName = WebUtility.HtmlEncode(fullName);
        var safeResetLink = WebUtility.HtmlEncode(resetLink);
        var builder = new BodyBuilder
        {
            HtmlBody = $"""
                <!doctype html>
                <html lang="vi">
                <body style="margin:0;padding:0;background:#f6f8fb;font-family:Arial,sans-serif;color:#111827;">
                  <div style="max-width:560px;margin:0 auto;padding:32px 20px;">
                    <div style="background:#ffffff;border:1px solid #e5e7eb;border-radius:12px;padding:28px;">
                      <h2 style="margin:0 0 16px;color:#111827;font-size:22px;">Đặt lại mật khẩu Mascoteach</h2>
                      <p style="margin:0 0 12px;line-height:1.6;">Xin chào {safeFullName},</p>
                      <p style="margin:0 0 20px;line-height:1.6;">Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản Mascoteach. Nhấn nút bên dưới để tiếp tục.</p>
                      <p style="margin:0 0 24px;">
                        <a href="{safeResetLink}" style="display:inline-block;padding:12px 20px;background:#1f2937;color:#ffffff;text-decoration:none;border-radius:8px;font-weight:700;">Đặt lại mật khẩu</a>
                      </p>
                      <p style="margin:0 0 12px;line-height:1.6;color:#4b5563;">Liên kết này sẽ hết hạn sau một thời gian ngắn.</p>
                      <p style="margin:0;line-height:1.6;color:#4b5563;">Nếu bạn không yêu cầu đặt lại mật khẩu, bạn có thể bỏ qua email này.</p>
                    </div>
                  </div>
                </body>
                </html>
                """,
            TextBody = $"Xin chào {fullName},\n\nBạn vừa yêu cầu đặt lại mật khẩu cho tài khoản Mascoteach.\nMở liên kết sau để tiếp tục: {resetLink}\n\nLiên kết này sẽ hết hạn sau một thời gian ngắn. Nếu bạn không yêu cầu đặt lại mật khẩu, bạn có thể bỏ qua email này."
        };
        message.Body = builder.ToMessageBody();

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
