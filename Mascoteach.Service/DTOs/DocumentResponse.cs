namespace Mascoteach.Service.DTOs;

public class DocumentResponse
{
    public int Id { get; set; }
    public string FileUrl { get; set; } = null!;
    public DateTime? UploadedAt { get; set; }
}