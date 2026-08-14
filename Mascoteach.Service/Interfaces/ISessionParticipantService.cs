using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface ISessionParticipantService
    {
        Task<IEnumerable<SessionParticipantResponse>> GetAllAsync();
        Task<IEnumerable<SessionParticipantResponse>> GetBySessionIdAsync(int sessionId);
        Task<SessionParticipantResponse?> GetByIdAsync(int id);
        Task<SessionParticipantResponse> CreateAsync(
            SessionParticipantCreateRequest request,
            int? studentId = null);
        Task<bool> UpdateAsync(int id, SessionParticipantUpdateRequest request);
        Task<bool> DeleteAsync(int id);
        Task<SessionParticipantResponse?> ToggleDeleteAsync(int id);
    }
}
