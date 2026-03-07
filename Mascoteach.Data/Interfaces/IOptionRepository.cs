using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces
{
    public interface IOptionRepository : IGenericRepository<Option>
    {
        Task<IEnumerable<Option>> GetByQuestionIdAsync(int questionId);
    }
}
