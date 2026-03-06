namespace Mascoteach.Service.DTOs;

public class GameTemplateResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string JsBundleUrl { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public bool IsDeleted { get; set; }
}
