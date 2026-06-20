namespace Mascoteach.Data.Models;

// Partial mở rộng cho User — tách khỏi file scaffold để không bị ghi đè khi
// re-scaffold. Map cột ở MascoteachDbContext.Avatar.cs (OnModelCreatingPartial).
public partial class User
{
    /// <summary>
    /// S3 key của ảnh đại diện (vd. "avatars/{guid}.jpg"). Null nếu chưa đặt.
    /// API trả về dưới dạng presigned download URL, KHÔNG lưu URL vào DB.
    /// </summary>
    public string? AvatarUrl { get; set; }
}
