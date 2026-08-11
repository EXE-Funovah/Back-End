using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface ISessionAnswerService
{
    Task<SubmitSessionAnswerResult> SubmitAsync(SubmitSessionAnswerRequest request);
}
