using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IOptionService
    {
        Task<IEnumerable<OptionResponse>> GetByQuestionIdAsync(int questionId);
        Task<OptionResponse?> GetByIdAsync(int id);
        Task<OptionResponse> CreateAsync(OptionCreateRequest request);
        Task<bool> UpdateAsync(int id, OptionUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
