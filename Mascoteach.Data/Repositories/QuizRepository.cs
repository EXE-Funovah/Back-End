using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories
{
    public class QuizRepository : GenericRepository<Quiz>, IQuizRepository
    {
        public QuizRepository(MascoteachDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Quiz>> GetByDocumentIdAsync(int documentId)
        {
            return await _context.Quizzes
                .Where(q => q.DocumentId == documentId
                    && q.IsDeleted == false
                    && q.Document.IsDeleted == false
                    && q.Document.Owner.IsDeleted == false)
                .ToListAsync();
        }

        public Task<Quiz?> GetVisibleByIdAsync(int id)
        {
            return _context.Quizzes.FirstOrDefaultAsync(q =>
                q.Id == id
                && q.IsDeleted == false
                && q.Document.IsDeleted == false
                && q.Document.Owner.IsDeleted == false);
        }

        public async Task<IEnumerable<Quiz>> GetMineAsync(int ownerId, string? activityType)
        {
            var query = _context.Quizzes
                .AsNoTracking()
                .Where(q => q.IsDeleted == false
                    && q.Document.IsDeleted == false
                    && q.Document.OwnerId == ownerId);

            if (!string.IsNullOrWhiteSpace(activityType))
                query = query.Where(q => q.ActivityType == activityType);

            return await query
                .Include(q => q.Questions.Where(question => question.IsDeleted == false))
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        public Task<Quiz?> GetOwnedVisibleByIdAsync(int id, int ownerId)
        {
            return _context.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == id
                    && q.IsDeleted == false
                    && q.Document.IsDeleted == false
                    && q.Document.Owner.IsDeleted == false
                    && q.Document.OwnerId == ownerId);
        }

        public async Task<Quiz?> GetDetailByIdAsync(int id, int ownerId)
        {
            return await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.Id == id
                    && q.IsDeleted == false
                    && q.Document.IsDeleted == false
                    && q.Document.OwnerId == ownerId)
                .Include(q => q.Questions
                    .Where(question => question.IsDeleted == false)
                    .OrderBy(question => question.Position))
                .ThenInclude(question => question.Options.Where(option => option.IsDeleted == false))
                .FirstOrDefaultAsync();
        }

        public async Task<Quiz?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _context.Quizzes.FindAsync(id);
        }
    }
}
