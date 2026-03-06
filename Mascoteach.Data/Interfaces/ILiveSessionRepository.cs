using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface ILiveSessionRepository : IGenericRepository<LiveSession>
    {
        Task<LiveSession?> GetByPinAsync(string pin);
        Task<IEnumerable<LiveSession>> GetByTeacherIdAsync(int teacherId);
        Task<LiveSession?> GetAllIncludingDeletedAsync(int id);
    }
}
