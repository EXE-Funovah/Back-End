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
        Task<bool> UpdateAsync(int id, int teacherId, LiveSessionUpdateRequest request);
        /// <summary>
        /// Internal update used by GameHub (no ownership check — hub validates via game PIN).
        /// </summary>
        Task<bool> UpdateStatusByPinAsync(string gamePin, string status);
        Task<bool> DeleteAsync(int id, int teacherId);
        Task<LiveSessionResponse?> ToggleDeleteAsync(int id, int teacherId);
    }
}
