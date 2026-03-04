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

        public AuthService(IUserRepository userRepository, IMapper mapper, IConfiguration config)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _config = config;
        }
       
        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            var users = await _userRepository.GetAllAsync();
            if (users.Any(u => u.Email == request.Email)) return null;

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
                IsDeleted = false
            };

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            var response = _mapper.Map<AuthResponse>(newUser);
            response.Token = GenerateJwtToken(newUser);
            return response;
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Email == request.Email);

            // Kiểm tra user tồn tại, chưa bị xóa, và dùng BCrypt để so sánh mật khẩu thô với Hash trong DB
            if (user == null || user.IsDeleted || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            var response = _mapper.Map<AuthResponse>(user);
            response.Token = GenerateJwtToken(user);
            return response;
        }

        private string GenerateJwtToken(Mascoteach.Data.Models.User user)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

            // Định nghĩa các thông tin bên trong Token (Claims)
            var claims = new List<Claim>
            {
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

