using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IQuizService
    {
        Task<IEnumerable<QuizResponse>> GetByDocumentIdAsync(int documentId);
        Task<QuizResponse?> GetByIdAsync(int id);
        Task<QuizResponse> CreateAsync(int teacherId, QuizCreateRequest request);
        Task<bool> UpdateAsync(int id, int teacherId, QuizUpdateRequest request);
        Task<bool> DeleteAsync(int id, int teacherId);
        Task<QuizResponse?> ToggleDeleteAsync(int id, int teacherId);
    }
}
