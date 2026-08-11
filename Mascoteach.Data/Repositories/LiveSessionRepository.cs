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
                .AsNoTracking()
                .Include(s => s.Quiz)
                .Include(s => s.SessionParticipants.Where(participant => !participant.IsDeleted))
                .Where(s => s.TeacherId == teacherId && s.IsDeleted == false)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<LiveSession?> GetReportByIdAsync(int id)
        {
            return await _context.LiveSessions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(session => session.Quiz)
                    .ThenInclude(quiz => quiz.Questions.Where(question => !question.IsDeleted))
                .Include(session => session.SessionParticipants.Where(participant => !participant.IsDeleted))
                    .ThenInclude(participant => participant.SessionAnswers)
                        .ThenInclude(answer => answer.Question)
                .Include(session => session.SessionParticipants.Where(participant => !participant.IsDeleted))
                    .ThenInclude(participant => participant.SessionAnswers)
                        .ThenInclude(answer => answer.SelectedOption)
                .FirstOrDefaultAsync(session => session.Id == id && !session.IsDeleted);
        }

        public async Task<LiveSession?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _context.LiveSessions.FindAsync(id);
        }
    }
}
