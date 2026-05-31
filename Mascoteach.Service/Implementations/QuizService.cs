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
        private readonly IDocumentRepository _documentRepository;
        private readonly IMapper _mapper;

        public QuizService(
            IQuizRepository quizRepository,
            IDocumentRepository documentRepository,
            IMapper mapper)
        {
            _quizRepository = quizRepository;
            _documentRepository = documentRepository;
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

        public async Task<QuizResponse> CreateAsync(int teacherId, QuizCreateRequest request)
        {
            // Verify document belongs to this teacher
            var doc = await _documentRepository.GetByIdAsync(request.DocumentId);
            if (doc == null || doc.TeacherId != teacherId)
                throw new UnauthorizedAccessException("Document does not exist or you do not own it.");

            var quiz = _mapper.Map<Quiz>(request);
            quiz.Status = "AI_Drafted";
            quiz.CreatedAt = DateTime.Now;
            await _quizRepository.AddAsync(quiz);
            await _quizRepository.SaveChangesAsync();
            return _mapper.Map<QuizResponse>(quiz);
        }

        public async Task<bool> UpdateAsync(int id, int teacherId, QuizUpdateRequest request)
        {
            var quiz = await _quizRepository.GetByIdAsync(id);
            if (quiz == null) return false;

            // Check ownership via document
            var doc = await _documentRepository.GetByIdAsync(quiz.DocumentId);
            if (doc == null || doc.TeacherId != teacherId) return false;

            quiz.Title = request.Title;
            quiz.Status = request.Status;

            _quizRepository.Update(quiz);
            return await _quizRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id, int teacherId)
        {
            var quiz = await _quizRepository.GetByIdAsync(id);
            if (quiz == null) return false;

            var doc = await _documentRepository.GetByIdAsync(quiz.DocumentId);
            if (doc == null || doc.TeacherId != teacherId) return false;

            _quizRepository.Delete(quiz);
            return await _quizRepository.SaveChangesAsync() > 0;
        }

        public async Task<QuizResponse?> ToggleDeleteAsync(int id, int teacherId)
        {
            var quiz = await _quizRepository.GetByIdIncludingDeletedAsync(id);
            if (quiz == null) return null;

            var doc = await _documentRepository.GetByIdIncludingDeletedAsync(quiz.DocumentId);
            if (doc == null || doc.TeacherId != teacherId) return null;

            quiz.IsDeleted = !quiz.IsDeleted;
            _quizRepository.Update(quiz);
            await _quizRepository.SaveChangesAsync();
            return _mapper.Map<QuizResponse>(quiz);
        }
    }
}
