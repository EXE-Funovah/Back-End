using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mascoteach.Service.DTOs;

public class DocumentCreateRequest
{
    [Required]
    public string FileUrl { get; set; } = null!;
}