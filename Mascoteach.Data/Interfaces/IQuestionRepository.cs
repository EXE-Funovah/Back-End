using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IQuestionRepository : IGenericRepository<Question>
    {
        Task<IEnumerable<Question>> GetByQuizIdAsync(int quizId);
        Task<Question?> GetVisibleByIdAsync(int id);
        Task<int> GetNextPositionAsync(int quizId);
        Task<Question?> GetByIdIncludingDeletedAsync(int id);
    }
}
