using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class QuizServiceTests
{
    private readonly Mock<IQuizRepository> _quizRepo = new();
    private readonly Mock<IDocumentRepository> _docRepo = new();
    private readonly IMapper _mapper = TestHelper.CreateMapper();
    private readonly QuizService _sut;

    public QuizServiceTests()
    {
        _sut = new QuizService(_quizRepo.Object, _docRepo.Object, _mapper);
    }

    // ── Helpers ──

    private Document MakeDoc(int id = 1, int teacherId = 10)
        => new() { Id = id, TeacherId = teacherId, FileUrl = "key.pdf" };

    private Quiz MakeQuiz(int id = 1, int docId = 1)
        => new() { Id = id, DocumentId = docId, Title = "Quiz 1", Status = "AI_Drafted" };

    // ── CreateAsync ──

    [Fact]
    public async Task CreateAsync_OwnerTeacher_ReturnsQuiz()
    {
        var doc = MakeDoc();
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);
        _quizRepo.Setup(r => r.AddAsync(It.IsAny<Quiz>())).Returns(Task.CompletedTask);
        _quizRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(10, new QuizCreateRequest { DocumentId = 1, Title = "Test" });

        Assert.NotNull(result);
        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task CreateAsync_WrongTeacher_Throws()
    {
        var doc = MakeDoc(teacherId: 99);
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateAsync(10, new QuizCreateRequest { DocumentId = 1, Title = "Test" }));
    }

    [Fact]
    public async Task CreateAsync_DocNotFound_Throws()
    {
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Document?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateAsync(10, new QuizCreateRequest { DocumentId = 1, Title = "Test" }));
    }

    // ── UpdateAsync ──

    [Fact]
    public async Task UpdateAsync_OwnerTeacher_ReturnsTrue()
    {
        var quiz = MakeQuiz();
        var doc = MakeDoc();
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);
        _quizRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, 10, new QuizUpdateRequest { Title = "New", Status = "Published" });

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateAsync_WrongTeacher_ReturnsFalse()
    {
        var quiz = MakeQuiz();
        var doc = MakeDoc(teacherId: 99);
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);

        var result = await _sut.UpdateAsync(1, 10, new QuizUpdateRequest { Title = "New", Status = "Published" });

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_QuizNotFound_ReturnsFalse()
    {
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Quiz?)null);

        var result = await _sut.UpdateAsync(1, 10, new QuizUpdateRequest { Title = "New", Status = "Published" });

        Assert.False(result);
    }

    // ── DeleteAsync ──

    [Fact]
    public async Task DeleteAsync_OwnerTeacher_ReturnsTrue()
    {
        var quiz = MakeQuiz();
        var doc = MakeDoc();
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);
        _quizRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.DeleteAsync(1, 10));
    }

    [Fact]
    public async Task DeleteAsync_WrongTeacher_ReturnsFalse()
    {
        var quiz = MakeQuiz();
        var doc = MakeDoc(teacherId: 99);
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);

        Assert.False(await _sut.DeleteAsync(1, 10));
    }

    // ── ToggleDeleteAsync ──

    [Fact]
    public async Task ToggleDeleteAsync_OwnerTeacher_TogglesAndReturns()
    {
        var quiz = MakeQuiz();
        quiz.IsDeleted = false;
        var doc = MakeDoc();
        _quizRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(quiz);
        _docRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(doc);
        _quizRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ToggleDeleteAsync(1, 10);

        Assert.NotNull(result);
        Assert.True(result!.IsDeleted);
    }

    [Fact]
    public async Task ToggleDeleteAsync_WrongTeacher_ReturnsNull()
    {
        var quiz = MakeQuiz();
        var doc = MakeDoc(teacherId: 99);
        _quizRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(quiz);
        _docRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(doc);

        Assert.Null(await _sut.ToggleDeleteAsync(1, 10));
    }

    // ── GetByIdAsync ──

    [Fact]
    public async Task GetByIdAsync_Exists_ReturnsResponse()
    {
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeQuiz());

        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Quiz?)null);

        Assert.Null(await _sut.GetByIdAsync(1));
    }
}
