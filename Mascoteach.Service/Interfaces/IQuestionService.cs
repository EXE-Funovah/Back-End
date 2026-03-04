using Mascoteach.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Service.Interfaces
{
    public interface IQuestionService
    {
        Task<IEnumerable<Question>> GetQuestionsByQuizAsync(int quizId);
        Task<Question> CreateQuestionAsync(Question question);
        Task<bool> UpdateQuestionAsync(Question question);
        Task<bool> DeleteQuestionAsync(int id);
    }
}
