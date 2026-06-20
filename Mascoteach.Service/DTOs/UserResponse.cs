namespace Mascoteach.Service.DTOs;

public class UserResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string SubscriptionTier { get; set; } = null!;
    public DateTime? PremiumExpiresAt { get; set; }
    public int? DocumentsProcessed { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Presigned download URL của ảnh đại diện (null nếu chưa đặt).</summary>
    public string? AvatarUrl { get; set; }
}
