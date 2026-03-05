using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetAllIncludingDeletedAsync(int id);
    }
}
