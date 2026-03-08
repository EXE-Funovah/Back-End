using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IOptionRepository _optionRepository;
        private readonly IMapper _mapper;

        public QuizService(
            IQuizRepository quizRepository,
            IQuestionRepository questionRepository,
            IOptionRepository optionRepository,
            IMapper mapper)
        {
            _quizRepository = quizRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<QuizResponse>> GetByDocumentIdAsync(int documentId)
        {
            var quizzes = await _quizRepository.GetByDocumentIdAsync(documentId);
            return _mapper.Map<IEnumerable<QuizResponse>>(quizzes);
        }

        public async Task<QuizResponse?> GetByIdAsync(int id)
        {
            var quiz = await _quizRepository.GetByIdAsync(id);
            return _mapper.Map<QuizResponse>(quiz);
        }

        public async Task<QuizResponse> CreateAsync(QuizCreateRequest request)
        {
            var quiz = _mapper.Map<Quiz>(request);
            quiz.Status = "AI_Drafted";
            quiz.CreatedAt = DateTime.Now;
            await _quizRepository.AddAsync(quiz);
            await _quizRepository.SaveChangesAsync();
            return _mapper.Map<QuizResponse>(quiz);
        }

        public async Task<bool> UpdateAsync(int id, QuizUpdateRequest request)
        {
            var quiz = await _quizRepository.GetByIdAsync(id);
            if (quiz == null) return false;

            quiz.Title = request.Title;
            quiz.Status = request.Status;

            _quizRepository.Update(quiz);
            return await _quizRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var quiz = await _quizRepository.GetByIdAsync(id);
            if (quiz == null) return false;

            _quizRepository.Delete(quiz);
            return await _quizRepository.SaveChangesAsync() > 0;
        }

        public async Task<QuizResponse?> ToggleDeleteAsync(int id)
        {
            var quiz = await _quizRepository.GetAllIncludingDeletedAsync(id);
            if (quiz == null) return null;

            quiz.IsDeleted = !quiz.IsDeleted;
            _quizRepository.Update(quiz);
            await _quizRepository.SaveChangesAsync();
            return _mapper.Map<QuizResponse>(quiz);
        }

    }
}
