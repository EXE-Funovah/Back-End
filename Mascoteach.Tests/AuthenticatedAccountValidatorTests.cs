using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AuthenticatedAccountValidatorTests
{
    [Fact]
    public async Task IsAllowed_ActiveUserWithCurrentRole_ReturnsTrue()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(value => value.GetByIdIncludingDeletedAsync(7))
            .ReturnsAsync(CreateUser(isDeleted: false, role: "Admin"));
        var validator = new AuthenticatedAccountValidator(repository.Object);

        Assert.True(await validator.IsAllowedAsync(7, "admin"));
    }

    [Theory]
    [InlineData(true, "Admin")]
    [InlineData(false, "Teacher")]
    public async Task IsAllowed_DeletedUserOrStaleRole_ReturnsFalse(
        bool isDeleted,
        string tokenRole)
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(value => value.GetByIdIncludingDeletedAsync(7))
            .ReturnsAsync(CreateUser(isDeleted, "Admin"));
        var validator = new AuthenticatedAccountValidator(repository.Object);

        Assert.False(await validator.IsAllowedAsync(7, tokenRole));
    }

    [Fact]
    public async Task IsAllowed_MissingUser_ReturnsFalse()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(value => value.GetByIdIncludingDeletedAsync(7))
            .ReturnsAsync((User?)null);
        var validator = new AuthenticatedAccountValidator(repository.Object);

        Assert.False(await validator.IsAllowedAsync(7, "Admin"));
    }

    private static User CreateUser(bool isDeleted, string role) => new()
    {
        Id = 7,
        FullName = "Admin",
        Email = "admin@mascoteach.com",
        Role = role,
        SubscriptionTier = "Freemium",
        IsDeleted = isDeleted
    };
}
