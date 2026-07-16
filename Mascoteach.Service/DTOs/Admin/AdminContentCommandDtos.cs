using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs.Admin;

public class AdminContentModerationRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = null!;
}

public class AdminDocumentModerationResponse
{
    public int DocumentId { get; set; }
    public bool IsDeleted { get; set; }
    public bool Changed { get; set; }
}

public enum AdminDocumentModerationStatus
{
    Updated,
    NoChange,
    DocumentNotFound
}

public class AdminDocumentModerationResult
{
    public AdminDocumentModerationStatus Status { get; set; }
    public AdminDocumentModerationResponse? Response { get; set; }
}
