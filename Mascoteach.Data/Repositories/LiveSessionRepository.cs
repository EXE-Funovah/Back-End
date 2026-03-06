using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories
{
    public class LiveSessionRepository : GenericRepository<LiveSession>, ILiveSessionRepository
    {
        public LiveSessionRepository(MascoteachDbContext context) : base(context)
        {
        }

        public async Task<LiveSession?> GetByPinAsync(string pin)
        {
            return await _context.LiveSessions
                .FirstOrDefaultAsync(s => s.GamePin == pin && s.IsDeleted == false);
        }

        public async Task<IEnumerable<LiveSession>> GetByTeacherIdAsync(int teacherId)
        {
            return await _context.LiveSessions
                .Where(s => s.TeacherId == teacherId && s.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<LiveSession?> GetAllIncludingDeletedAsync(int id)
        {
            return await _context.LiveSessions.FindAsync(id);
        }
    }
}
