using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations
{
    public class LiveSessionService : ILiveSessionService
    {
        private readonly ILiveSessionRepository _liveSessionRepository;
        private readonly IMapper _mapper;

        public LiveSessionService(ILiveSessionRepository liveSessionRepository, IMapper mapper)
        {
            _liveSessionRepository = liveSessionRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LiveSessionResponse>> GetAllAsync()
        {
            var sessions = await _liveSessionRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<LiveSessionResponse>>(sessions);
        }

        public async Task<IEnumerable<LiveSessionResponse>> GetByTeacherIdAsync(int teacherId)
        {
            var sessions = await _liveSessionRepository.GetByTeacherIdAsync(teacherId);
            return _mapper.Map<IEnumerable<LiveSessionResponse>>(sessions);
        }

        public async Task<LiveSessionResponse?> GetByIdAsync(int id)
        {
            var session = await _liveSessionRepository.GetByIdAsync(id);
            return _mapper.Map<LiveSessionResponse>(session);
        }

        public async Task<LiveSessionResponse?> GetByPinAsync(string pin)
        {
            var session = await _liveSessionRepository.GetByPinAsync(pin);
            return _mapper.Map<LiveSessionResponse>(session);
        }

        public async Task<LiveSessionResponse> CreateAsync(int teacherId, LiveSessionCreateRequest request)
        {
            var gamePin = await GenerateUniquePinAsync();

            var session = _mapper.Map<LiveSession>(request);
            session.TeacherId = teacherId;
            session.GamePin = gamePin;
            session.Status = "Waiting";
            session.CreatedAt = DateTime.Now;

            await _liveSessionRepository.AddAsync(session);
            await _liveSessionRepository.SaveChangesAsync();
            return _mapper.Map<LiveSessionResponse>(session);
        }

        public async Task<bool> UpdateAsync(int id, LiveSessionUpdateRequest request)
        {
            var session = await _liveSessionRepository.GetByIdAsync(id);
            if (session == null) return false;

            session.Status = request.Status;

            _liveSessionRepository.Update(session);
            return await _liveSessionRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var session = await _liveSessionRepository.GetByIdAsync(id);
            if (session == null) return false;

            _liveSessionRepository.Delete(session);
            return await _liveSessionRepository.SaveChangesAsync() > 0;
        }

        public async Task<LiveSessionResponse?> ToggleDeleteAsync(int id)
        {
            var session = await _liveSessionRepository.GetAllIncludingDeletedAsync(id);
            if (session == null) return null;

            session.IsDeleted = !session.IsDeleted;
            _liveSessionRepository.Update(session);
            await _liveSessionRepository.SaveChangesAsync();
            return _mapper.Map<LiveSessionResponse>(session);
        }

        private async Task<string> GenerateUniquePinAsync()
        {
            var random = new Random();
            string pin;
            do
            {
                pin = random.Next(100000, 999999).ToString();
            }
            while (await _liveSessionRepository.GetByPinAsync(pin) != null);
            return pin;
        }
    }
}
