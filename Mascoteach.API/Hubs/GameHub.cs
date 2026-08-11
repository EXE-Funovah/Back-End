using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Mascoteach.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Mascoteach.API.Hubs;

public class GameHub : Hub
{
    private readonly IMemoryCache _cache;
    private readonly ILiveSessionService _sessionService;
    private readonly ISessionParticipantService _participantService;
    private readonly ISessionAnswerService _answerService;
    private readonly ILiveGameQuestionService _gameQuestionService;
    private readonly IGuestGameTokenService _guestGameTokenService;

    private static string QuestionKey(string pin) => $"game:question:{pin}";
    private static string CurrentQuestionIdKey(string pin) => $"game:question-id:{pin}";

    public GameHub(
        IMemoryCache cache,
        ILiveSessionService sessionService,
        ISessionParticipantService participantService,
        ISessionAnswerService answerService,
        ILiveGameQuestionService gameQuestionService,
        IGuestGameTokenService guestGameTokenService)
    {
        _cache = cache;
        _sessionService = sessionService;
        _participantService = participantService;
        _answerService = answerService;
        _gameQuestionService = gameQuestionService;
        _guestGameTokenService = guestGameTokenService;
    }

    [Authorize(Roles = "Teacher")]
    public async Task JoinAsHost(string gamePin)
    {
        await EnsureSessionOwnerAsync(gamePin);
        await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
        await Clients.Caller.SendAsync("HostJoined", gamePin);
    }

    public async Task JoinAsStudent(string gamePin, int participantId, string joinToken)
    {
        var session = await _sessionService.GetByPinAsync(gamePin);
        var participant = await _participantService.GetByIdAsync(participantId);

        if (session == null
            || participant == null
            || participant.SessionId != session.Id
            || !IsValidGuestIdentity(joinToken, participant.Id, session.Id))
        {
            throw new HubException("Participant does not belong to this live session.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
        await Clients.Group(gamePin).SendAsync("PlayerJoined", new
        {
            participantId = participant.Id,
            studentName = participant.StudentName,
            connectionId = Context.ConnectionId
        });
    }

    [Authorize(Roles = "Teacher")]
    public async Task StartGame(string gamePin)
    {
        await EnsureSessionOwnerAsync(gamePin);

        if (!await _sessionService.UpdateStatusByPinAsync(gamePin, "Active"))
            throw new HubException("Unable to start this live session.");

        await Clients.Group(gamePin).SendAsync("GameStarted");
    }

    [Authorize(Roles = "Teacher")]
    public async Task SendQuestion(string gamePin, int questionId)
    {
        await EnsureSessionOwnerAsync(gamePin);

        var questionData = await _gameQuestionService.GetForSessionAsync(gamePin, questionId);
        if (questionData == null)
            throw new HubException("Question does not belong to the active live session.");

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(2));

        // Only the sanitized question is cached and sent to students.
        _cache.Set(QuestionKey(gamePin), questionData, cacheOptions);
        _cache.Set(CurrentQuestionIdKey(gamePin), questionData.QuestionId, cacheOptions);

        await Clients.Group(gamePin).SendAsync("NewQuestion", questionData);
    }

    public async Task RequestCurrentQuestion(string gamePin)
    {
        if (_cache.TryGetValue(QuestionKey(gamePin), out LiveGameQuestionResponse? currentQuestion)
            && currentQuestion != null)
        {
            await Clients.Caller.SendAsync("NewQuestion", currentQuestion);
        }
    }

    public async Task SubmitAnswer(
        string gamePin,
        int participantId,
        string joinToken,
        int questionId,
        int optionId)
    {
        var session = await _sessionService.GetByPinAsync(gamePin);
        if (session == null || !IsValidGuestIdentity(joinToken, participantId, session.Id))
        {
            await Clients.Caller.SendAsync("AnswerResult", new SubmitSessionAnswerResult
            {
                ErrorCode = "INVALID_PARTICIPANT_TOKEN",
                Message = "Participant token is invalid or expired."
            });
            return;
        }

        if (!_cache.TryGetValue(CurrentQuestionIdKey(gamePin), out int currentQuestionId)
            || currentQuestionId != questionId)
        {
            await Clients.Caller.SendAsync("AnswerResult", new SubmitSessionAnswerResult
            {
                ErrorCode = "QUESTION_NOT_ACTIVE",
                Message = "This question is not currently accepting answers."
            });
            return;
        }

        var result = await _answerService.SubmitAsync(new SubmitSessionAnswerRequest
        {
            GamePin = gamePin,
            ParticipantId = participantId,
            QuestionId = questionId,
            SelectedOptionId = optionId
        });

        await Clients.Caller.SendAsync("AnswerResult", result);

        if (result.Accepted)
        {
            await Clients.OthersInGroup(gamePin).SendAsync("AnswerSubmitted", new
            {
                participantId,
                studentName = result.StudentName,
                questionId,
                isCorrect = result.IsCorrect,
                timestamp = result.AnsweredAt
            });
        }
    }

    [Authorize(Roles = "Teacher")]
    public async Task CloseQuestion(string gamePin)
    {
        await EnsureSessionOwnerAsync(gamePin);
        _cache.Remove(QuestionKey(gamePin));
        _cache.Remove(CurrentQuestionIdKey(gamePin));
        await Clients.Group(gamePin).SendAsync("QuestionClosed");
    }

    [Authorize(Roles = "Teacher")]
    public async Task BroadcastScores(string gamePin, object scores)
    {
        await EnsureSessionOwnerAsync(gamePin);
        await Clients.Group(gamePin).SendAsync("ScoresUpdated", scores);
    }

    [Authorize(Roles = "Teacher")]
    public async Task EndGame(string gamePin)
    {
        await EnsureSessionOwnerAsync(gamePin);
        _cache.Remove(QuestionKey(gamePin));
        _cache.Remove(CurrentQuestionIdKey(gamePin));

        if (!await _sessionService.UpdateStatusByPinAsync(gamePin, "Ended"))
            throw new HubException("Unable to end this live session.");

        await Clients.Group(gamePin).SendAsync("GameEnded");
    }

    private async Task EnsureSessionOwnerAsync(string gamePin)
    {
        var userIdValue = Context.User?.FindFirst("UserId")?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdValue, out var userId))
            throw new HubException("Authenticated teacher identity is missing.");

        var session = await _sessionService.GetByPinAsync(gamePin);
        if (session == null || session.TeacherId != userId)
            throw new HubException("You do not own this live session.");
    }

    private bool IsValidGuestIdentity(string token, int participantId, int sessionId)
    {
        return _guestGameTokenService.TryValidate(token, out var identity)
            && identity.ParticipantId == participantId
            && identity.SessionId == sessionId;
    }
}
