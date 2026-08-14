namespace Mascoteach.Service.DTOs;

public sealed class LiveSessionReportResponse
{
    public int SessionId { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = null!;
    public string GamePin { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalParticipants { get; set; }
    public int TotalAnswers { get; set; }
    public int CorrectAnswers { get; set; }
    public decimal CorrectRate { get; set; }
    public decimal AverageScore { get; set; }
    public IReadOnlyList<LiveSessionParticipantReportResponse> Participants { get; set; }
        = Array.Empty<LiveSessionParticipantReportResponse>();
    public IReadOnlyList<LiveSessionQuestionReportResponse> Questions { get; set; }
        = Array.Empty<LiveSessionQuestionReportResponse>();
}

public sealed class LiveSessionParticipantReportResponse
{
    public int Rank { get; set; }
    public int ParticipantId { get; set; }
    public int? StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public int TotalScore { get; set; }
    public int AnsweredCount { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public decimal AccuracyRate { get; set; }
    public IReadOnlyList<LiveSessionAnswerReportResponse> Answers { get; set; }
        = Array.Empty<LiveSessionAnswerReportResponse>();
}

public sealed class LiveSessionAnswerReportResponse
{
    public int QuestionId { get; set; }
    public int Position { get; set; }
    public string QuestionText { get; set; } = null!;
    public int SelectedOptionId { get; set; }
    public string SelectedOptionText { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int ScoreAwarded { get; set; }
    public DateTime AnsweredAt { get; set; }
}

public sealed class LiveSessionQuestionReportResponse
{
    public int QuestionId { get; set; }
    public int Position { get; set; }
    public string QuestionText { get; set; } = null!;
    public int AnsweredCount { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public int UnansweredCount { get; set; }
    public decimal CorrectRate { get; set; }
}
