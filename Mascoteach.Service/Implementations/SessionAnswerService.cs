using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Service.Implementations;

public sealed class SessionAnswerService : ISessionAnswerService
{
    private const int CorrectAnswerScore = 1000;

    private readonly ILiveSessionRepository _liveSessionRepository;
    private readonly ISessionParticipantRepository _participantRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IOptionRepository _optionRepository;
    private readonly ISessionAnswerRepository _answerRepository;
    private readonly TimeProvider _timeProvider;

    public SessionAnswerService(
        ILiveSessionRepository liveSessionRepository,
        ISessionParticipantRepository participantRepository,
        IQuestionRepository questionRepository,
        IOptionRepository optionRepository,
        ISessionAnswerRepository answerRepository,
        TimeProvider timeProvider)
    {
        _liveSessionRepository = liveSessionRepository;
        _participantRepository = participantRepository;
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
        _answerRepository = answerRepository;
        _timeProvider = timeProvider;
    }

    public async Task<SubmitSessionAnswerResult> SubmitAsync(SubmitSessionAnswerRequest request)
    {
        var gamePin = request.GamePin?.Trim();
        if (string.IsNullOrWhiteSpace(gamePin))
            return Reject("INVALID_GAME_PIN", "Game PIN is required.");

        var session = await _liveSessionRepository.GetByPinAsync(gamePin);
        if (session == null)
            return Reject("SESSION_NOT_FOUND", "Live session does not exist.");

        if (!string.Equals(session.Status, "Active", StringComparison.OrdinalIgnoreCase))
            return Reject("SESSION_NOT_ACTIVE", "Live session is not active.");

        var participant = await _participantRepository.GetByIdAsync(request.ParticipantId);
        if (participant == null || participant.SessionId != session.Id)
            return Reject("PARTICIPANT_NOT_FOUND", "Participant does not belong to this session.");

        var question = await _questionRepository.GetVisibleByIdAsync(request.QuestionId);
        if (question == null || question.QuizId != session.QuizId)
            return Reject("QUESTION_NOT_FOUND", "Question does not belong to this session quiz.");

        var existingAnswer = await _answerRepository.GetByParticipantAndQuestionAsync(
            session.Id,
            participant.Id,
            question.Id);

        if (existingAnswer != null)
            return Duplicate(existingAnswer, participant);

        var selectedOption = await _optionRepository.GetVisibleByIdAsync(request.SelectedOptionId);
        if (selectedOption == null || selectedOption.QuestionId != question.Id)
            return Reject("OPTION_NOT_FOUND", "Selected option does not belong to this question.");

        var previousTotalScore = participant.TotalScore ?? 0;
        var scoreAwarded = selectedOption.IsCorrect ? CorrectAnswerScore : 0;
        var answeredAt = _timeProvider.GetUtcNow().UtcDateTime;

        var answer = new SessionAnswer
        {
            SessionId = session.Id,
            ParticipantId = participant.Id,
            QuestionId = question.Id,
            SelectedOptionId = selectedOption.Id,
            IsCorrect = selectedOption.IsCorrect,
            ScoreAwarded = scoreAwarded,
            AnsweredAt = answeredAt
        };

        participant.TotalScore = previousTotalScore + scoreAwarded;
        await _answerRepository.AddAsync(answer);
        _participantRepository.Update(participant);

        try
        {
            // Both changes share the same scoped DbContext, so SaveChanges is atomic.
            await _answerRepository.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return new SubmitSessionAnswerResult
            {
                AlreadyAnswered = true,
                TotalScore = previousTotalScore,
                StudentName = participant.StudentName,
                ErrorCode = "ALREADY_ANSWERED",
                Message = "Participant has already answered this question."
            };
        }

        return new SubmitSessionAnswerResult
        {
            Accepted = true,
            IsCorrect = selectedOption.IsCorrect,
            ScoreAwarded = scoreAwarded,
            TotalScore = participant.TotalScore.Value,
            StudentName = participant.StudentName,
            AnsweredAt = answeredAt
        };
    }

    private static SubmitSessionAnswerResult Reject(string errorCode, string message)
    {
        return new SubmitSessionAnswerResult
        {
            ErrorCode = errorCode,
            Message = message
        };
    }

    private static SubmitSessionAnswerResult Duplicate(
        SessionAnswer existingAnswer,
        SessionParticipant participant)
    {
        return new SubmitSessionAnswerResult
        {
            AlreadyAnswered = true,
            IsCorrect = existingAnswer.IsCorrect,
            ScoreAwarded = existingAnswer.ScoreAwarded,
            TotalScore = participant.TotalScore ?? 0,
            StudentName = participant.StudentName,
            AnsweredAt = existingAnswer.AnsweredAt,
            ErrorCode = "ALREADY_ANSWERED",
            Message = "Participant has already answered this question."
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        Exception? current = exception;
        while (current != null)
        {
            if (current is SqlException sqlException
                && (sqlException.Number == 2601 || sqlException.Number == 2627))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
