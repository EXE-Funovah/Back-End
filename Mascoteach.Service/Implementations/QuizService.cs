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
            var quiz = await _quizRepository.GetVisibleByIdAsync(id);
            return _mapper.Map<QuizResponse>(quiz);
        }

        public async Task<QuizResponse> CreateAsync(int teacherId, QuizCreateRequest request)
        {
            // Verify document belongs to this user
            var doc = await _documentRepository.GetByIdAsync(request.DocumentId);
            if (doc == null || doc.OwnerId != teacherId)
                throw new UnauthorizedAccessException("Document does not exist or you do not own it.");

            EnsureValidActivityType(request.ActivityType);

            var quiz = _mapper.Map<Quiz>(request);
            quiz.Status = "AI_Drafted";
            quiz.CreatedAt = DateTime.Now;
            await _quizRepository.AddAsync(quiz);
            await _quizRepository.SaveChangesAsync();
            return _mapper.Map<QuizResponse>(quiz);
        }

        public async Task<IEnumerable<QuizResponse>> GetMineAsync(int ownerId, string? activityType)
        {
            if (!string.IsNullOrWhiteSpace(activityType))
                EnsureValidActivityType(activityType);

            var quizzes = await _quizRepository.GetMineAsync(ownerId, activityType);
            return _mapper.Map<IEnumerable<QuizResponse>>(quizzes);
        }

        public async Task<QuizDetailResponse?> GetDetailAsync(int id, int ownerId)
        {
            var quiz = await _quizRepository.GetDetailByIdAsync(id, ownerId);
            if (quiz == null) return null;

            var response = _mapper.Map<QuizDetailResponse>(quiz);
            response.Questions = response.Questions
                .OrderBy(question => question.Position)
                .ToList();
            return response;
        }

        public async Task<QuizDetailResponse> PublishAsync(int ownerId, QuizPublishRequest request)
        {
            var document = await _documentRepository.GetByIdAsync(request.DocumentId);
            if (document == null || document.OwnerId != ownerId)
                throw new UnauthorizedAccessException("Document does not exist or you do not own it.");

            ValidatePublishRequest(request);

            var quiz = new Quiz
            {
                DocumentId = request.DocumentId,
                Title = request.Title.Trim(),
                ActivityType = request.ActivityType,
                Status = "Teacher_Approved",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                Questions = request.Questions
                    .OrderBy(question => question.Position)
                    .Select(question => new Question
                    {
                        QuestionText = question.QuestionText.Trim(),
                        QuestionType = question.QuestionType,
                        Position = question.Position,
                        IsDeleted = false,
                        Options = question.Options.Select(option => new Option
                        {
                            OptionText = option.OptionText.Trim(),
                            IsCorrect = option.IsCorrect,
                            IsDeleted = false
                        }).ToList()
                    }).ToList()
            };

            using var transaction = await _quizRepository.BeginTransactionAsync();
            try
            {
                await _quizRepository.AddAsync(quiz);
                await _quizRepository.SaveChangesAsync();
                await transaction.CommitAsync();
                return _mapper.Map<QuizDetailResponse>(quiz);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static void ValidatePublishRequest(QuizPublishRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title is required.");

            EnsureValidActivityType(request.ActivityType);

            if (request.Questions == null || request.Questions.Count == 0)
                throw new ArgumentException("At least one question is required.");

            if (request.Questions.Any(question => question.Position < 0)
                || request.Questions.Select(question => question.Position).Distinct().Count() != request.Questions.Count)
                throw new ArgumentException("Question positions must be unique and non-negative.");

            foreach (var question in request.Questions)
            {
                if (string.IsNullOrWhiteSpace(question.QuestionText))
                    throw new ArgumentException("Question text is required.");

                if (request.ActivityType == "Flashcard")
                {
                    if (question.QuestionType != "Flashcard")
                        throw new ArgumentException("Flashcard activity can only contain Flashcard questions.");

                    if (question.Options == null || question.Options.Count != 1)
                        throw new ArgumentException("Each flashcard must contain exactly one back side.");

                    var back = question.Options[0];
                    if (string.IsNullOrWhiteSpace(back.OptionText) || !back.IsCorrect)
                        throw new ArgumentException("Flashcard back side is required and must be marked correct.");
                }
                else
                {
                    if (question.QuestionType != "MultipleChoice")
                        throw new ArgumentException("Quiz activity can only contain MultipleChoice questions.");

                    if (question.Options == null || question.Options.Count < 2)
                        throw new ArgumentException("MultipleChoice questions require at least two options.");

                    if (question.Options.Count(option => option.IsCorrect) != 1
                        || question.Options.Any(option => string.IsNullOrWhiteSpace(option.OptionText)))
                        throw new ArgumentException("MultipleChoice questions require non-empty options and exactly one correct answer.");
                }
            }
        }

        private static void EnsureValidActivityType(string activityType)
        {
            if (activityType is not ("Quiz" or "Flashcard"))
                throw new ArgumentException("Activity type must be Quiz or Flashcard.");
        }

        public async Task<bool> UpdateAsync(int id, int teacherId, QuizUpdateRequest request)
        {
            var quiz = await _quizRepository.GetByIdAsync(id);
            if (quiz == null) return false;

            // Check ownership via document
            var doc = await _documentRepository.GetByIdAsync(quiz.DocumentId);
            if (doc == null || doc.OwnerId != teacherId) return false;

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
            if (doc == null || doc.OwnerId != teacherId) return false;

            _quizRepository.Delete(quiz);
            return await _quizRepository.SaveChangesAsync() > 0;
        }

        public async Task<QuizResponse?> ToggleDeleteAsync(int id, int teacherId)
        {
            var quiz = await _quizRepository.GetByIdIncludingDeletedAsync(id);
            if (quiz == null) return null;

            var doc = await _documentRepository.GetByIdIncludingDeletedAsync(quiz.DocumentId);
            if (doc == null || doc.OwnerId != teacherId) return null;

            quiz.IsDeleted = !quiz.IsDeleted;
            _quizRepository.Update(quiz);
            await _quizRepository.SaveChangesAsync();
            return _mapper.Map<QuizResponse>(quiz);
        }
    }
}
