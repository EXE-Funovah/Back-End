namespace Mascoteach.Service.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string fullName, string resetLink);
    Task SendEmailVerificationAsync(string email, string fullName, string verificationLink);
}
