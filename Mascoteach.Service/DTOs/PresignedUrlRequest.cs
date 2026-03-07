using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class PresignedUrlRequest
{
    [Required]
    public string FileName { get; set; } = null!;
    
    [Required]
    public string ContentType { get; set; } = null!;
}
