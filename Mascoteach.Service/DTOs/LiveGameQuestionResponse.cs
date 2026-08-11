namespace Mascoteach.Service.DTOs;

public sealed class LiveGameQuestionResponse
{
    public int QuestionId { get; init; }
    public string QuestionText { get; init; } = null!;
    public int Position { get; init; }
    public int TotalQuestions { get; init; }
    public IReadOnlyList<LiveGameOptionResponse> Options { get; init; } = [];
}

public sealed class LiveGameOptionResponse
{
    public int OptionId { get; init; }
    public string Text { get; init; } = null!;
}
