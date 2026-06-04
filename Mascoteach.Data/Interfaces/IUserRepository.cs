using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByGoogleSubjectAsync(string googleSubject);
        Task<User?> GetByResetTokenHashAsync(string resetTokenHash);
        Task<User?> GetByIdIncludingDeletedAsync(int id);
    }
}
