namespace Mascoteach.Service.DTOs;

/// <summary>
/// Body cho PATCH /api/User/avatar — lưu S3 key của ảnh đã upload.
/// Gửi null/rỗng để gỡ avatar.
/// </summary>
public class AvatarUpdateRequest
{
    public string? AvatarUrl { get; set; }
}
