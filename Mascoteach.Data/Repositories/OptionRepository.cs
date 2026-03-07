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
                .Where(o => o.QuestionId == questionId && o.IsDeleted == false)
                .ToListAsync();
        }
    }
}
