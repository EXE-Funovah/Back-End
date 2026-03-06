using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories
{
    public class SessionParticipantRepository : GenericRepository<SessionParticipant>, ISessionParticipantRepository
    {
        public SessionParticipantRepository(MascoteachDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SessionParticipant>> GetBySessionIdAsync(int sessionId)
        {
            return await _context.SessionParticipants
                .Where(p => p.SessionId == sessionId && p.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<SessionParticipant?> GetAllIncludingDeletedAsync(int id)
        {
            return await _context.SessionParticipants.FindAsync(id);
        }
    }
}
