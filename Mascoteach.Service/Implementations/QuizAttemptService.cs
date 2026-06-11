using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class QuizAttemptService : IQuizAttemptService
{
    private readonly IQuizAttemptRepository _attemptRepository;
    private readonly IQuizRepository _quizRepository;
    private readonly IUserStatService _userStatService;

    public QuizAttemptService(
        IQuizAttemptRepository attemptRepository,
        IQuizRepository quizRepository,
        IUserStatService userStatService)
    {
        _attemptRepository = attemptRepository;
        _quizRepository = quizRepository;
        _userStatService = userStatService;
    }

    public async Task<QuizAttemptResponse> SubmitAsync(int userId, QuizAttemptSubmitRequest request)
    {
        if (request.CorrectCount > request.TotalQuestions)
            throw new ArgumentException("CorrectCount cannot exceed TotalQuestions.");

        var quiz = await _quizRepository.GetByIdAsync(request.QuizId)
            ?? throw new KeyNotFoundException($"Quiz {request.QuizId} not found.");

        var attempt = new QuizAttempt
        {
            UserId = userId,
            QuizId = quiz.Id,
            CorrectCount = request.CorrectCount,
            TotalQuestions = request.TotalQuestions,
            DurationSeconds = request.DurationSeconds,
            XpEarned = ComputeXp(request),
            CompletedAt = DateTime.UtcNow,
        };

        using var transaction = await _attemptRepository.BeginTransactionAsync();
        try
        {
            await _attemptRepository.AddAsync(attempt);
            await _attemptRepository.SaveChangesAsync();

            var stat = await _userStatService.ApplyAttemptAsync(userId, attempt);

            await transaction.CommitAsync();

            return ToResponse(attempt, UserStatService.ToResponse(stat));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<QuizAttemptResponse>> GetMineAsync(int userId, DateTime? from, DateTime? to)
    {
        var attempts = await _attemptRepository.GetByUserIdAsync(userId, from, to);
        return attempts.Select(a => ToResponse(a, null));
    }

    /// <summary>
    /// XP = đúng × 10, +50 nếu perfect, +20 nếu nhanh hơn 20s/câu.
    /// </summary>
    private static int ComputeXp(QuizAttemptSubmitRequest request)
    {
        var xp = request.CorrectCount * 10;
        if (request.TotalQuestions > 0 && request.CorrectCount == request.TotalQuestions)
            xp += 50;
        var expectedSeconds = request.TotalQuestions * 20;
        if (request.DurationSeconds > 0 && request.DurationSeconds < expectedSeconds)
            xp += 20;
        return xp;
    }

    private static QuizAttemptResponse ToResponse(QuizAttempt attempt, UserStatResponse? stats) => new()
    {
        Id = attempt.Id,
        QuizId = attempt.QuizId,
        CorrectCount = attempt.CorrectCount,
        TotalQuestions = attempt.TotalQuestions,
        DurationSeconds = attempt.DurationSeconds,
        XpEarned = attempt.XpEarned,
        CompletedAt = attempt.CompletedAt,
        Stats = stats,
    };
}
