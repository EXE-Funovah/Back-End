using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.Implementations;
using Mascoteach.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
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
}
