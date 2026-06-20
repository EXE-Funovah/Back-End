using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces
{
    public interface IQuizService
    {
        Task<IEnumerable<QuizResponse>> GetByDocumentIdAsync(int documentId);
        Task<QuizResponse?> GetByIdAsync(int id);
        Task<IEnumerable<QuizResponse>> GetMineAsync(int ownerId, string? activityType);
        Task<QuizDetailResponse?> GetDetailAsync(int id, int ownerId);
        Task<QuizResponse> CreateAsync(int teacherId, QuizCreateRequest request);
        Task<QuizDetailResponse> PublishAsync(int ownerId, QuizPublishRequest request);
        Task<bool> UpdateAsync(int id, int teacherId, QuizUpdateRequest request);
        Task<bool> DeleteAsync(int id, int teacherId);
        Task<QuizResponse?> ToggleDeleteAsync(int id, int teacherId);
    }
}
