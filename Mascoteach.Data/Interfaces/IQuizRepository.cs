using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IQuizRepository : IGenericRepository<Quiz>
    {
        Task<IEnumerable<Quiz>> GetByDocumentIdAsync(int documentId);
        Task<Quiz?> GetVisibleByIdAsync(int id);
        Task<IEnumerable<Quiz>> GetMineAsync(int ownerId, string? activityType);
        Task<Quiz?> GetOwnedVisibleByIdAsync(int id, int ownerId);
        Task<Quiz?> GetDetailByIdAsync(int id, int ownerId);
        Task<Quiz?> GetByIdIncludingDeletedAsync(int id);
    }
}
