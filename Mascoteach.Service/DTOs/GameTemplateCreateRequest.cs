using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class GameTemplateCreateRequest
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string JsBundleUrl { get; set; } = null!;

    public string? ThumbnailUrl { get; set; }
}
