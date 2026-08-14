using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Mascoteach.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.Text.Json;
using Xunit;

namespace Mascoteach.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IS3Service> _s3Service = new();
    private readonly IMapper _mapper = TestHelper.CreateMapper();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepo.Object, _mapper, _s3Service.Object);
    }

    [Fact]
    public async Task UpdateAsync_AttackerSuppliedRole_PreservesStoredRole()
    {
        var user = new User
        {
            Id = 10,
            FullName = "Student",
            Email = "student@test.com",
            Role = "Student",
            SubscriptionTier = "Freemium"
        };
        var request = JsonSerializer.Deserialize<UserUpdateRequest>(
            """
            {
              "FullName": "Updated Student",
              "Email": "updated@test.com",
              "Role": "Admin",
              "SubscriptionTier": "Freemium"
            }
            """)!;
        _userRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(10, request);

        Assert.True(result);
        Assert.Equal("Student", user.Role);
    }

    [Fact]
    public async Task UpdateAsync_AttackerSuppliedSubscription_PreservesStoredSubscription()
    {
        var user = new User
        {
            Id = 10,
            FullName = "Teacher",
            Email = "teacher@test.com",
            Role = "Teacher",
            SubscriptionTier = "Freemium"
        };
        var request = JsonSerializer.Deserialize<UserUpdateRequest>(
            """
            {
              "FullName": "Updated Teacher",
              "Email": "updated@test.com",
              "Role": "Teacher",
              "SubscriptionTier": "Premium"
            }
            """)!;
        _userRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(10, request);

        Assert.True(result);
        Assert.Equal("Freemium", user.SubscriptionTier);
    }

    [Fact]
    public async Task UpdateAsync_LegitimateProfileFields_UpdatesNameAndEmail()
    {
        var user = new User
        {
            Id = 10,
            FullName = "Old Name",
            Email = "old@test.com",
            Role = "Teacher",
            SubscriptionTier = "Freemium"
        };
        var request = new UserUpdateRequest
        {
            FullName = "New Name",
            Email = "new@test.com"
        };
        _userRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(10, request);

        Assert.True(result);
        Assert.Equal("New Name", user.FullName);
        Assert.Equal("new@test.com", user.Email);
    }

    [Fact]
    public async Task DeleteAsync_ExistingUser_HardDeletesGraphInTransaction()
    {
        var transaction = new Mock<IDbContextTransaction>();
        var user = new User
        {
            Id = 10,
            FullName = "Teacher",
            Email = "teacher@test.com",
            AvatarUrl = "avatars/user-10.png",
            Documents =
            {
                new Document { Id = 1, FileUrl = "documents/doc-1.zip" },
                new Document { Id = 2, FileUrl = "documents/doc-2.zip" }
            }
        };

        _userRepo.Setup(r => r.GetAccountDeletionGraphAsync(10)).ReturnsAsync(user);
        _userRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _s3Service.Setup(s => s.DeleteObjectsAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(10);

        Assert.True(result);
        _userRepo.Verify(r => r.HardDeleteAccountGraph(user), Times.Once);
        _userRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _s3Service.Verify(s => s.DeleteObjectsAsync(It.Is<IEnumerable<string>>(keys =>
            keys.SequenceEqual(new[]
            {
                "documents/doc-1.zip",
                "documents/doc-2.zip",
                "avatars/user-10.png",
            }))), Times.Once);
        transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_UserNotFound_ReturnsFalse()
    {
        _userRepo.Setup(r => r.GetAccountDeletionGraphAsync(10)).ReturnsAsync((User?)null);

        var result = await _sut.DeleteAsync(10);

        Assert.False(result);
        _userRepo.Verify(r => r.BeginTransactionAsync(), Times.Never);
        _userRepo.Verify(r => r.HardDeleteAccountGraph(It.IsAny<User>()), Times.Never);
        _s3Service.Verify(s => s.DeleteObjectsAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_S3CleanupFails_StillReturnsTrueAfterCommit()
    {
        var transaction = new Mock<IDbContextTransaction>();
        var user = new User
        {
            Id = 10,
            FullName = "Teacher",
            Email = "teacher@test.com",
            Documents = { new Document { Id = 1, FileUrl = "documents/doc-1.zip" } }
        };

        _userRepo.Setup(r => r.GetAccountDeletionGraphAsync(10)).ReturnsAsync(user);
        _userRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _s3Service.Setup(s => s.DeleteObjectsAsync(It.IsAny<IEnumerable<string>>()))
            .ThrowsAsync(new InvalidOperationException("S3 cleanup failed"));

        var result = await _sut.DeleteAsync(10);

        Assert.True(result);
        transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_TeacherTransfersOwnedClassesBeforeHardDelete()
    {
        var transaction = new Mock<IDbContextTransaction>();
        var teacher = new User
        {
            Id = 10,
            FullName = "Teacher",
            Email = "teacher@test.com",
            Role = "Teacher"
        };
        _userRepo.Setup(r => r.GetAccountDeletionGraphAsync(10)).ReturnsAsync(teacher);
        _userRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        _userRepo.Setup(r => r.TransferOwnedClassesBeforeDeactivationAsync(10)).ReturnsAsync(true);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(10);

        Assert.True(result);
        _userRepo.Verify(r => r.TransferOwnedClassesBeforeDeactivationAsync(10), Times.Once);
        _userRepo.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
        _userRepo.Verify(r => r.HardDeleteAccountGraph(teacher), Times.Once);
    }

    [Fact]
    public async Task ToggleDeleteAsync_TeacherWithSuccessors_TransfersClassesBeforeDisabling()
    {
        var teacher = new User
        {
            Id = 10,
            FullName = "Teacher",
            Email = "teacher@test.com",
            Role = "Teacher",
            IsDeleted = false
        };
        _userRepo.Setup(r => r.GetByIdIncludingDeletedAsync(10)).ReturnsAsync(teacher);
        _userRepo.Setup(r => r.TransferOwnedClassesBeforeDeactivationAsync(10)).ReturnsAsync(true);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ToggleDeleteAsync(10);

        Assert.NotNull(result);
        Assert.True(teacher.IsDeleted);
        _userRepo.Verify(r => r.TransferOwnedClassesBeforeDeactivationAsync(10), Times.Once);
    }

    [Fact]
    public async Task ToggleDeleteAsync_TeacherWithoutSuccessor_RejectsDeactivation()
    {
        var teacher = new User
        {
            Id = 10,
            FullName = "Teacher",
            Email = "teacher@test.com",
            Role = "Teacher",
            IsDeleted = false
        };
        _userRepo.Setup(r => r.GetByIdIncludingDeletedAsync(10)).ReturnsAsync(teacher);
        _userRepo.Setup(r => r.TransferOwnedClassesBeforeDeactivationAsync(10)).ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ToggleDeleteAsync(10));

        Assert.False(teacher.IsDeleted);
        _userRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
