using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories
{
    public class UserStatRepository : GenericRepository<UserStat>, IUserStatRepository
    {
        public UserStatRepository(MascoteachDbContext context) : base(context)
        {
        }

        public async Task<UserStat?> GetByUserIdAsync(int userId)
        {
            return await _context.UserStats
                        .FirstOrDefaultAsync(s => s.UserId == userId);
        }
    }
}
