namespace Mascoteach.Service.DTOs;

public class GoogleUserInfo
{
    public string Subject { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public bool EmailVerified { get; set; }
}
