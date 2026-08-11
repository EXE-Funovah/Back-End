using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class LiveSessionServiceTests
{
    private readonly Mock<ILiveSessionRepository> _repo = new();
    private readonly IMapper _mapper = TestHelper.CreateMapper();
    private readonly LiveSessionService _sut;

    public LiveSessionServiceTests()
    {
        _sut = new LiveSessionService(_repo.Object, _mapper);
    }

    private LiveSession MakeSession(int teacherId = 10) => new()
    {
        Id = 1, TeacherId = teacherId, QuizId = 1, TemplateId = 1,
        GamePin = "123456", Status = "Waiting", CreatedAt = DateTime.Now
    };

    // ── UpdateAsync ──

    [Fact]
    public async Task UpdateAsync_OwnerTeacher_ReturnsTrue()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSession());
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.UpdateAsync(1, 10, new LiveSessionUpdateRequest { Status = "Active" }));
    }

    [Fact]
    public async Task UpdateAsync_WrongTeacher_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSession(teacherId: 99));

        Assert.False(await _sut.UpdateAsync(1, 10, new LiveSessionUpdateRequest { Status = "Active" }));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((LiveSession?)null);

        Assert.False(await _sut.UpdateAsync(1, 10, new LiveSessionUpdateRequest { Status = "Active" }));
    }

    // ── DeleteAsync ──

    [Fact]
    public async Task DeleteAsync_OwnerTeacher_ReturnsTrue()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSession());
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.DeleteAsync(1, 10));
    }

    [Fact]
    public async Task DeleteAsync_WrongTeacher_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSession(teacherId: 99));

        Assert.False(await _sut.DeleteAsync(1, 10));
    }

    // ── ToggleDeleteAsync ──

    [Fact]
    public async Task ToggleDeleteAsync_OwnerTeacher_Toggles()
    {
        var session = MakeSession();
        session.IsDeleted = false;
        _repo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(session);
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ToggleDeleteAsync(1, 10);

        Assert.NotNull(result);
        Assert.True(result!.IsDeleted);
    }

    [Fact]
    public async Task ToggleDeleteAsync_WrongTeacher_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(MakeSession(teacherId: 99));

        Assert.Null(await _sut.ToggleDeleteAsync(1, 10));
    }

    // ── UpdateStatusByPinAsync ──

    [Fact]
    public async Task UpdateStatusByPinAsync_ValidPin_UpdatesStatus()
    {
        var session = MakeSession();
        _repo.Setup(r => r.GetByPinAsync("123456")).ReturnsAsync(session);
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.UpdateStatusByPinAsync("123456", "Active"));
        Assert.Equal("Active", session.Status);
    }

    [Fact]
    public async Task UpdateStatusByPinAsync_InvalidPin_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByPinAsync("000000")).ReturnsAsync((LiveSession?)null);

        Assert.False(await _sut.UpdateStatusByPinAsync("000000", "Active"));
    }

    [Fact]
    public async Task UpdateStatusByPinAsync_CannotReturnActiveSessionToWaiting()
    {
        var session = MakeSession();
        session.Status = "Active";
        _repo.Setup(r => r.GetByPinAsync("123456")).ReturnsAsync(session);

        Assert.False(await _sut.UpdateStatusByPinAsync("123456", "Waiting"));
        Assert.Equal("Active", session.Status);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusByPinAsync_ActiveSessionCanEnd()
    {
        var session = MakeSession();
        session.Status = "Active";
        _repo.Setup(r => r.GetByPinAsync("123456")).ReturnsAsync(session);
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.UpdateStatusByPinAsync("123456", "Ended"));
        Assert.Equal("Ended", session.Status);
    }

    // ── GetByPinAsync ──

    [Fact]
    public async Task GetByPinAsync_EndedSession_ReturnsNull()
    {
        var session = MakeSession();
        session.Status = "Ended";
        _repo.Setup(r => r.GetByPinAsync("123456")).ReturnsAsync(session);

        Assert.Null(await _sut.GetByPinAsync("123456"));
    }

    [Fact]
    public async Task GetByPinAsync_ActiveSession_ReturnsResponse()
    {
        var session = MakeSession();
        session.Status = "Active";
        _repo.Setup(r => r.GetByPinAsync("123456")).ReturnsAsync(session);

        var result = await _sut.GetByPinAsync("123456");

        Assert.NotNull(result);
        Assert.Equal("123456", result!.GamePin);
    }

    // GetReportAsync

    [Fact]
    public async Task GetReportAsync_OwnerTeacher_ReturnsAggregatedReport()
    {
        var correctOption = new Option
        {
            Id = 101,
            QuestionId = 11,
            OptionText = "Correct",
            IsCorrect = true
        };
        var incorrectOption = new Option
        {
            Id = 102,
            QuestionId = 11,
            OptionText = "Incorrect",
            IsCorrect = false
        };
        var question = new Question
        {
            Id = 11,
            QuizId = 1,
            QuestionText = "Question 1",
            QuestionType = "MultipleChoice",
            Position = 0
        };
        var firstParticipant = new SessionParticipant
        {
            Id = 21,
            SessionId = 1,
            StudentName = "An",
            TotalScore = 1000
        };
        var secondParticipant = new SessionParticipant
        {
            Id = 22,
            SessionId = 1,
            StudentName = "Binh",
            TotalScore = 0
        };
        firstParticipant.SessionAnswers.Add(new SessionAnswer
        {
            Id = 31,
            SessionId = 1,
            ParticipantId = firstParticipant.Id,
            QuestionId = question.Id,
            SelectedOptionId = correctOption.Id,
            IsCorrect = true,
            ScoreAwarded = 1000,
            AnsweredAt = DateTime.UtcNow,
            Question = question,
            SelectedOption = correctOption
        });
        secondParticipant.SessionAnswers.Add(new SessionAnswer
        {
            Id = 32,
            SessionId = 1,
            ParticipantId = secondParticipant.Id,
            QuestionId = question.Id,
            SelectedOptionId = incorrectOption.Id,
            IsCorrect = false,
            ScoreAwarded = 0,
            AnsweredAt = DateTime.UtcNow,
            Question = question,
            SelectedOption = incorrectOption
        });

        var session = MakeSession();
        session.Status = "Ended";
        session.Quiz = new Quiz
        {
            Id = 1,
            DocumentId = 1,
            Title = "Test quiz",
            Status = "Published",
            ActivityType = "Quiz"
        };
        session.Quiz.Questions.Add(question);
        session.SessionParticipants.Add(firstParticipant);
        session.SessionParticipants.Add(secondParticipant);
        _repo.Setup(repository => repository.GetReportByIdAsync(1)).ReturnsAsync(session);

        var result = await _sut.GetReportAsync(1, 10);

        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalParticipants);
        Assert.Equal(2, result.TotalAnswers);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.Equal(50m, result.CorrectRate);
        Assert.Equal(500m, result.AverageScore);
        Assert.Equal("An", result.Participants[0].StudentName);
        Assert.Equal(1, result.Participants[0].Rank);
        Assert.Equal(2, result.Questions[0].AnsweredCount);
        Assert.Equal(50m, result.Questions[0].CorrectRate);
    }

    [Fact]
    public async Task GetReportAsync_WrongTeacher_ReturnsNull()
    {
        var session = MakeSession(teacherId: 99);
        _repo.Setup(repository => repository.GetReportByIdAsync(1)).ReturnsAsync(session);

        Assert.Null(await _sut.GetReportAsync(1, 10));
    }

    [Fact]
    public async Task GetReportAsync_NoParticipants_ReturnsZeroMetrics()
    {
        var session = MakeSession();
        session.Quiz = new Quiz
        {
            Id = 1,
            DocumentId = 1,
            Title = "Empty quiz",
            Status = "Published",
            ActivityType = "Quiz"
        };
        _repo.Setup(repository => repository.GetReportByIdAsync(1)).ReturnsAsync(session);

        var result = await _sut.GetReportAsync(1, 10);

        Assert.NotNull(result);
        Assert.Equal(0, result!.CorrectRate);
        Assert.Equal(0, result.AverageScore);
        Assert.Empty(result.Participants);
    }
}
