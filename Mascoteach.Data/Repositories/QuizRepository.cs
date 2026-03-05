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
                .Where(q => q.DocumentId == documentId && q.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<Quiz?> GetAllIncludingDeletedAsync(int id)
        {
            return await _context.Quizzes.FindAsync(id);
        }
    }
}
