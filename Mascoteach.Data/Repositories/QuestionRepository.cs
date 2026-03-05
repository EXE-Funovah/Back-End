using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories
{
    public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
    {
        public QuestionRepository(MascoteachDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Question>> GetByQuizIdAsync(int quizId)
        {
            return await _context.Questions
                .Where(q => q.QuizId == quizId && q.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<Question?> GetAllIncludingDeletedAsync(int id)
        {
            return await _context.Questions.FindAsync(id);
        }
    }
}
