using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations
{
    public class OptionService : IOptionService
    {
        private readonly IOptionRepository _optionRepository;
        private readonly IMapper _mapper;

        public OptionService(IOptionRepository optionRepository, IMapper mapper)
        {
            _optionRepository = optionRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<OptionResponse>> GetByQuestionIdAsync(int questionId)
        {
            var options = await _optionRepository.GetByQuestionIdAsync(questionId);
            return _mapper.Map<IEnumerable<OptionResponse>>(options);
        }

        public async Task<OptionResponse?> GetByIdAsync(int id)
        {
            var option = await _optionRepository.GetByIdAsync(id);
            return _mapper.Map<OptionResponse>(option);
        }

        public async Task<OptionResponse> CreateAsync(OptionCreateRequest request)
        {
            var option = _mapper.Map<Option>(request);
            await _optionRepository.AddAsync(option);
            await _optionRepository.SaveChangesAsync();
            return _mapper.Map<OptionResponse>(option);
        }

        public async Task<bool> UpdateAsync(int id, OptionUpdateRequest request)
        {
            var option = await _optionRepository.GetByIdAsync(id);
            if (option == null) return false;

            option.OptionText = request.OptionText;
            option.IsCorrect = request.IsCorrect;

            _optionRepository.Update(option);
            return await _optionRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var option = await _optionRepository.GetByIdAsync(id);
            if (option == null) return false;

            _optionRepository.Delete(option);
            return await _optionRepository.SaveChangesAsync() > 0;
        }
    }
}
