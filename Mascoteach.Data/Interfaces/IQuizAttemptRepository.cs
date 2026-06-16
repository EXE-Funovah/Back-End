using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IQuizAttemptRepository : IGenericRepository<QuizAttempt>
    {
        Task<IEnumerable<QuizAttempt>> GetByUserIdAsync(int userId, DateTime? from, DateTime? to);
    }
}
