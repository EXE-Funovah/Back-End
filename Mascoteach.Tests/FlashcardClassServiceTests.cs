using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public sealed class FlashcardClassServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
    private readonly Mock<IFlashcardClassRepository> _repository = new();
    private readonly FlashcardClassService _service;

    public FlashcardClassServiceTests()
    {
        _service = new FlashcardClassService(_repository.Object, new FixedTimeProvider(Now));
        _repository.Setup(item => item.SaveChangesAsync()).ReturnsAsync(1);
    }

    [Fact]
    public async Task CreateClassAsync_NormalizesInputAndPersistsTeacherClass()
    {
        Class? saved = null;
        _repository.Setup(item => item.ClassCodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _repository.Setup(item => item.AddClassAsync(It.IsAny<Class>()))
            .Callback<Class>(item => saved = item)
            .Returns(Task.CompletedTask);

        var result = await _service.CreateClassAsync(10, new ClassCreateRequest
        {
            Name = "  Lớp 10A  ",
            Description = "  Ôn tập  "
        });

        Assert.NotNull(saved);
        Assert.Equal(10, saved!.TeacherId);
        Assert.Equal("Lớp 10A", saved.Name);
        Assert.Equal("Ôn tập", saved.Description);
        Assert.Matches("^[0-9]{6}$", saved.ClassCode);
        Assert.Equal(Now.UtcDateTime, saved.CreatedAt);
        Assert.Equal(saved.ClassCode, result.ClassCode);
    }

    [Fact]
    public async Task JoinClassAsync_PreviouslyRemovedMember_ReactivatesMembership()
    {
        var classroom = MakeClass();
        var member = new ClassMember
        {
            Id = 5,
            ClassId = classroom.Id,
            StudentId = 20,
            IsDeleted = true
        };
        _repository.Setup(item => item.GetActiveClassByCodeAsync("123456"))
            .ReturnsAsync(classroom);
        _repository.Setup(item => item.GetMemberIncludingDeletedAsync(1, 20))
            .ReturnsAsync(member);

        await _service.JoinClassAsync(20, new ClassJoinRequest { ClassCode = " 123456 " });

        Assert.False(member.IsDeleted);
        Assert.Equal(Now.UtcDateTime, member.JoinedAt);
        _repository.Verify(item => item.AddMemberAsync(It.IsAny<ClassMember>()), Times.Never);
    }

    [Fact]
    public async Task AssignFlashcardAsync_ValidPublishedSet_PersistsOnlyAssignmentAsNewGraph()
    {
        var classroom = MakeClass();
        var flashcard = MakeFlashcard();
        FlashcardAssignment? saved = null;
        _repository.Setup(item => item.GetOwnedClassAsync(1, 10)).ReturnsAsync(classroom);
        _repository.Setup(item => item.GetOwnedPublishedFlashcardAsync(2, 10)).ReturnsAsync(flashcard);
        _repository.Setup(item => item.GetActiveAssignmentAsync(1, 2))
            .ReturnsAsync((FlashcardAssignment?)null);
        _repository.Setup(item => item.AddAssignmentAsync(It.IsAny<FlashcardAssignment>()))
            .Callback<FlashcardAssignment>(item =>
            {
                saved = item;
                Assert.Null(item.Class);
                Assert.Null(item.Quiz);
            })
            .Returns(Task.CompletedTask);

        var result = await _service.AssignFlashcardAsync(1, 10, new FlashcardAssignmentCreateRequest
        {
            QuizId = 2,
            Instructions = "  Học trước giờ học  "
        });

        Assert.NotNull(saved);
        Assert.Equal(1, saved!.ClassId);
        Assert.Equal(2, saved.QuizId);
        Assert.Equal("Học trước giờ học", saved.Instructions);
        Assert.Equal("Bộ thẻ", result.Title);
        Assert.Equal(1, result.CardCount);
    }

    [Fact]
    public async Task AssignFlashcardAsync_FlashcardWithExtraOption_RejectsBeforePersistence()
    {
        var flashcard = MakeFlashcard();
        flashcard.Questions.Single().Options.Add(new Option
        {
            Id = 4,
            QuestionId = 3,
            OptionText = "Extra",
            IsCorrect = false
        });
        _repository.Setup(item => item.GetOwnedClassAsync(1, 10)).ReturnsAsync(MakeClass());
        _repository.Setup(item => item.GetOwnedPublishedFlashcardAsync(2, 10)).ReturnsAsync(flashcard);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AssignFlashcardAsync(1, 10, new FlashcardAssignmentCreateRequest { QuizId = 2 }));

        _repository.Verify(item => item.AddAssignmentAsync(It.IsAny<FlashcardAssignment>()), Times.Never);
    }

    [Fact]
    public async Task GetStudyAsync_ReturnsOrderedTwoSidedCardsAndStudentProgress()
    {
        var assignment = MakeAssignment();
        assignment.Quiz.Questions.Add(new Question
        {
            Id = 4,
            QuizId = 2,
            Position = 1,
            QuestionText = "Front 2",
            QuestionType = "Flashcard",
            Options = [new Option { Id = 5, QuestionId = 4, OptionText = "Back 2", IsCorrect = true }]
        });
        assignment.FlashcardStudyProgresses.Add(new FlashcardStudyProgress
        {
            AssignmentId = assignment.Id,
            StudentId = 20,
            QuestionId = 3,
            Status = "Mastered",
            ReviewCount = 1,
            KnownCount = 1
        });
        _repository.Setup(item => item.GetStudentAssignmentAsync(7, 20)).ReturnsAsync(assignment);

        var result = await _service.GetStudyAsync(7, 20);

        Assert.NotNull(result);
        Assert.Equal(new[] { 0, 1 }, result!.Cards.Select(item => item.Position));
        Assert.Equal("Front", result.Cards[0].Front);
        Assert.Equal("Back", result.Cards[0].Back);
        Assert.Equal("Mastered", result.Cards[0].Status);
        Assert.Equal(1, result.MasteredCount);
    }

    [Fact]
    public async Task UpdateProgressAsync_FirstKnownReview_CreatesMasteredProgress()
    {
        var assignment = MakeAssignment();
        FlashcardStudyProgress? saved = null;
        _repository.Setup(item => item.GetStudentAssignmentAsync(7, 20)).ReturnsAsync(assignment);
        _repository.Setup(item => item.GetProgressAsync(7, 20, 3))
            .ReturnsAsync((FlashcardStudyProgress?)null);
        _repository.Setup(item => item.AddProgressAsync(It.IsAny<FlashcardStudyProgress>()))
            .Callback<FlashcardStudyProgress>(item => saved = item)
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateProgressAsync(
            7,
            3,
            20,
            new FlashcardProgressUpdateRequest { IsKnown = true });

        Assert.NotNull(saved);
        Assert.NotNull(result);
        Assert.Equal("Mastered", result!.Status);
        Assert.Equal(1, result.ReviewCount);
        Assert.Equal(1, result.KnownCount);
        Assert.Equal(Now.UtcDateTime, result.MasteredAt);
    }

    [Fact]
    public async Task UpdateProgressAsync_QuestionOutsideAssignment_RejectsWithoutSaving()
    {
        _repository.Setup(item => item.GetStudentAssignmentAsync(7, 20))
            .ReturnsAsync(MakeAssignment());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateProgressAsync(
                7,
                999,
                20,
                new FlashcardProgressUpdateRequest { IsKnown = true }));

        _repository.Verify(item => item.SaveChangesAsync(), Times.Never);
    }

    private static Class MakeClass() => new()
    {
        Id = 1,
        TeacherId = 10,
        Name = "10A",
        ClassCode = "123456",
        Teacher = new User { Id = 10, FullName = "Teacher", Email = "teacher@test.local" }
    };

    private static Quiz MakeFlashcard() => new()
    {
        Id = 2,
        Title = "Bộ thẻ",
        ActivityType = "Flashcard",
        Status = "Teacher_Approved",
        Questions =
        [
            new Question
            {
                Id = 3,
                QuizId = 2,
                Position = 0,
                QuestionText = "Front",
                QuestionType = "Flashcard",
                Options =
                [
                    new Option
                    {
                        Id = 4,
                        QuestionId = 3,
                        OptionText = "Back",
                        IsCorrect = true
                    }
                ]
            }
        ]
    };

    private static FlashcardAssignment MakeAssignment() => new()
    {
        Id = 7,
        ClassId = 1,
        QuizId = 2,
        Class = MakeClass(),
        Quiz = MakeFlashcard(),
        AssignedAt = Now.UtcDateTime
    };

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _value;

        public FixedTimeProvider(DateTimeOffset value)
        {
            _value = value;
        }

        public override DateTimeOffset GetUtcNow() => _value;
    }
}
