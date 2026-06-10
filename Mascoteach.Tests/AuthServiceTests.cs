using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Mascoteach.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IGoogleTokenValidator> _googleTokenValidator = new();
    private readonly IMapper _mapper = TestHelper.CreateMapper();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Key", "SuperSecretKeyForTestingPurposesOnly1234567890!" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:DurationInMinutes", "60" },
            { "Frontend:ResetPasswordUrl", "https://mascoteach.com/reset-password" },
            { "Frontend:VerifyEmailUrl", "https://mascoteach.com/verify-email" },
            { "Auth:EmailVerificationTokenHours", "24" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        _sut = new AuthService(_userRepo.Object, _mapper, config, _emailService.Object, _googleTokenValidator.Object);
    }

    // ── RegisterAsync ──

    [Fact]
    public async Task RegisterAsync_NewEmail_StoresVerificationTokenAndSendsEmail()
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
        Assert.Equal("Registration successful. Please check your email to verify your account.", result!.Message);
        _userRepo.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == "new@test.com" &&
            u.EmailVerified == false &&
            !string.IsNullOrWhiteSpace(u.EmailVerificationTokenHash) &&
            u.EmailVerificationTokenExpiresAt > DateTime.UtcNow)), Times.Once);
        _emailService.Verify(s => s.SendEmailVerificationAsync(
            "new@test.com",
            "Test User",
            It.Is<string>(link => link.StartsWith("https://mascoteach.com/verify-email?token="))),
            Times.Once);
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
                PasswordHash = hash, Role = "Teacher", SubscriptionTier = "Freemium",
                EmailVerified = true
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
                PasswordHash = hash, Role = "Teacher", SubscriptionTier = "Freemium",
                EmailVerified = true
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

    [Fact]
    public async Task LoginAsync_GoogleOnlyAccount_ThrowsGoogleLoginMessage()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("google@test.com"))
            .ReturnsAsync(new User
            {
                Id = 1,
                Email = "google@test.com",
                FullName = "Google User",
                PasswordHash = null,
                Role = "Teacher",
                SubscriptionTier = "Freemium",
                Authenticator = "Google"
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(new LoginRequest { Email = "google@test.com", Password = "any" }));

        Assert.Contains("Google", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_UnverifiedLocalUser_ThrowsVerifyEmailMessage()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct_password");
        _userRepo.Setup(r => r.GetByEmailAsync("unverified@test.com"))
            .ReturnsAsync(new User
            {
                Id = 1,
                Email = "unverified@test.com",
                FullName = "Unverified User",
                PasswordHash = hash,
                Role = "Teacher",
                SubscriptionTier = "Freemium",
                Authenticator = "Local",
                EmailVerified = false
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(new LoginRequest
            {
                Email = "unverified@test.com",
                Password = "correct_password"
            }));

        Assert.Contains("verify your email", ex.Message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_LocalUser_StoresTokenHashAndSendsEmail()
    {
        var user = new User
        {
            Id = 1,
            Email = "local@test.com",
            FullName = "Local User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old_password"),
            Role = "Teacher",
            SubscriptionTier = "Freemium",
            Authenticator = "Local"
        };
        _userRepo.Setup(r => r.GetByEmailAsync("local@test.com")).ReturnsAsync(user);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "local@test.com" });

        Assert.False(string.IsNullOrWhiteSpace(user.ResetTokenHash));
        Assert.True(user.ResetTokenExpiresAt > DateTime.UtcNow);
        _emailService.Verify(s => s.SendPasswordResetEmailAsync(
            "local@test.com",
            "Local User",
            It.Is<string>(link => link.StartsWith("https://mascoteach.com/reset-password?token="))),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordAndClearsResetFields()
    {
        var token = "reset-token";
        var user = new User
        {
            Id = 1,
            Email = "local@test.com",
            FullName = "Local User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old_password"),
            Role = "Teacher",
            SubscriptionTier = "Freemium",
            Authenticator = "Local",
            ResetTokenHash = AuthService.HashResetToken(token),
            ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
        _userRepo.Setup(r => r.GetByResetTokenHashAsync(AuthService.HashResetToken(token)))
            .ReturnsAsync(user);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = token,
            NewPassword = "new_password",
            ConfirmPassword = "new_password"
        });

        Assert.True(result);
        Assert.True(BCrypt.Net.BCrypt.Verify("new_password", user.PasswordHash));
        Assert.Null(user.ResetTokenHash);
        Assert.Null(user.ResetTokenExpiresAt);
    }

    [Fact]
    public async Task ResetPasswordAsync_NewPasswordMatchesCurrentPassword_Throws()
    {
        var token = "reset-token";
        var currentHash = BCrypt.Net.BCrypt.HashPassword("old_password");
        var user = new User
        {
            Id = 1,
            Email = "local@test.com",
            FullName = "Local User",
            PasswordHash = currentHash,
            Role = "Teacher",
            SubscriptionTier = "Freemium",
            Authenticator = "Local",
            ResetTokenHash = AuthService.HashResetToken(token),
            ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
        _userRepo.Setup(r => r.GetByResetTokenHashAsync(AuthService.HashResetToken(token)))
            .ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ResetPasswordAsync(new ResetPasswordRequest
            {
                Token = token,
                NewPassword = "old_password",
                ConfirmPassword = "old_password"
            }));

        Assert.Contains("different", ex.Message);
        Assert.Equal(currentHash, user.PasswordHash);
        Assert.NotNull(user.ResetTokenHash);
        Assert.NotNull(user.ResetTokenExpiresAt);
        _userRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidToken_MarksUserVerifiedAndClearsToken()
    {
        var token = "verify-token";
        var user = new User
        {
            Id = 1,
            Email = "local@test.com",
            FullName = "Local User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "Teacher",
            SubscriptionTier = "Freemium",
            Authenticator = "Local",
            EmailVerified = false,
            EmailVerificationTokenHash = AuthService.HashResetToken(token),
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _userRepo.Setup(r => r.GetByEmailVerificationTokenHashAsync(AuthService.HashResetToken(token)))
            .ReturnsAsync(user);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.VerifyEmailAsync(new VerifyEmailRequest { Token = token });

        Assert.True(result);
        Assert.True(user.EmailVerified);
        Assert.True(user.EmailVerifiedAt <= DateTime.UtcNow);
        Assert.Null(user.EmailVerificationTokenHash);
        Assert.Null(user.EmailVerificationTokenExpiresAt);
    }

    [Fact]
    public async Task VerifyEmailAsync_ExpiredToken_ReturnsFalse()
    {
        var token = "expired-token";
        var user = new User
        {
            Id = 1,
            Email = "local@test.com",
            FullName = "Local User",
            Role = "Teacher",
            SubscriptionTier = "Freemium",
            EmailVerified = false,
            EmailVerificationTokenHash = AuthService.HashResetToken(token),
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _userRepo.Setup(r => r.GetByEmailVerificationTokenHashAsync(AuthService.HashResetToken(token)))
            .ReturnsAsync(user);

        var result = await _sut.VerifyEmailAsync(new VerifyEmailRequest { Token = token });

        Assert.False(result);
        Assert.False(user.EmailVerified);
        Assert.NotNull(user.EmailVerificationTokenHash);
        _userRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ResendVerificationAsync_UnverifiedUser_StoresNewTokenAndSendsEmail()
    {
        var user = new User
        {
            Id = 1,
            Email = "local@test.com",
            FullName = "Local User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "Teacher",
            SubscriptionTier = "Freemium",
            Authenticator = "Local",
            EmailVerified = false
        };
        _userRepo.Setup(r => r.GetByEmailAsync("local@test.com")).ReturnsAsync(user);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.ResendVerificationAsync(new ResendVerificationRequest { Email = "local@test.com" });

        Assert.False(string.IsNullOrWhiteSpace(user.EmailVerificationTokenHash));
        Assert.True(user.EmailVerificationTokenExpiresAt > DateTime.UtcNow);
        _emailService.Verify(s => s.SendEmailVerificationAsync(
            "local@test.com",
            "Local User",
            It.Is<string>(link => link.StartsWith("https://mascoteach.com/verify-email?token="))),
            Times.Once);
    }

    [Fact]
    public async Task GoogleLoginAsync_NewGoogleUser_CreatesTeacherUserAndReturnsToken()
    {
        _googleTokenValidator.Setup(v => v.ValidateAsync("google-id-token"))
            .ReturnsAsync(new GoogleUserInfo
            {
                Subject = "google-sub",
                Email = "google@test.com",
                FullName = "Google User",
                EmailVerified = true
            });
        _userRepo.Setup(r => r.GetByGoogleSubjectAsync("google-sub")).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.GetByEmailAsync("google@test.com")).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask)
            .Callback<User>(u => u.Id = 10);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.GoogleLoginAsync(new GoogleLoginRequest { Credential = "google-id-token" });

        Assert.NotNull(result);
        Assert.Equal("google@test.com", result!.Email);
        Assert.Equal("Teacher", result.Role);
        Assert.False(string.IsNullOrEmpty(result.Token));
        _userRepo.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Authenticator == "Google" &&
            u.GoogleSubject == "google-sub" &&
            u.PasswordHash == null &&
            u.EmailVerified &&
            u.EmailVerifiedAt != null &&
            u.Role == "Teacher")), Times.Once);
    }
}
