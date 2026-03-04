using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Service.Implementations
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepository;

        public QuizService(IQuizRepository quizRepository)
        {
            _quizRepository = quizRepository;
        }
        public async Task<IEnumerable<Quiz>> GetQuizzesByDocumentAsync(int documentId)
        {
            var allQuizzes = await _quizRepository.GetAllAsync();
            return allQuizzes.Where(q => q.DocumentId == documentId);
        }

        public async Task<Quiz?> GetQuizDetailAsync(int quizId)
        {
            // Need to add logic Include questions in Repository
            return await _quizRepository.GetByIdAsync(quizId);  
        }

        public async Task<Quiz> CreateQuizAsync(int documentId, string title)
        {
            var newQuiz = new Quiz
            {
                DocumentId = documentId,
                Title = title,
                Status = "AI_Drafted", // default status from SQL
                CreatedAt = DateTime.Now
            };
            await _quizRepository.AddAsync(newQuiz);
            await _quizRepository.SaveChangesAsync();
            return newQuiz;
        }

        

        
    }
}
