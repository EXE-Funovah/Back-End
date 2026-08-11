using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface ILiveGameQuestionService
{
    Task<LiveGameQuestionResponse?> GetForSessionAsync(string gamePin, int questionId);
}
