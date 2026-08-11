using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces;

public interface ISessionAnswerRepository : IGenericRepository<SessionAnswer>
{
    Task<SessionAnswer?> GetByParticipantAndQuestionAsync(
        int sessionId,
        int participantId,
        int questionId);
}
