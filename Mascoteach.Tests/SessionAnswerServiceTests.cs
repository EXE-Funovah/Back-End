using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class SessionAnswerServiceTests
{
    private readonly Mock<ILiveSessionRepository> _sessionRepository = new();
    private readonly Mock<ISessionParticipantRepository> _participantRepository = new();
    private readonly Mock<IQuestionRepository> _questionRepository = new();
    private readonly Mock<IOptionRepository> _optionRepository = new();
    private readonly Mock<ISessionAnswerRepository> _answerRepository = new();
    private readonly SessionAnswerService _sut;

    public SessionAnswerServiceTests()
    {
        _sut = new SessionAnswerService(
            _sessionRepository.Object,
            _participantRepository.Object,
            _questionRepository.Object,
            _optionRepository.Object,
            _answerRepository.Object,
            TimeProvider.System);
    }

    [Fact]
    public async Task SubmitAsync_CorrectAnswer_PersistsAnswerAndAddsScore()
    {
        ArrangeValidSubmission(isCorrect: true);
        SessionAnswer? capturedAnswer = null;
        _answerRepository
            .Setup(repository => repository.AddAsync(It.IsAny<SessionAnswer>()))
            .Callback<SessionAnswer>(answer => capturedAnswer = answer)
            .Returns(Task.CompletedTask);
        _answerRepository.Setup(repository => repository.SaveChangesAsync()).ReturnsAsync(2);

        var result = await _sut.SubmitAsync(MakeRequest());

        Assert.True(result.Accepted);
        Assert.True(result.IsCorrect);
        Assert.Equal(1000, result.ScoreAwarded);
        Assert.Equal(1250, result.TotalScore);
        Assert.NotNull(capturedAnswer);
        Assert.True(capturedAnswer!.IsCorrect);
        Assert.Equal(1000, capturedAnswer.ScoreAwarded);
        _participantRepository.Verify(
            repository => repository.Update(It.Is<SessionParticipant>(participant =>
                participant.TotalScore == 1250)),
            Times.Once);
        _answerRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WrongAnswer_PersistsAnswerWithoutAddingScore()
    {
        ArrangeValidSubmission(isCorrect: false);
        _answerRepository.Setup(repository => repository.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.SubmitAsync(MakeRequest());

        Assert.True(result.Accepted);
        Assert.False(result.IsCorrect);
        Assert.Equal(0, result.ScoreAwarded);
        Assert.Equal(250, result.TotalScore);
        _participantRepository.Verify(
            repository => repository.Update(It.Is<SessionParticipant>(participant =>
                participant.TotalScore == 250)),
            Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_ExistingAnswer_DoesNotAwardScoreAgain()
    {
        ArrangeValidSubmission(isCorrect: true);
        _answerRepository
            .Setup(repository => repository.GetByParticipantAndQuestionAsync(10, 20, 30))
            .ReturnsAsync(new SessionAnswer
            {
                Id = 1,
                SessionId = 10,
                ParticipantId = 20,
                QuestionId = 30,
                SelectedOptionId = 40,
                IsCorrect = true,
                ScoreAwarded = 1000,
                AnsweredAt = DateTime.UtcNow
            });

        var result = await _sut.SubmitAsync(MakeRequest());

        Assert.False(result.Accepted);
        Assert.True(result.AlreadyAnswered);
        Assert.Equal("ALREADY_ANSWERED", result.ErrorCode);
        _answerRepository.Verify(
            repository => repository.AddAsync(It.IsAny<SessionAnswer>()),
            Times.Never);
        _participantRepository.Verify(
            repository => repository.Update(It.IsAny<SessionParticipant>()),
            Times.Never);
        _answerRepository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_InactiveSession_IsRejected()
    {
        _sessionRepository
            .Setup(repository => repository.GetByPinAsync("123456"))
            .ReturnsAsync(new LiveSession
            {
                Id = 10,
                QuizId = 5,
                GamePin = "123456",
                Status = "Waiting"
            });

        var result = await _sut.SubmitAsync(MakeRequest());

        Assert.False(result.Accepted);
        Assert.Equal("SESSION_NOT_ACTIVE", result.ErrorCode);
        _participantRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_ParticipantFromAnotherSession_IsRejected()
    {
        _sessionRepository
            .Setup(repository => repository.GetByPinAsync("123456"))
            .ReturnsAsync(MakeSession());
        _participantRepository
            .Setup(repository => repository.GetByIdAsync(20))
            .ReturnsAsync(new SessionParticipant
            {
                Id = 20,
                SessionId = 999,
                StudentName = "Minh"
            });

        var result = await _sut.SubmitAsync(MakeRequest());

        Assert.False(result.Accepted);
        Assert.Equal("PARTICIPANT_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task SubmitAsync_OptionFromAnotherQuestion_IsRejected()
    {
        ArrangeValidSubmission(isCorrect: true, optionQuestionId: 999);

        var result = await _sut.SubmitAsync(MakeRequest());

        Assert.False(result.Accepted);
        Assert.Equal("OPTION_NOT_FOUND", result.ErrorCode);
        _answerRepository.Verify(
            repository => repository.AddAsync(It.IsAny<SessionAnswer>()),
            Times.Never);
    }

    private void ArrangeValidSubmission(bool isCorrect, int optionQuestionId = 30)
    {
        _sessionRepository
            .Setup(repository => repository.GetByPinAsync("123456"))
            .ReturnsAsync(MakeSession());
        _participantRepository
            .Setup(repository => repository.GetByIdAsync(20))
            .ReturnsAsync(new SessionParticipant
            {
                Id = 20,
                SessionId = 10,
                StudentName = "Minh",
                TotalScore = 250
            });
        _questionRepository
            .Setup(repository => repository.GetVisibleByIdAsync(30))
            .ReturnsAsync(new Question
            {
                Id = 30,
                QuizId = 5,
                QuestionText = "Question",
                QuestionType = "MultipleChoice"
            });
        _answerRepository
            .Setup(repository => repository.GetByParticipantAndQuestionAsync(10, 20, 30))
            .ReturnsAsync((SessionAnswer?)null);
        _optionRepository
            .Setup(repository => repository.GetVisibleByIdAsync(40))
            .ReturnsAsync(new Option
            {
                Id = 40,
                QuestionId = optionQuestionId,
                OptionText = "Option",
                IsCorrect = isCorrect
            });
    }

    private static LiveSession MakeSession()
    {
        return new LiveSession
        {
            Id = 10,
            QuizId = 5,
            TeacherId = 1,
            TemplateId = 1,
            GamePin = "123456",
            Status = "Active"
        };
    }

    private static SubmitSessionAnswerRequest MakeRequest()
    {
        return new SubmitSessionAnswerRequest
        {
            GamePin = "123456",
            ParticipantId = 20,
            QuestionId = 30,
            SelectedOptionId = 40
        };
    }
}
