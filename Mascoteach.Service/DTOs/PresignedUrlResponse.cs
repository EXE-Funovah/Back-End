namespace Mascoteach.Service.DTOs;

public class PresignedUrlResponse
{
    public string UploadUrl { get; set; } = null!;
    public string S3Key { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
