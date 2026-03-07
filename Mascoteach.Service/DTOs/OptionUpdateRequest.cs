namespace Mascoteach.Service.DTOs;

public class OptionUpdateRequest
{
    public string OptionText { get; set; } = null!;
    public bool IsCorrect { get; set; }
}
