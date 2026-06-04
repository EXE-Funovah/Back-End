namespace Mascoteach.Service.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string fullName, string resetLink);
}
