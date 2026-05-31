using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IOptionService
    {
        Task<IEnumerable<OptionResponse>> GetByQuestionIdAsync(int questionId);
        Task<OptionResponse?> GetByIdAsync(int id);
        Task<OptionResponse> CreateAsync(int teacherId, OptionCreateRequest request);
        Task<bool> UpdateAsync(int id, int teacherId, OptionUpdateRequest request);
        Task<bool> DeleteAsync(int id, int teacherId);
        Task<OptionResponse?> ToggleDeleteAsync(int id, int teacherId);
    }
}
