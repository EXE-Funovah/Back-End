using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories
{
    public class GameTemplateRepository : GenericRepository<GameTemplate>, IGameTemplateRepository
    {
        public GameTemplateRepository(MascoteachDbContext context) : base(context)
        {
        }

        public async Task<GameTemplate?> GetAllIncludingDeletedAsync(int id)
        {
            return await _context.GameTemplates.FindAsync(id);
        }
    }
}
