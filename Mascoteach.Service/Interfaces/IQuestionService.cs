using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IQuestionService
    {
        Task<IEnumerable<QuestionResponse>> GetByQuizIdAsync(int quizId);
        Task<QuestionResponse?> GetByIdAsync(int id);
        Task<QuestionResponse> CreateAsync(int teacherId, QuestionCreateRequest request);
        Task<bool> UpdateAsync(int id, int teacherId, QuestionUpdateRequest request);
        Task<bool> DeleteAsync(int id, int teacherId);
        Task<QuestionResponse?> ToggleDeleteAsync(int id, int teacherId);
    }
}
