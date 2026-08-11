namespace Mascoteach.Service.DTOs;

public sealed class SubmitSessionAnswerResult
{
    public bool Accepted { get; init; }

    public bool AlreadyAnswered { get; init; }

    public bool? IsCorrect { get; init; }

    public int ScoreAwarded { get; init; }

    public int TotalScore { get; init; }

    public string? StudentName { get; init; }

    public DateTime? AnsweredAt { get; init; }

    public string? ErrorCode { get; init; }

    public string? Message { get; init; }
}
