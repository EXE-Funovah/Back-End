using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class QuizAttemptService : IQuizAttemptService
{
    private readonly IQuizAttemptRepository _attemptRepository;
    private readonly IQuizRepository _quizRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IOptionRepository _optionRepository;
    private readonly IUserStatService _userStatService;

    public QuizAttemptService(
        IQuizAttemptRepository attemptRepository,
        IQuizRepository quizRepository,
        IDocumentRepository documentRepository,
        IQuestionRepository questionRepository,
        IOptionRepository optionRepository,
        IUserStatService userStatService)
    {
        _attemptRepository = attemptRepository;
        _quizRepository = quizRepository;
        _documentRepository = documentRepository;
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
        _userStatService = userStatService;
    }

    public async Task<QuizAttemptResponse> SubmitAsync(int userId, QuizAttemptSubmitRequest request)
    {
        var quiz = await _quizRepository.GetByIdAsync(request.QuizId)
            ?? throw new KeyNotFoundException($"Quiz {request.QuizId} not found.");

        var document = await _documentRepository.GetByIdAsync(quiz.DocumentId)
            ?? throw new KeyNotFoundException($"Document {quiz.DocumentId} not found.");

        if (document.TeacherId != userId)
            throw new UnauthorizedAccessException("You do not own this quiz.");

        var questions = (await _questionRepository.GetByQuizIdAsync(quiz.Id)).ToList();
        if (questions.Count == 0)
            throw new ArgumentException("Quiz has no questions.");

        if (request.Answers.Count != questions.Count)
            throw new ArgumentException("Answers count must match quiz question count.");

        var distinctQuestionCount = request.Answers.Select(a => a.QuestionId).Distinct().Count();
        if (distinctQuestionCount != request.Answers.Count)
            throw new ArgumentException("Duplicate answers are not allowed.");

        var questionIds = questions.Select(q => q.Id).ToHashSet();
        var correctCount = 0;

        foreach (var answer in request.Answers)
        {
            if (!questionIds.Contains(answer.QuestionId))
                throw new ArgumentException("Answer contains question not in quiz.");

            var options = (await _optionRepository.GetByQuestionIdAsync(answer.QuestionId)).ToList();
            var selectedOption = options.FirstOrDefault(o => o.Id == answer.OptionId);
            if (selectedOption == null)
                throw new ArgumentException("Answer contains option not in question.");

            if (selectedOption.IsCorrect)
                correctCount++;
        }

        var totalQuestions = questions.Count;

        var attempt = new QuizAttempt
        {
            UserId = userId,
            QuizId = quiz.Id,
            CorrectCount = correctCount,
            TotalQuestions = totalQuestions,
            DurationSeconds = request.DurationSeconds,
            XpEarned = ComputeXp(correctCount, totalQuestions, request.DurationSeconds),
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
    private static int ComputeXp(int correctCount, int totalQuestions, int durationSeconds)
    {
        var xp = correctCount * 10;
        if (totalQuestions > 0 && correctCount == totalQuestions)
            xp += 50;
        var expectedSeconds = totalQuestions * 20;
        if (durationSeconds > 0 && durationSeconds < expectedSeconds)
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
