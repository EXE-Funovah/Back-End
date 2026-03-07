namespace Mascoteach.Service.DTOs;

public class QuestionUpdateRequest
{
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = "MultipleChoice";
}
