using Mascoteach.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Service.Interfaces
{
    public interface IQuizService
    {
        Task<IEnumerable<Quiz>> GetQuizzesByDocumentAsync(int documentId);
        Task<Quiz?> GetQuizDetailAsync(int quizId);
        Task<Quiz> CreateQuizAsync(int documentId, string title);
    }
}
