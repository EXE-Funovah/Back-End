using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Service.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IGoogleTokenValidator _googleTokenValidator;

        public AuthService(
            IUserRepository userRepository,
            IMapper mapper,
            IConfiguration config,
            IEmailService emailService,
            IGoogleTokenValidator googleTokenValidator)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _config = config;
            _emailService = emailService;
            _googleTokenValidator = googleTokenValidator;
        }
       
        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null) return null;

            // Mã hóa mật khẩu thô thành chuỗi Hash
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = hashedPassword, // Lưu chuỗi đã mã hóa
                Role = request.Role,           // 'Teacher', 'Parent', 'Student', 'Admin'
                SubscriptionTier = "Freemium",
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                Authenticator = "Local"
            };

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            var response = _mapper.Map<AuthResponse>(newUser);
            response.Token = GenerateJwtToken(newUser);
            return response;
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            // Kiểm tra user tồn tại, chưa bị xóa, và dùng BCrypt để so sánh mật khẩu thô với Hash trong DB
            if (user == null || user.IsDeleted)
                return null;

            if (IsGoogleOnlyUser(user))
                throw new InvalidOperationException("This account was registered with Google. Please sign in with Google.");

            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            var response = _mapper.Map<AuthResponse>(user);
            response.Token = GenerateJwtToken(user);
            return response;
        }

        public async Task<AuthResponse?> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var googleUser = await _googleTokenValidator.ValidateAsync(request.Credential);
            if (googleUser == null || !googleUser.EmailVerified) return null;

            var user = await _userRepository.GetByGoogleSubjectAsync(googleUser.Subject)
                ?? await _userRepository.GetByEmailAsync(googleUser.Email);

            if (user == null)
            {
                user = new User
                {
                    FullName = googleUser.FullName,
                    Email = googleUser.Email,
                    PasswordHash = null,
                    Role = "Teacher",
                    SubscriptionTier = "Freemium",
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    GoogleSubject = googleUser.Subject,
                    Authenticator = "Google"
                };

                await _userRepository.AddAsync(user);
            }
            else
            {
                user.GoogleSubject ??= googleUser.Subject;
                user.Authenticator = string.IsNullOrEmpty(user.PasswordHash) ? "Google" : "Both";
                _userRepository.Update(user);
            }

            await _userRepository.SaveChangesAsync();

            var response = _mapper.Map<AuthResponse>(user);
            response.Token = GenerateJwtToken(user);
            return response;
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || user.IsDeleted || IsGoogleOnlyUser(user))
                return;

            var rawToken = GenerateResetToken();
            user.ResetTokenHash = HashResetToken(rawToken);
            user.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(GetPasswordResetDurationMinutes());

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            var resetBaseUrl = _config["Frontend:ResetPasswordUrl"] ?? "http://localhost:5173/reset-password";
            var resetLink = $"{resetBaseUrl}?token={WebUtility.UrlEncode(rawToken)}";
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink);
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                throw new InvalidOperationException("Confirm password does not match.");

            var tokenHash = HashResetToken(request.Token);
            var user = await _userRepository.GetByResetTokenHashAsync(tokenHash);
            if (user == null || user.IsDeleted || user.ResetTokenExpiresAt == null)
                return false;

            if (user.ResetTokenExpiresAt <= DateTime.UtcNow)
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetTokenHash = null;
            user.ResetTokenExpiresAt = null;
            user.Authenticator = user.Authenticator == "Google" ? "Both" : "Local";

            _userRepository.Update(user);
            return await _userRepository.SaveChangesAsync() > 0;
        }

        public static string HashResetToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        private static string GenerateResetToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private int GetPasswordResetDurationMinutes()
        {
            return int.TryParse(_config["Auth:PasswordResetTokenMinutes"], out var minutes)
                ? minutes
                : 30;
        }

        private static bool IsGoogleOnlyUser(User user)
        {
            return user.Authenticator == "Google" && string.IsNullOrEmpty(user.PasswordHash);
        }

        private string GenerateJwtToken(Mascoteach.Data.Models.User user)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

            // Định nghĩa các thông tin bên trong Token (Claims)
            var claims = new List<Claim>
            {
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role), // Role: Teacher, Parent, hoặc Student từ SQL
                new Claim("UserId", user.Id.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["DurationInMinutes"]!)),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}

