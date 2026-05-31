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
        private readonly IQuestionRepository _questionRepository;
        private readonly IQuizRepository _quizRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IMapper _mapper;

        public OptionService(
            IOptionRepository optionRepository,
            IQuestionRepository questionRepository,
            IQuizRepository quizRepository,
            IDocumentRepository documentRepository,
            IMapper mapper)
        {
            _optionRepository = optionRepository;
            _questionRepository = questionRepository;
            _quizRepository = quizRepository;
            _documentRepository = documentRepository;
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

        /// <summary>
        /// Verify ownership: Option → Question → Quiz → Document → TeacherId
        /// </summary>
        private async Task<bool> IsOwnerAsync(int questionId, int teacherId)
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            if (question == null) return false;
            var quiz = await _quizRepository.GetByIdAsync(question.QuizId);
            if (quiz == null) return false;
            var doc = await _documentRepository.GetByIdAsync(quiz.DocumentId);
            return doc != null && doc.TeacherId == teacherId;
        }

        public async Task<OptionResponse> CreateAsync(int teacherId, OptionCreateRequest request)
        {
            if (!await IsOwnerAsync(request.QuestionId, teacherId))
                throw new UnauthorizedAccessException("Question does not exist or you do not own it.");

            var option = _mapper.Map<Option>(request);
            await _optionRepository.AddAsync(option);
            await _optionRepository.SaveChangesAsync();
            return _mapper.Map<OptionResponse>(option);
        }

        public async Task<bool> UpdateAsync(int id, int teacherId, OptionUpdateRequest request)
        {
            var option = await _optionRepository.GetByIdAsync(id);
            if (option == null) return false;
            if (!await IsOwnerAsync(option.QuestionId, teacherId)) return false;

            option.OptionText = request.OptionText;
            option.IsCorrect = request.IsCorrect;

            _optionRepository.Update(option);
            return await _optionRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id, int teacherId)
        {
            var option = await _optionRepository.GetByIdAsync(id);
            if (option == null) return false;
            if (!await IsOwnerAsync(option.QuestionId, teacherId)) return false;

            _optionRepository.Delete(option);
            return await _optionRepository.SaveChangesAsync() > 0;
        }

        public async Task<OptionResponse?> ToggleDeleteAsync(int id, int teacherId)
        {
            var option = await _optionRepository.GetByIdIncludingDeletedAsync(id);
            if (option == null) return null;
            if (!await IsOwnerAsync(option.QuestionId, teacherId)) return null;

            option.IsDeleted = !option.IsDeleted;
            _optionRepository.Update(option);
            await _optionRepository.SaveChangesAsync();
            return _mapper.Map<OptionResponse>(option);
        }
    }
}
