using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IUserStatRepository : IGenericRepository<UserStat>
    {
        Task<UserStat?> GetByUserIdAsync(int userId);
    }
}
