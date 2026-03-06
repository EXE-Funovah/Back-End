using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations
{
    public class SessionParticipantService : ISessionParticipantService
    {
        private readonly ISessionParticipantRepository _sessionParticipantRepository;
        private readonly IMapper _mapper;

        public SessionParticipantService(ISessionParticipantRepository sessionParticipantRepository, IMapper mapper)
        {
            _sessionParticipantRepository = sessionParticipantRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SessionParticipantResponse>> GetAllAsync()
        {
            var participants = await _sessionParticipantRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<SessionParticipantResponse>>(participants);
        }

        public async Task<IEnumerable<SessionParticipantResponse>> GetBySessionIdAsync(int sessionId)
        {
            var participants = await _sessionParticipantRepository.GetBySessionIdAsync(sessionId);
            return _mapper.Map<IEnumerable<SessionParticipantResponse>>(participants);
        }

        public async Task<SessionParticipantResponse?> GetByIdAsync(int id)
        {
            var participant = await _sessionParticipantRepository.GetByIdAsync(id);
            return _mapper.Map<SessionParticipantResponse>(participant);
        }

        public async Task<SessionParticipantResponse> CreateAsync(SessionParticipantCreateRequest request)
        {
            var participant = _mapper.Map<SessionParticipant>(request);
            await _sessionParticipantRepository.AddAsync(participant);
            await _sessionParticipantRepository.SaveChangesAsync();
            return _mapper.Map<SessionParticipantResponse>(participant);
        }

        public async Task<bool> UpdateAsync(int id, SessionParticipantUpdateRequest request)
        {
            var participant = await _sessionParticipantRepository.GetByIdAsync(id);
            if (participant == null) return false;

            participant.StudentName = request.StudentName;
            participant.TotalScore = request.TotalScore;

            _sessionParticipantRepository.Update(participant);
            return await _sessionParticipantRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var participant = await _sessionParticipantRepository.GetByIdAsync(id);
            if (participant == null) return false;

            _sessionParticipantRepository.Delete(participant);
            return await _sessionParticipantRepository.SaveChangesAsync() > 0;
        }

        public async Task<SessionParticipantResponse?> ToggleDeleteAsync(int id)
        {
            var participant = await _sessionParticipantRepository.GetAllIncludingDeletedAsync(id);
            if (participant == null) return null;

            participant.IsDeleted = !participant.IsDeleted;
            _sessionParticipantRepository.Update(participant);
            await _sessionParticipantRepository.SaveChangesAsync();
            return _mapper.Map<SessionParticipantResponse>(participant);
        }
    }
}
