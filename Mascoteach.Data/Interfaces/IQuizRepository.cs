using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IQuizRepository : IGenericRepository<Quiz>
    {
        Task<IEnumerable<Quiz>> GetByDocumentIdAsync(int documentId);
        Task<Quiz?> GetAllIncludingDeletedAsync(int id);
    }
}
