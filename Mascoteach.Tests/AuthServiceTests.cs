using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly IMapper _mapper = TestHelper.CreateMapper();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Key", "SuperSecretKeyForTestingPurposesOnly1234567890!" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:DurationInMinutes", "60" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        _sut = new AuthService(_userRepo.Object, _mapper, config);
    }

    // ── RegisterAsync ──

    [Fact]
    public async Task RegisterAsync_NewEmail_ReturnsAuthResponse()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            FullName = "Test User",
            Email = "new@test.com",
            Password = "pass123",
            Role = "Teacher"
        });

        Assert.NotNull(result);
        Assert.Equal("new@test.com", result!.Email);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsNull()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("exists@test.com"))
            .ReturnsAsync(new User { Id = 1, Email = "exists@test.com", FullName = "X", PasswordHash = "h", Role = "Teacher", SubscriptionTier = "Freemium" });

        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            FullName = "Test", Email = "exists@test.com", Password = "pass", Role = "Teacher"
        });

        Assert.Null(result);
    }

    // ── LoginAsync ──

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct_password");
        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com"))
            .ReturnsAsync(new User
            {
                Id = 1, Email = "user@test.com", FullName = "User",
                PasswordHash = hash, Role = "Teacher", SubscriptionTier = "Freemium"
            });

        var result = await _sut.LoginAsync(new LoginRequest
        {
            Email = "user@test.com", Password = "correct_password"
        });

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result!.Token));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct_password");
        _userRepo.Setup(r => r.GetByEmailAsync("user@test.com"))
            .ReturnsAsync(new User
            {
                Id = 1, Email = "user@test.com", FullName = "User",
                PasswordHash = hash, Role = "Teacher", SubscriptionTier = "Freemium"
            });

        var result = await _sut.LoginAsync(new LoginRequest
        {
            Email = "user@test.com", Password = "wrong_password"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_DeletedUser_ReturnsNull()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("pass");
        _userRepo.Setup(r => r.GetByEmailAsync("deleted@test.com"))
            .ReturnsAsync(new User
            {
                Id = 1, Email = "deleted@test.com", FullName = "Del",
                PasswordHash = hash, Role = "Teacher", SubscriptionTier = "Freemium",
                IsDeleted = true
            });

        Assert.Null(await _sut.LoginAsync(new LoginRequest { Email = "deleted@test.com", Password = "pass" }));
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsNull()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("ghost@test.com")).ReturnsAsync((User?)null);

        Assert.Null(await _sut.LoginAsync(new LoginRequest { Email = "ghost@test.com", Password = "pass" }));
    }
}
