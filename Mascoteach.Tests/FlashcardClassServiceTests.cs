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
            Description = "  Ôn tập  ",
            Password = "secret123"
        });

        Assert.NotNull(saved);
        Assert.Equal(10, saved!.TeacherId);
        Assert.Equal("Lớp 10A", saved.Name);
        Assert.Equal("Ôn tập", saved.Description);
        Assert.Matches("^[0-9]{6}$", saved.ClassCode);
        Assert.Equal(Now.UtcDateTime, saved.CreatedAt);
        var ownerMembership = Assert.Single(saved.ClassTeachers);
        Assert.Equal(10, ownerMembership.TeacherId);
        Assert.Equal("Owner", ownerMembership.Role);
        Assert.Equal(saved.Name, result.Name);
        Assert.True(result.IsOwner);
        Assert.NotEqual("secret123", saved.JoinPasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("secret123", saved.JoinPasswordHash));
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
        classroom.JoinPasswordHash = BCrypt.Net.BCrypt.HashPassword("secret123");
        _repository.Setup(item => item.GetActiveClassByIdAsync(1))
            .ReturnsAsync(classroom);
        _repository.Setup(item => item.GetMemberIncludingDeletedAsync(1, 20))
            .ReturnsAsync(member);

        await _service.JoinClassAsync(20, new ClassJoinRequest
        {
            ClassId = 1,
            Password = "secret123"
        });

        Assert.False(member.IsDeleted);
        Assert.Equal(Now.UtcDateTime, member.JoinedAt);
        _repository.Verify(item => item.AddMemberAsync(It.IsAny<ClassMember>()), Times.Never);
    }

    [Fact]
    public async Task JoinClassAsync_WrongPassword_RejectsWithoutPersistence()
    {
        var classroom = MakeClass();
        classroom.JoinPasswordHash = BCrypt.Net.BCrypt.HashPassword("secret123");
        _repository.Setup(item => item.GetActiveClassByIdAsync(1)).ReturnsAsync(classroom);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.JoinClassAsync(20, new ClassJoinRequest
            {
                ClassId = 1,
                Password = "wrong-password"
            }));

        _repository.Verify(item => item.GetMemberIncludingDeletedAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _repository.Verify(item => item.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SearchClassesAsync_ReturnsMinimalSearchResults()
    {
        var classroom = MakeClass();
        classroom.ClassMembers.Add(new ClassMember { ClassId = 1, StudentId = 20, IsDeleted = false });
        _repository.Setup(item => item.SearchActiveClassesAsync("10A", 20))
            .ReturnsAsync([classroom]);

        var result = await _service.SearchClassesAsync(" 10A ");

        var item = Assert.Single(result);
        Assert.Equal(1, item.Id);
        Assert.Equal("10A", item.Name);
        Assert.Equal("Teacher", item.TeacherName);
        Assert.Equal(1, item.MemberCount);
    }

    [Fact]
    public async Task AssignFlashcardAsync_ValidPublishedSet_PersistsOnlyAssignmentAsNewGraph()
    {
        var classroom = MakeClass();
        var flashcard = MakeFlashcard();
        FlashcardAssignment? saved = null;
        _repository.Setup(item => item.GetAccessibleClassAsync(1, 10)).ReturnsAsync(classroom);
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
        _repository.Setup(item => item.GetAccessibleClassAsync(1, 10)).ReturnsAsync(MakeClass());
        _repository.Setup(item => item.GetOwnedPublishedFlashcardAsync(2, 10)).ReturnsAsync(flashcard);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AssignFlashcardAsync(1, 10, new FlashcardAssignmentCreateRequest { QuizId = 2 }));

        _repository.Verify(item => item.AddAssignmentAsync(It.IsAny<FlashcardAssignment>()), Times.Never);
    }

    [Fact]
    public async Task AssignFlashcardAsync_CollaboratingTeacher_CanAssignOwnedFlashcard()
    {
        var classroom = MakeClass();
        var collaborator = new User
        {
            Id = 11,
            FullName = "Teacher Two",
            Email = "teacher2@test.local",
            Role = "Teacher"
        };
        classroom.ClassTeachers.Add(new ClassTeacher
        {
            ClassId = classroom.Id,
            TeacherId = collaborator.Id,
            Teacher = collaborator,
            Role = "Teacher"
        });
        var flashcard = MakeFlashcard();
        _repository.Setup(item => item.GetAccessibleClassAsync(1, 11)).ReturnsAsync(classroom);
        _repository.Setup(item => item.GetOwnedPublishedFlashcardAsync(2, 11)).ReturnsAsync(flashcard);
        _repository.Setup(item => item.GetActiveAssignmentAsync(1, 2))
            .ReturnsAsync((FlashcardAssignment?)null);

        var result = await _service.AssignFlashcardAsync(
            1,
            11,
            new FlashcardAssignmentCreateRequest { QuizId = 2 });

        Assert.Equal(11, result.AssignedById);
        Assert.Equal("Teacher Two", result.AssignedByName);
        _repository.Verify(item => item.AddAssignmentAsync(
            It.Is<FlashcardAssignment>(assignment => assignment.AssignedBy == 11)), Times.Once);
    }

    [Fact]
    public async Task AddTeacherAsync_OwnerAddsActiveTeacherByEmail()
    {
        var classroom = MakeClass();
        var teacher = new User
        {
            Id = 11,
            FullName = "Teacher Two",
            Email = "teacher2@test.local",
            Role = "Teacher"
        };
        ClassTeacher? saved = null;
        _repository.Setup(item => item.GetOwnedClassAsync(1, 10)).ReturnsAsync(classroom);
        _repository.Setup(item => item.GetActiveTeacherByEmailAsync("teacher2@test.local"))
            .ReturnsAsync(teacher);
        _repository.Setup(item => item.GetClassTeacherIncludingDeletedAsync(1, 11))
            .ReturnsAsync((ClassTeacher?)null);
        _repository.Setup(item => item.AddClassTeacherAsync(It.IsAny<ClassTeacher>()))
            .Callback<ClassTeacher>(membership => saved = membership)
            .Returns(Task.CompletedTask);

        var result = await _service.AddTeacherAsync(
            1,
            10,
            new ClassTeacherAddRequest { Email = " Teacher2@Test.Local " });

        Assert.NotNull(saved);
        Assert.Equal(11, saved!.TeacherId);
        Assert.Equal("Teacher", saved.Role);
        Assert.Equal("Teacher Two", result.FullName);
    }

    [Fact]
    public async Task AddTeacherAsync_NonOwnerCannotManageTeachers()
    {
        _repository.Setup(item => item.GetOwnedClassAsync(1, 11))
            .ReturnsAsync((Class?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AddTeacherAsync(
            1,
            11,
            new ClassTeacherAddRequest { Email = "teacher2@test.local" }));

        _repository.Verify(item => item.GetActiveTeacherByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TransferOwnershipAsync_ChangesOwnerAndKeepsPreviousOwnerAsTeacher()
    {
        var classroom = MakeClass();
        var nextOwner = new User
        {
            Id = 11,
            FullName = "Teacher Two",
            Email = "teacher2@test.local",
            Role = "Teacher"
        };
        classroom.ClassTeachers.Add(new ClassTeacher
        {
            ClassId = 1,
            TeacherId = 11,
            Teacher = nextOwner,
            Role = "Teacher"
        });
        _repository.Setup(item => item.GetOwnedClassForUpdateAsync(1, 10)).ReturnsAsync(classroom);

        var result = await _service.TransferOwnershipAsync(
            1,
            10,
            new ClassOwnershipTransferRequest { TeacherId = 11 });

        Assert.Equal(11, classroom.TeacherId);
        Assert.Equal("Teacher", classroom.ClassTeachers.Single(item => item.TeacherId == 10).Role);
        Assert.Equal("Owner", classroom.ClassTeachers.Single(item => item.TeacherId == 11).Role);
        Assert.Equal("Owner", result.Role);
    }

    [Fact]
    public async Task LeaveClassAsTeacherAsync_OwnerCannotLeaveBeforeTransfer()
    {
        _repository.Setup(item => item.GetAccessibleClassAsync(1, 10)).ReturnsAsync(MakeClass());

        var result = await _service.LeaveClassAsTeacherAsync(1, 10);

        Assert.False(result);
        _repository.Verify(item => item.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task LeaveClassAsStudentAsync_ActiveMemberSoftDeletesMembership()
    {
        var member = new ClassMember { ClassId = 1, StudentId = 20, IsDeleted = false };
        _repository.Setup(item => item.GetMemberIncludingDeletedAsync(1, 20)).ReturnsAsync(member);

        var result = await _service.LeaveClassAsStudentAsync(1, 20);

        Assert.True(result);
        Assert.True(member.IsDeleted);
    }

    [Fact]
    public async Task RemoveFlashcardAssignmentAsync_AssigningTeacherCanWithdrawOwnAssignment()
    {
        var classroom = MakeClass();
        var assignment = MakeAssignment();
        assignment.AssignedBy = 11;
        _repository.Setup(item => item.GetAccessibleClassAsync(1, 11)).ReturnsAsync(classroom);
        _repository.Setup(item => item.GetActiveAssignmentByIdAsync(1, 7)).ReturnsAsync(assignment);

        var result = await _service.RemoveFlashcardAssignmentAsync(1, 7, 11);

        Assert.True(result);
        Assert.True(assignment.IsDeleted);
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
        Teacher = new User { Id = 10, FullName = "Teacher", Email = "teacher@test.local", Role = "Teacher" },
        ClassTeachers =
        [
            new ClassTeacher
            {
                ClassId = 1,
                TeacherId = 10,
                Role = "Owner",
                Teacher = new User { Id = 10, FullName = "Teacher", Email = "teacher@test.local", Role = "Teacher" }
            }
        ]
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
