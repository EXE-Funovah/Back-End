using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponse>> GetAllUsersAsync();
        Task<UserResponse?> GetByIdAsync(int id);
        Task<UserResponse?> GetCurrentUserAsync(int userId);
        Task<bool> UpdateAsync(int id, UserUpdateRequest request);

        /// <summary>Lưu S3 key ảnh đại diện cho user hiện tại (null = gỡ avatar).</summary>
        Task<UserResponse?> UpdateAvatarAsync(int userId, string? avatarKey);

        Task<bool> DeleteAsync(int id);
        Task<UserResponse?> ToggleDeleteAsync(int id);

        // dùng nội bộ bởi AuthService
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> RegisterUserAsync(User user);
    }
}
