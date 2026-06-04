using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(MascoteachDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsDeleted == false);
        }

        public async Task<User?> GetByGoogleSubjectAsync(string googleSubject)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.GoogleSubject == googleSubject && u.IsDeleted == false);
        }

        public async Task<User?> GetByResetTokenHashAsync(string resetTokenHash)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.ResetTokenHash == resetTokenHash && u.IsDeleted == false);
        }

        public async Task<User?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }
    }
}
