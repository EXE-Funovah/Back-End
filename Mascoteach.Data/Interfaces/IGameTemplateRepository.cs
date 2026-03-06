using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IGameTemplateRepository : IGenericRepository<GameTemplate>
    {
        Task<GameTemplate?> GetAllIncludingDeletedAsync(int id);
    }
}
