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
        private readonly IMapper _mapper;

        public QuestionService(IQuestionRepository questionRepository, IMapper mapper)
        {
            _questionRepository = questionRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<QuestionResponse>> GetByQuizIdAsync(int quizId)
        {
            var questions = await _questionRepository.GetByQuizIdAsync(quizId);
            return _mapper.Map<IEnumerable<QuestionResponse>>(questions);
        }

        public async Task<QuestionResponse?> GetByIdAsync(int id)
        {
            var question = await _questionRepository.GetByIdAsync(id);
            return _mapper.Map<QuestionResponse>(question);
        }

        public async Task<QuestionResponse> CreateAsync(QuestionCreateRequest request)
        {
            var question = _mapper.Map<Question>(request);
            await _questionRepository.AddAsync(question);
            await _questionRepository.SaveChangesAsync();
            return _mapper.Map<QuestionResponse>(question);
        }

        public async Task<bool> UpdateAsync(int id, QuestionUpdateRequest request)
        {
            var question = await _questionRepository.GetByIdAsync(id);
            if (question == null) return false;

            question.QuestionText = request.QuestionText;
            question.Options = request.Options;
            question.CorrectAnswer = request.CorrectAnswer;

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
