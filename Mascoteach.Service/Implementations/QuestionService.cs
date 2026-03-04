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
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;

        public QuestionService(IQuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
        }

        public async Task<IEnumerable<Question>> GetQuestionsByQuizAsync(int quizId)
        {
            var allQuestions = await _questionRepository.GetAllAsync();
            return allQuestions.Where(q => q.QuizId == quizId);
        }

        public async Task<Question> CreateQuestionAsync(Question question)
        {
            await _questionRepository.AddAsync(question);
            await _questionRepository.SaveChangesAsync();
            return question;
        }

        public async Task<bool> UpdateQuestionAsync(Question question)
        {
            _questionRepository.Update(question);
            return await _questionRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            var question = await _questionRepository.GetByIdAsync(id);
            if (question == null) return false;

            _questionRepository.Delete(question);
            return await _questionRepository.SaveChangesAsync() > 0;
        }
    }
}
