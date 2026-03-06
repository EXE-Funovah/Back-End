using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations
{
    public class GameTemplateService : IGameTemplateService
    {
        private readonly IGameTemplateRepository _gameTemplateRepository;
        private readonly IMapper _mapper;

        public GameTemplateService(IGameTemplateRepository gameTemplateRepository, IMapper mapper)
        {
            _gameTemplateRepository = gameTemplateRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<GameTemplateResponse>> GetAllAsync()
        {
            var templates = await _gameTemplateRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<GameTemplateResponse>>(templates);
        }

        public async Task<GameTemplateResponse?> GetByIdAsync(int id)
        {
            var template = await _gameTemplateRepository.GetByIdAsync(id);
            return _mapper.Map<GameTemplateResponse>(template);
        }

        public async Task<GameTemplateResponse> CreateAsync(GameTemplateCreateRequest request)
        {
            var template = _mapper.Map<GameTemplate>(request);
            await _gameTemplateRepository.AddAsync(template);
            await _gameTemplateRepository.SaveChangesAsync();
            return _mapper.Map<GameTemplateResponse>(template);
        }

        public async Task<bool> UpdateAsync(int id, GameTemplateUpdateRequest request)
        {
            var template = await _gameTemplateRepository.GetByIdAsync(id);
            if (template == null) return false;

            template.Name = request.Name;
            template.JsBundleUrl = request.JsBundleUrl;
            template.ThumbnailUrl = request.ThumbnailUrl;

            _gameTemplateRepository.Update(template);
            return await _gameTemplateRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var template = await _gameTemplateRepository.GetByIdAsync(id);
            if (template == null) return false;

            _gameTemplateRepository.Delete(template);
            return await _gameTemplateRepository.SaveChangesAsync() > 0;
        }

        public async Task<GameTemplateResponse?> ToggleDeleteAsync(int id)
        {
            var template = await _gameTemplateRepository.GetAllIncludingDeletedAsync(id);
            if (template == null) return null;

            template.IsDeleted = !template.IsDeleted;
            _gameTemplateRepository.Update(template);
            await _gameTemplateRepository.SaveChangesAsync();
            return _mapper.Map<GameTemplateResponse>(template);
        }
    }
}
