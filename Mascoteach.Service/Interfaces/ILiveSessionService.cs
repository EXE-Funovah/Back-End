using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface ILiveSessionService
    {
        Task<IEnumerable<LiveSessionResponse>> GetAllAsync();
        Task<IEnumerable<LiveSessionResponse>> GetByTeacherIdAsync(int teacherId);
        Task<LiveSessionResponse?> GetByIdAsync(int id);
        Task<LiveSessionResponse?> GetByPinAsync(string pin);
        Task<LiveSessionResponse> CreateAsync(int teacherId, LiveSessionCreateRequest request);
        Task<bool> UpdateAsync(int id, LiveSessionUpdateRequest request);
        Task<bool> DeleteAsync(int id);
        Task<LiveSessionResponse?> ToggleDeleteAsync(int id);
    }
}
