using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IGameTemplateService
    {
        Task<IEnumerable<GameTemplateResponse>> GetAllAsync();
        Task<GameTemplateResponse?> GetByIdAsync(int id);
        Task<GameTemplateResponse> CreateAsync(GameTemplateCreateRequest request);
        Task<bool> UpdateAsync(int id, GameTemplateUpdateRequest request);
        Task<bool> DeleteAsync(int id);
        Task<GameTemplateResponse?> ToggleDeleteAsync(int id);
    }
}
