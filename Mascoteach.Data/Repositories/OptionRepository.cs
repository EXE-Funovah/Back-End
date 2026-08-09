using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories
{
    public class OptionRepository : GenericRepository<Option>, IOptionRepository
    {
        public OptionRepository(MascoteachDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Option>> GetByQuestionIdAsync(int questionId)
        {
            return await _context.Options
                .Where(o => o.QuestionId == questionId
                    && o.IsDeleted == false
                    && o.Question.IsDeleted == false
                    && o.Question.Quiz.IsDeleted == false
                    && o.Question.Quiz.Document.IsDeleted == false
                    && o.Question.Quiz.Document.Owner.IsDeleted == false)
                .ToListAsync();
        }

        public Task<Option?> GetVisibleByIdAsync(int id)
        {
            return _context.Options.FirstOrDefaultAsync(o =>
                o.Id == id
                && o.IsDeleted == false
                && o.Question.IsDeleted == false
                && o.Question.Quiz.IsDeleted == false
                && o.Question.Quiz.Document.IsDeleted == false
                && o.Question.Quiz.Document.Owner.IsDeleted == false);
        }

        public async Task<Option?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _context.Options.FindAsync(id);
        }
    }
}
