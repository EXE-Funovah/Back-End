namespace Mascoteach.Service.DTOs;

public class DocumentResponse
{
    public int Id { get; set; }
    public string S3Key { get; set; } = null!;
    public string PresignedUrl { get; set; } = null!;
    public DateTime? UploadedAt { get; set; }
    public bool IsDeleted { get; set; }
}