using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mascoteach.Service.DTOs;

public class DocumentCreateRequest
{
    [Required]
    public string S3Key { get; set; } = null!;
}