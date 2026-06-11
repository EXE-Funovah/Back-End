using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories
{
    public class QuizAttemptRepository : GenericRepository<QuizAttempt>, IQuizAttemptRepository
    {
        public QuizAttemptRepository(MascoteachDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<QuizAttempt>> GetByUserIdAsync(int userId, DateTime? from, DateTime? to)
        {
            var query = _context.QuizAttempts.Where(a => a.UserId == userId);
            if (from.HasValue) query = query.Where(a => a.CompletedAt >= from.Value);
            if (to.HasValue) query = query.Where(a => a.CompletedAt <= to.Value);
            return await query
                        .OrderByDescending(a => a.CompletedAt)
                        .ToListAsync();
        }
    }
}
