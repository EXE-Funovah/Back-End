using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories;

public class SessionAnswerRepository : GenericRepository<SessionAnswer>, ISessionAnswerRepository
{
    public SessionAnswerRepository(MascoteachDbContext context) : base(context)
    {
    }

    public Task<SessionAnswer?> GetByParticipantAndQuestionAsync(
        int sessionId,
        int participantId,
        int questionId)
    {
        return _context.SessionAnswers
            .AsNoTracking()
            .FirstOrDefaultAsync(answer =>
                answer.SessionId == sessionId
                && answer.ParticipantId == participantId
                && answer.QuestionId == questionId);
    }
}
