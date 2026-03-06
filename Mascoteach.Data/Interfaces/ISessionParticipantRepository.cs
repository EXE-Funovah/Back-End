using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface ISessionParticipantRepository : IGenericRepository<SessionParticipant>
    {
        Task<IEnumerable<SessionParticipant>> GetBySessionIdAsync(int sessionId);
        Task<SessionParticipant?> GetAllIncludingDeletedAsync(int id);
    }
}
