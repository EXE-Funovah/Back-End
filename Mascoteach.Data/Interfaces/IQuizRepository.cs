using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IQuizRepository : IGenericRepository<Quiz>
    {
        Task<IEnumerable<Quiz>> GetByDocumentIdAsync(int documentId);
        Task<IEnumerable<Quiz>> GetMineAsync(int ownerId, string? activityType);
        Task<Quiz?> GetDetailByIdAsync(int id, int ownerId);
        Task<Quiz?> GetByIdIncludingDeletedAsync(int id);
    }
}
