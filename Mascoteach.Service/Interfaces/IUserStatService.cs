using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IUserStatService
    {
        /// <summary>Lấy stats của user; tự tạo row 0 nếu chưa có.</summary>
        Task<UserStatResponse> GetOrCreateAsync(int userId);

        /// <summary>Lấy stats của user khác (Teacher/Parent xem học sinh).</summary>
        Task<UserStatResponse?> GetByUserIdAsync(int userId);

        /// <summary>
        /// Áp 1 attempt vào stats: cộng XP, cập nhật streak + totals.
        /// Gọi bên trong transaction của QuizAttemptService.
        /// </summary>
        Task<UserStat> ApplyAttemptAsync(int userId, QuizAttempt attempt);
    }
}
