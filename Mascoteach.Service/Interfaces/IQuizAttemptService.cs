using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IQuizAttemptService
    {
        /// <summary>Ghi nhận 1 lần làm quiz: lưu attempt + cập nhật UserStat (atomic).</summary>
        Task<QuizAttemptResponse> SubmitAsync(int userId, QuizAttemptSubmitRequest request);

        /// <summary>Lịch sử attempt của user (lọc theo khoảng thời gian cho week chart).</summary>
        Task<IEnumerable<QuizAttemptResponse>> GetMineAsync(int userId, DateTime? from, DateTime? to);
    }
}
