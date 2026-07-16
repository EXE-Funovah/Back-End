using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Microsoft.EntityFrameworkCore.Storage;
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

    private Document MakeDoc(int id = 1, int ownerId = 10)
        => new() { Id = id, OwnerId = ownerId, FileUrl = "key.pdf" };

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
    public async Task CreateAsync_ActivityTypeMissing_DefaultsToQuiz()
    {
        Quiz? savedQuiz = null;
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());
        _quizRepo.Setup(r => r.AddAsync(It.IsAny<Quiz>()))
            .Callback<Quiz>(quiz => savedQuiz = quiz)
            .Returns(Task.CompletedTask);
        _quizRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.CreateAsync(10, new QuizCreateRequest { DocumentId = 1, Title = "Test" });

        Assert.NotNull(savedQuiz);
        Assert.Equal("Quiz", savedQuiz!.ActivityType);
    }

    [Fact]
    public async Task CreateAsync_WrongTeacher_Throws()
    {
        var doc = MakeDoc(ownerId: 99);
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

    [Fact]
    public async Task CreateAsync_InvalidActivityType_ThrowsBeforeSaving()
    {
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(10, new QuizCreateRequest
        {
            DocumentId = 1,
            Title = "Test",
            ActivityType = "Unknown"
        }));

        _quizRepo.Verify(r => r.AddAsync(It.IsAny<Quiz>()), Times.Never);
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
        var doc = MakeDoc(ownerId: 99);
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
        var doc = MakeDoc(ownerId: 99);
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
        var doc = MakeDoc(ownerId: 99);
        _quizRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(quiz);
        _docRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(doc);

        Assert.Null(await _sut.ToggleDeleteAsync(1, 10));
    }

    // ── GetByIdAsync ──

    [Fact]
    public async Task GetByIdAsync_Exists_ReturnsResponse()
    {
        _quizRepo.Setup(r => r.GetVisibleByIdAsync(1)).ReturnsAsync(MakeQuiz());

        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _quizRepo.Setup(r => r.GetVisibleByIdAsync(1)).ReturnsAsync((Quiz?)null);

        Assert.Null(await _sut.GetByIdAsync(1));
    }

    // ── PublishAsync ──

    [Fact]
    public async Task PublishAsync_ValidFlashcards_SavesWholeGraphAndPreservesPositions()
    {
        Quiz? savedQuiz = null;
        var transaction = new Mock<IDbContextTransaction>();
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());
        _quizRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        _quizRepo.Setup(r => r.AddAsync(It.IsAny<Quiz>()))
            .Callback<Quiz>(quiz => savedQuiz = quiz)
            .Returns(Task.CompletedTask);
        _quizRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.PublishAsync(10, MakeFlashcardPublishRequest());

        Assert.NotNull(savedQuiz);
        Assert.Equal("Flashcard", savedQuiz!.ActivityType);
        Assert.Equal("Teacher_Approved", savedQuiz.Status);
        Assert.Equal(new[] { 0, 1 }, savedQuiz.Questions.Select(q => q.Position).ToArray());
        Assert.All(savedQuiz.Questions, question =>
        {
            Assert.Equal("Flashcard", question.QuestionType);
            var option = Assert.Single(question.Options);
            Assert.True(option.IsCorrect);
        });
        Assert.Equal(2, result.Questions.Count);
        transaction.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_DocumentOwnedByAnotherUser_ThrowsBeforeSaving()
    {
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc(ownerId: 99));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.PublishAsync(10, MakeFlashcardPublishRequest()));

        _quizRepo.Verify(r => r.AddAsync(It.IsAny<Quiz>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_FlashcardWithMissingBack_ThrowsBeforeSaving()
    {
        var request = MakeFlashcardPublishRequest();
        request.Questions[0].Options[0].OptionText = " ";
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.PublishAsync(10, request));

        _quizRepo.Verify(r => r.AddAsync(It.IsAny<Quiz>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_FlashcardWithMultipleOptions_ThrowsBeforeSaving()
    {
        var request = MakeFlashcardPublishRequest();
        request.Questions[0].Options.Add(new QuizPublishOptionRequest
        {
            OptionText = "Another back",
            IsCorrect = true
        });
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.PublishAsync(10, request));

        _quizRepo.Verify(r => r.AddAsync(It.IsAny<Quiz>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_MismatchedActivityAndQuestionType_ThrowsBeforeSaving()
    {
        var request = MakeFlashcardPublishRequest();
        request.Questions[0].QuestionType = "MultipleChoice";
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.PublishAsync(10, request));

        _quizRepo.Verify(r => r.AddAsync(It.IsAny<Quiz>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_DuplicatePositions_ThrowsBeforeSaving()
    {
        var request = MakeFlashcardPublishRequest();
        request.Questions[1].Position = request.Questions[0].Position;
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.PublishAsync(10, request));

        _quizRepo.Verify(r => r.AddAsync(It.IsAny<Quiz>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_SaveFails_RollsBackTransaction()
    {
        var transaction = new Mock<IDbContextTransaction>();
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());
        _quizRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        _quizRepo.Setup(r => r.AddAsync(It.IsAny<Quiz>())).Returns(Task.CompletedTask);
        _quizRepo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("DB failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PublishAsync(10, MakeFlashcardPublishRequest()));

        transaction.Verify(t => t.RollbackAsync(default), Times.Once);
        transaction.Verify(t => t.CommitAsync(default), Times.Never);
    }

    // ── GetMineAsync / GetDetailAsync ──

    [Fact]
    public async Task GetMineAsync_FlashcardFilter_ReturnsOwnedFlashcardsWithQuestionCount()
    {
        var quiz = MakeQuiz();
        quiz.ActivityType = "Flashcard";
        quiz.Questions.Add(new Question { Id = 1, QuizId = quiz.Id, Position = 0 });
        quiz.Questions.Add(new Question { Id = 2, QuizId = quiz.Id, Position = 1 });
        _quizRepo.Setup(r => r.GetMineAsync(10, "Flashcard"))
            .ReturnsAsync(new[] { quiz });

        var result = (await _sut.GetMineAsync(10, "Flashcard")).ToList();

        var response = Assert.Single(result);
        Assert.Equal("Flashcard", response.ActivityType);
        Assert.Equal(2, response.QuestionCount);
    }

    [Fact]
    public async Task GetDetailAsync_Exists_ReturnsQuestionsOrderedByPosition()
    {
        var quiz = MakeQuiz();
        quiz.ActivityType = "Flashcard";
        quiz.Questions.Add(new Question
        {
            Id = 2,
            QuizId = quiz.Id,
            QuestionText = "Second",
            QuestionType = "Flashcard",
            Position = 1,
            Options = [new Option { Id = 2, QuestionId = 2, OptionText = "Back 2", IsCorrect = true }]
        });
        quiz.Questions.Add(new Question
        {
            Id = 1,
            QuizId = quiz.Id,
            QuestionText = "First",
            QuestionType = "Flashcard",
            Position = 0,
            Options = [new Option { Id = 1, QuestionId = 1, OptionText = "Back 1", IsCorrect = true }]
        });
        _quizRepo.Setup(r => r.GetDetailByIdAsync(1, 10)).ReturnsAsync(quiz);

        var result = await _sut.GetDetailAsync(1, 10);

        Assert.NotNull(result);
        Assert.Equal(new[] { 0, 1 }, result!.Questions.Select(q => q.Position).ToArray());
        Assert.Equal("Back 1", Assert.Single(result.Questions[0].Options).OptionText);
    }

    [Fact]
    public async Task GetDetailAsync_NotOwnedOrMissing_ReturnsNull()
    {
        _quizRepo.Setup(r => r.GetDetailByIdAsync(1, 10)).ReturnsAsync((Quiz?)null);

        Assert.Null(await _sut.GetDetailAsync(1, 10));
    }

    private static QuizPublishRequest MakeFlashcardPublishRequest() => new()
    {
        DocumentId = 1,
        Title = "Biology cards",
        ActivityType = "Flashcard",
        Questions =
        [
            new QuizPublishQuestionRequest
            {
                QuestionText = "Front 1",
                QuestionType = "Flashcard",
                Position = 0,
                Options =
                [
                    new QuizPublishOptionRequest { OptionText = "Back 1", IsCorrect = true }
                ]
            },
            new QuizPublishQuestionRequest
            {
                QuestionText = "Front 2",
                QuestionType = "Flashcard",
                Position = 1,
                Options =
                [
                    new QuizPublishOptionRequest { OptionText = "Back 2", IsCorrect = true }
                ]
            }
        ]
    };
}
