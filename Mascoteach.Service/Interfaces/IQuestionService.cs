using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IQuestionService
    {
        Task<IEnumerable<QuestionResponse>> GetByQuizIdAsync(int quizId);
        Task<QuestionResponse?> GetByIdAsync(int id);
        Task<QuestionResponse> CreateAsync(QuestionCreateRequest request);
        Task<bool> UpdateAsync(int id, QuestionUpdateRequest request);
        Task<bool> DeleteAsync(int id);
        Task<QuestionResponse?> ToggleDeleteAsync(int id);
    }
}
