using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IOptionRepository _optionRepository;
        private readonly IMapper _mapper;

        public QuestionService(
            IQuestionRepository questionRepository,
            IOptionRepository optionRepository,
            IMapper mapper)
        {
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<QuestionResponse>> GetByQuizIdAsync(int quizId)
        {
            var questions = await _questionRepository.GetByQuizIdAsync(quizId);
            var responses = _mapper.Map<IEnumerable<QuestionResponse>>(questions).ToList();

            foreach (var response in responses)
            {
                var options = await _optionRepository.GetByQuestionIdAsync(response.Id);
                response.Options = _mapper.Map<List<OptionResponse>>(options);
            }

            return responses;
        }

        public async Task<QuestionResponse?> GetByIdAsync(int id)
        {
            var question = await _questionRepository.GetByIdAsync(id);
            if (question == null) return null;

            var response = _mapper.Map<QuestionResponse>(question);
            var options = await _optionRepository.GetByQuestionIdAsync(id);
            response.Options = _mapper.Map<List<OptionResponse>>(options);
            return response;
        }

        public async Task<QuestionResponse> CreateAsync(QuestionCreateRequest request)
        {
            using var transaction = await _questionRepository.BeginTransactionAsync();
            try
            {
                var question = _mapper.Map<Question>(request);
                await _questionRepository.AddAsync(question);
                await _questionRepository.SaveChangesAsync();

                if (request.Options != null && request.Options.Any())
                {
                    foreach (var optItem in request.Options)
                    {
                        var option = new Option
                        {
                            QuestionId = question.Id,
                            OptionText = optItem.OptionText,
                            IsCorrect = optItem.IsCorrect,
                            IsDeleted = false
                        };
                        await _optionRepository.AddAsync(option);
                    }
                    await _optionRepository.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return await GetByIdAsync(question.Id) ?? _mapper.Map<QuestionResponse>(question);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, QuestionUpdateRequest request)
        {
            var question = await _questionRepository.GetByIdAsync(id);
            if (question == null) return false;

            question.QuestionText = request.QuestionText;
            question.QuestionType = request.QuestionType;

            _questionRepository.Update(question);
            return await _questionRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var question = await _questionRepository.GetByIdAsync(id);
            if (question == null) return false;

            _questionRepository.Delete(question);
            return await _questionRepository.SaveChangesAsync() > 0;
        }

        public async Task<QuestionResponse?> ToggleDeleteAsync(int id)
        {
            var question = await _questionRepository.GetAllIncludingDeletedAsync(id);
            if (question == null) return null;

            question.IsDeleted = !question.IsDeleted;
            _questionRepository.Update(question);
            await _questionRepository.SaveChangesAsync();
            return _mapper.Map<QuestionResponse>(question);
        }
    }
}
