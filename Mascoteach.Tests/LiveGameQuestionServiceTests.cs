using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class LiveGameQuestionServiceTests
{
    private readonly Mock<ILiveSessionRepository> _sessionRepository = new();
    private readonly Mock<IQuestionRepository> _questionRepository = new();
    private readonly Mock<IOptionRepository> _optionRepository = new();
    private readonly LiveGameQuestionService _sut;

    public LiveGameQuestionServiceTests()
    {
        _sut = new LiveGameQuestionService(
            _sessionRepository.Object,
            _questionRepository.Object,
            _optionRepository.Object);
    }

    [Fact]
    public async Task GetForSessionAsync_ValidQuestion_ReturnsSanitizedPayload()
    {
        _sessionRepository.Setup(r => r.GetByPinAsync("123456")).ReturnsAsync(new LiveSession
        {
            Id = 1,
            QuizId = 10,
            Status = "Active",
            GamePin = "123456"
        });
        _questionRepository.Setup(r => r.GetVisibleByIdAsync(20)).ReturnsAsync(new Question
        {
            Id = 20,
            QuizId = 10,
            QuestionText = "2 + 2?",
            QuestionType = "MultipleChoice",
            Position = 0
        });
        _questionRepository.Setup(r => r.GetByQuizIdAsync(10)).ReturnsAsync(
        [
            new Question { Id = 20, QuizId = 10, QuestionText = "Q1", QuestionType = "MultipleChoice" },
            new Question { Id = 21, QuizId = 10, QuestionText = "Q2", QuestionType = "MultipleChoice" }
        ]);
        _optionRepository.Setup(r => r.GetByQuestionIdAsync(20)).ReturnsAsync(
        [
            new Option { Id = 30, QuestionId = 20, OptionText = "4", IsCorrect = true },
            new Option { Id = 31, QuestionId = 20, OptionText = "5", IsCorrect = false }
        ]);

        var result = await _sut.GetForSessionAsync("123456", 20);

        Assert.NotNull(result);
        Assert.Equal(20, result!.QuestionId);
        Assert.Equal(2, result.TotalQuestions);
        Assert.Collection(result.Options,
            option =>
            {
                Assert.Equal(30, option.OptionId);
                Assert.Equal("4", option.Text);
            },
            option =>
            {
                Assert.Equal(31, option.OptionId);
                Assert.Equal("5", option.Text);
            });
        Assert.DoesNotContain(
            result.Options[0].GetType().GetProperties(),
            property => property.Name.Contains("Correct", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetForSessionAsync_QuestionFromAnotherQuiz_ReturnsNull()
    {
        _sessionRepository.Setup(r => r.GetByPinAsync("123456")).ReturnsAsync(new LiveSession
        {
            Id = 1,
            QuizId = 10,
            Status = "Active",
            GamePin = "123456"
        });
        _questionRepository.Setup(r => r.GetVisibleByIdAsync(20)).ReturnsAsync(new Question
        {
            Id = 20,
            QuizId = 999,
            QuestionText = "Other quiz",
            QuestionType = "MultipleChoice"
        });

        Assert.Null(await _sut.GetForSessionAsync("123456", 20));
        _optionRepository.Verify(r => r.GetByQuestionIdAsync(It.IsAny<int>()), Times.Never);
    }
}
