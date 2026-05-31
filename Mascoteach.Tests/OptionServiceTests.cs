using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class OptionServiceTests
{
    private readonly Mock<IOptionRepository> _optionRepo = new();
    private readonly Mock<IQuestionRepository> _questionRepo = new();
    private readonly Mock<IQuizRepository> _quizRepo = new();
    private readonly Mock<IDocumentRepository> _docRepo = new();
    private readonly IMapper _mapper = TestHelper.CreateMapper();
    private readonly OptionService _sut;

    public OptionServiceTests()
    {
        _sut = new OptionService(
            _optionRepo.Object, _questionRepo.Object,
            _quizRepo.Object, _docRepo.Object, _mapper);
    }

    private Document MakeDoc(int teacherId = 10) => new() { Id = 1, TeacherId = teacherId, FileUrl = "k" };
    private Quiz MakeQuiz() => new() { Id = 1, DocumentId = 1, Title = "Q", Status = "AI_Drafted" };
    private Question MakeQuestion() => new() { Id = 1, QuizId = 1, QuestionText = "What?" };
    private Option MakeOption(int id = 1) => new() { Id = id, QuestionId = 1, OptionText = "A", IsCorrect = true };

    private void SetupOwnership(int teacherId = 10)
    {
        _questionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeQuestion());
        _quizRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeQuiz());
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc(teacherId));
    }

    // ── CreateAsync ──

    [Fact]
    public async Task CreateAsync_OwnerTeacher_Succeeds()
    {
        SetupOwnership();
        _optionRepo.Setup(r => r.AddAsync(It.IsAny<Option>())).Returns(Task.CompletedTask);
        _optionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(10, new OptionCreateRequest
        {
            QuestionId = 1, OptionText = "Answer A", IsCorrect = true
        });

        Assert.NotNull(result);
        Assert.Equal("Answer A", result.OptionText);
    }

    [Fact]
    public async Task CreateAsync_WrongTeacher_Throws()
    {
        SetupOwnership(teacherId: 99);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateAsync(10, new OptionCreateRequest { QuestionId = 1, OptionText = "A" }));
    }

    // ── UpdateAsync ──

    [Fact]
    public async Task UpdateAsync_OwnerTeacher_ReturnsTrue()
    {
        SetupOwnership();
        _optionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOption());
        _optionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.UpdateAsync(1, 10, new OptionUpdateRequest { OptionText = "B", IsCorrect = false }));
    }

    [Fact]
    public async Task UpdateAsync_WrongTeacher_ReturnsFalse()
    {
        SetupOwnership(teacherId: 99);
        _optionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOption());

        Assert.False(await _sut.UpdateAsync(1, 10, new OptionUpdateRequest { OptionText = "B" }));
    }

    // ── DeleteAsync ──

    [Fact]
    public async Task DeleteAsync_OwnerTeacher_ReturnsTrue()
    {
        SetupOwnership();
        _optionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOption());
        _optionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.DeleteAsync(1, 10));
    }

    [Fact]
    public async Task DeleteAsync_WrongTeacher_ReturnsFalse()
    {
        SetupOwnership(teacherId: 99);
        _optionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOption());

        Assert.False(await _sut.DeleteAsync(1, 10));
    }

    // ── ToggleDeleteAsync ──

    [Fact]
    public async Task ToggleDeleteAsync_OwnerTeacher_Toggles()
    {
        SetupOwnership();
        var opt = MakeOption();
        opt.IsDeleted = false;
        _optionRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(opt);
        _optionRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ToggleDeleteAsync(1, 10);

        Assert.NotNull(result);
        Assert.True(result!.IsDeleted);
    }

    [Fact]
    public async Task ToggleDeleteAsync_WrongTeacher_ReturnsNull()
    {
        SetupOwnership(teacherId: 99);
        _optionRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(MakeOption());

        Assert.Null(await _sut.ToggleDeleteAsync(1, 10));
    }

    [Fact]
    public async Task ToggleDeleteAsync_NotFound_ReturnsNull()
    {
        _optionRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync((Option?)null);

        Assert.Null(await _sut.ToggleDeleteAsync(1, 10));
    }
}
