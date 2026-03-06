using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class GameTemplateUpdateRequest
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string JsBundleUrl { get; set; } = null!;

    public string? ThumbnailUrl { get; set; }
}
