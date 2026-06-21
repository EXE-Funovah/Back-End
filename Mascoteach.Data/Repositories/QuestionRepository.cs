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
                .OrderBy(q => q.Position)
                .ToListAsync();
        }

        public async Task<int> GetNextPositionAsync(int quizId)
        {
            var positions = _context.Questions
                .Where(question => question.QuizId == quizId)
                .Select(question => (int?)question.Position);

            return (await positions.MaxAsync() ?? -1) + 1;
        }

        public async Task<Question?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _context.Questions.FindAsync(id);
        }
    }
}
