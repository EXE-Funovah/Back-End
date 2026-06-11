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
        Task<bool> DeleteAsync(int id);
        Task<UserResponse?> ToggleDeleteAsync(int id);

        /// <summary>Nâng cấp gói (mock payment): set SubscriptionTier = "Premium".</summary>
        Task<UserResponse?> UpgradeSubscriptionAsync(int userId);

        // dùng nội bộ bởi AuthService
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> RegisterUserAsync(User user);
    }
}
