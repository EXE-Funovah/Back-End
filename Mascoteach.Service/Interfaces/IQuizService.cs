using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IQuizService
    {
        Task<IEnumerable<QuizResponse>> GetByDocumentIdAsync(int documentId);
        Task<QuizResponse?> GetByIdAsync(int id);
        Task<QuizResponse> CreateAsync(QuizCreateRequest request);
        Task<bool> UpdateAsync(int id, QuizUpdateRequest request);
        Task<bool> DeleteAsync(int id);
        Task<QuizResponse?> ToggleDeleteAsync(int id);
    }
}
