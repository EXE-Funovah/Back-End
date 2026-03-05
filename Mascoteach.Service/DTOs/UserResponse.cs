namespace Mascoteach.Service.DTOs;

public class UserResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string SubscriptionTier { get; set; } = null!;
    public int? DocumentsProcessed { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
