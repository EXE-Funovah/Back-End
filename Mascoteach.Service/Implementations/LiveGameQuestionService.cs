using Mascoteach.Data.Interfaces;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public sealed class LiveGameQuestionService : ILiveGameQuestionService
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IOptionRepository _optionRepository;

    public LiveGameQuestionService(
        ILiveSessionRepository sessionRepository,
        IQuestionRepository questionRepository,
        IOptionRepository optionRepository)
    {
        _sessionRepository = sessionRepository;
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
    }

    public async Task<LiveGameQuestionResponse?> GetForSessionAsync(string gamePin, int questionId)
    {
        var session = await _sessionRepository.GetByPinAsync(gamePin);
        if (session == null
            || !string.Equals(session.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var question = await _questionRepository.GetVisibleByIdAsync(questionId);
        if (question == null || question.QuizId != session.QuizId)
            return null;

        var quizQuestions = (await _questionRepository.GetByQuizIdAsync(session.QuizId)).ToList();
        var options = await _optionRepository.GetByQuestionIdAsync(question.Id);

        return new LiveGameQuestionResponse
        {
            QuestionId = question.Id,
            QuestionText = question.QuestionText,
            Position = question.Position,
            TotalQuestions = quizQuestions.Count,
            Options = options.Select(option => new LiveGameOptionResponse
            {
                OptionId = option.Id,
                Text = option.OptionText
            }).ToList()
        };
    }
}
