namespace Mascoteach.Service.DTOs;

public class OptionResponse
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string OptionText { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public bool IsDeleted { get; set; }
}
