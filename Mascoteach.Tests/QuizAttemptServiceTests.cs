using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Mascoteach.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class QuizAttemptServiceTests
{
    private readonly Mock<IQuizAttemptRepository> _attemptRepo = new();
    private readonly Mock<IQuizRepository> _quizRepo = new();
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<IQuestionRepository> _questionRepo = new();
    private readonly Mock<IOptionRepository> _optionRepo = new();
    private readonly Mock<IUserStatService> _userStatService = new();
    private readonly QuizAttemptService _sut;

    public QuizAttemptServiceTests()
    {
        _sut = new QuizAttemptService(
            _attemptRepo.Object,
            _quizRepo.Object,
            _documentRepo.Object,
            _questionRepo.Object,
            _optionRepo.Object,
            _userStatService.Object);
    }

    private static Quiz MakeQuiz() => new()
    {
        Id = 7,
        DocumentId = 3,
        Title = "Practice quiz",
        Status = "AI_Drafted"
    };

    private static Document MakeDocument(int ownerId = 10) => new()
    {
        Id = 3,
        TeacherId = ownerId,
        FileUrl = "documents/2026/06/11/sample.zip"
    };

    private static List<Question> MakeQuestions() => new()
    {
        new() { Id = 101, QuizId = 7, QuestionText = "Q1", QuestionType = "MultipleChoice" },
        new() { Id = 102, QuizId = 7, QuestionText = "Q2", QuestionType = "MultipleChoice" }
    };

    private static List<Option> OptionsFor(int questionId) => questionId switch
    {
        101 => new List<Option>
        {
            new() { Id = 1001, QuestionId = 101, OptionText = "Wrong", IsCorrect = false },
            new() { Id = 1002, QuestionId = 101, OptionText = "Right", IsCorrect = true }
        },
        102 => new List<Option>
        {
            new() { Id = 2001, QuestionId = 102, OptionText = "Right", IsCorrect = true },
            new() { Id = 2002, QuestionId = 102, OptionText = "Wrong", IsCorrect = false }
        },
        _ => new List<Option>()
    };

    private void SetupOwnedQuiz(int ownerId = 10)
    {
        _quizRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(MakeQuiz());
        _documentRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(MakeDocument(ownerId));
        _questionRepo.Setup(r => r.GetByQuizIdAsync(7)).ReturnsAsync(MakeQuestions());
        _optionRepo.Setup(r => r.GetByQuestionIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int questionId) => OptionsFor(questionId));
        _attemptRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(new Mock<IDbContextTransaction>().Object);
        _attemptRepo.Setup(r => r.AddAsync(It.IsAny<QuizAttempt>()))
            .Callback<QuizAttempt>(a => a.Id = 55)
            .Returns(Task.CompletedTask);
        _attemptRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _userStatService.Setup(s => s.ApplyAttemptAsync(10, It.IsAny<QuizAttempt>()))
            .ReturnsAsync((int userId, QuizAttempt attempt) => new UserStat
            {
                UserId = userId,
                Xp = attempt.XpEarned,
                CurrentStreak = 1,
                LongestStreak = 1,
                LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TotalCorrectAnswers = attempt.CorrectCount,
                TotalQuestionsAnswered = attempt.TotalQuestions,
                TotalLearningSeconds = attempt.DurationSeconds
            });
    }

    [Fact]
    public async Task SubmitAsync_ScoresAnswersOnServerAndUpdatesStats()
    {
        SetupOwnedQuiz();
        QuizAttempt? savedAttempt = null;
        _attemptRepo.Setup(r => r.AddAsync(It.IsAny<QuizAttempt>()))
            .Callback<QuizAttempt>(a =>
            {
                a.Id = 55;
                savedAttempt = a;
            })
            .Returns(Task.CompletedTask);

        var result = await _sut.SubmitAsync(10, new QuizAttemptSubmitRequest
        {
            QuizId = 7,
            DurationSeconds = 30,
            Answers = new List<QuizAnswerSubmitRequest>
            {
                new() { QuestionId = 101, OptionId = 1002 },
                new() { QuestionId = 102, OptionId = 2002 }
            }
        });

        Assert.NotNull(savedAttempt);
        Assert.Equal(1, savedAttempt!.CorrectCount);
        Assert.Equal(2, savedAttempt.TotalQuestions);
        Assert.Equal(30, savedAttempt.DurationSeconds);
        Assert.Equal(30, savedAttempt.XpEarned);
        Assert.Equal(1, result.CorrectCount);
        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(30, result.XpEarned);
        Assert.NotNull(result.Stats);
    }

    [Fact]
    public async Task SubmitAsync_RejectsQuizOwnedByAnotherUser()
    {
        SetupOwnedQuiz(ownerId: 99);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.SubmitAsync(10, new QuizAttemptSubmitRequest
        {
            QuizId = 7,
            DurationSeconds = 30,
            Answers = new List<QuizAnswerSubmitRequest>
            {
                new() { QuestionId = 101, OptionId = 1002 },
                new() { QuestionId = 102, OptionId = 2001 }
            }
        }));

        _attemptRepo.Verify(r => r.AddAsync(It.IsAny<QuizAttempt>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_RejectsAnswerForQuestionOutsideQuiz()
    {
        SetupOwnedQuiz();

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitAsync(10, new QuizAttemptSubmitRequest
        {
            QuizId = 7,
            DurationSeconds = 30,
            Answers = new List<QuizAnswerSubmitRequest>
            {
                new() { QuestionId = 101, OptionId = 1002 },
                new() { QuestionId = 999, OptionId = 9001 }
            }
        }));
    }

    [Fact]
    public async Task SubmitAsync_RejectsOptionOutsideQuestion()
    {
        SetupOwnedQuiz();

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitAsync(10, new QuizAttemptSubmitRequest
        {
            QuizId = 7,
            DurationSeconds = 30,
            Answers = new List<QuizAnswerSubmitRequest>
            {
                new() { QuestionId = 101, OptionId = 2001 },
                new() { QuestionId = 102, OptionId = 2002 }
            }
        }));
    }

    [Fact]
    public async Task SubmitAsync_RejectsDuplicateAnswers()
    {
        SetupOwnedQuiz();

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitAsync(10, new QuizAttemptSubmitRequest
        {
            QuizId = 7,
            DurationSeconds = 30,
            Answers = new List<QuizAnswerSubmitRequest>
            {
                new() { QuestionId = 101, OptionId = 1002 },
                new() { QuestionId = 101, OptionId = 1001 }
            }
        }));
    }
}
