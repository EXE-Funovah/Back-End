using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class QuestionServiceTests
{
    private readonly Mock<IQuestionRepository> _questionRepo = new();
    private readonly Mock<IOptionRepository> _optionRepo = new();
    private readonly Mock<IQuizRepository> _quizRepo = new();
    private readonly Mock<IDocumentRepository> _docRepo = new();
    private readonly IMapper _mapper = TestHelper.CreateMapper();
    private readonly QuestionService _sut;

    public QuestionServiceTests()
    {
        _sut = new QuestionService(
            _questionRepo.Object, _optionRepo.Object,
            _quizRepo.Object, _docRepo.Object, _mapper);
    }

    private Document MakeDoc(int teacherId = 10) => new() { Id = 1, TeacherId = teacherId, FileUrl = "k" };
    private Quiz MakeQuiz() => new() { Id = 1, DocumentId = 1, Title = "Q", Status = "AI_Drafted" };
    private Question MakeQuestion(int id = 1) => new() { Id = id, QuizId = 1, QuestionText = "What?", QuestionType = "MultipleChoice" };

    private void SetupOwnership(int teacherId = 10)
    {
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeQuiz());
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc(teacherId));
    }

    // ── CreateAsync ──

    [Fact]
    public async Task CreateAsync_OwnerTeacher_Succeeds()
    {
        SetupOwnership();
        var mockTx = new Mock<IDbContextTransaction>();
        _questionRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTx.Object);
        _questionRepo.Setup(r => r.AddAsync(It.IsAny<Question>())).Returns(Task.CompletedTask);
        _questionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _questionRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(MakeQuestion());
        _optionRepo.Setup(r => r.GetByQuestionIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Option>());

        var result = await _sut.CreateAsync(10, new QuestionCreateRequest
        {
            QuizId = 1,
            QuestionText = "What?"
        });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateAsync_WrongTeacher_Throws()
    {
        SetupOwnership(teacherId: 99);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateAsync(10, new QuestionCreateRequest { QuizId = 1, QuestionText = "What?" }));
    }

    // ── UpdateAsync ──

    [Fact]
    public async Task UpdateAsync_OwnerTeacher_ReturnsTrue()
    {
        SetupOwnership();
        _questionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeQuestion());
        _questionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.UpdateAsync(1, 10, new QuestionUpdateRequest { QuestionText = "New?" }));
    }

    [Fact]
    public async Task UpdateAsync_WrongTeacher_ReturnsFalse()
    {
        SetupOwnership(teacherId: 99);
        _questionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeQuestion());

        Assert.False(await _sut.UpdateAsync(1, 10, new QuestionUpdateRequest { QuestionText = "New?" }));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFalse()
    {
        _questionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Question?)null);

        Assert.False(await _sut.UpdateAsync(1, 10, new QuestionUpdateRequest { QuestionText = "New?" }));
    }

    // ── DeleteAsync ──

    [Fact]
    public async Task DeleteAsync_OwnerTeacher_ReturnsTrue()
    {
        SetupOwnership();
        _questionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeQuestion());
        _questionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.DeleteAsync(1, 10));
    }

    [Fact]
    public async Task DeleteAsync_WrongTeacher_ReturnsFalse()
    {
        SetupOwnership(teacherId: 99);
        _questionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeQuestion());

        Assert.False(await _sut.DeleteAsync(1, 10));
    }

    // ── ToggleDeleteAsync ──

    [Fact]
    public async Task ToggleDeleteAsync_OwnerTeacher_Toggles()
    {
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeQuiz());
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());
        var q = MakeQuestion();
        q.IsDeleted = false;
        _questionRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(q);
        _questionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ToggleDeleteAsync(1, 10);

        Assert.NotNull(result);
        Assert.True(result!.IsDeleted);
    }

    [Fact]
    public async Task ToggleDeleteAsync_WrongTeacher_ReturnsNull()
    {
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeQuiz());
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc(teacherId: 99));
        _questionRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(MakeQuestion());

        Assert.Null(await _sut.ToggleDeleteAsync(1, 10));
    }
}
