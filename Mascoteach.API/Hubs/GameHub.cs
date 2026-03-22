using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Mascoteach.Service.Interfaces;
using Mascoteach.Service.DTOs;

namespace Mascoteach.API.Hubs
{
    public class GameHub : Hub
    {
        private readonly IMemoryCache _cache;
        private readonly ILiveSessionService _sessionService;
        private readonly ISessionParticipantService _participantService;

        // Cache keys
        private static string QuestionKey(string pin) => $"game:question:{pin}";
        private static string CorrectOptionKey(string pin) => $"game:correct:{pin}";

        public GameHub(
            IMemoryCache cache,
            ILiveSessionService sessionService,
            ISessionParticipantService participantService)
        {
            _cache = cache;
            _sessionService = sessionService;
            _participantService = participantService;
        }

        // ── Teacher join ──
        public async Task JoinAsHost(string gamePin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
            await Clients.Caller.SendAsync("HostJoined", gamePin);
        }

        // ── Student join ──
        public async Task JoinAsStudent(string gamePin, string studentName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
            await Clients.Group(gamePin).SendAsync("PlayerJoined", new
            {
                studentName,
                connectionId = Context.ConnectionId
            });
        }

        // ── Teacher start game ──
        public async Task StartGame(string gamePin)
        {
            // Update status → Active trong DB
            var session = await _sessionService.GetByPinAsync(gamePin);
            if (session != null)
            {
                await _sessionService.UpdateAsync(session.Id, new LiveSessionUpdateRequest
                {
                    Status = "Active"
                });
            }

            await Clients.Group(gamePin).SendAsync("GameStarted");
        }

        // ── Teacher gửi câu hỏi ──
        public async Task SendQuestion(string gamePin, object questionData)
        {
            // Lưu câu hỏi hiện tại vào cache (TTL 2 giờ)
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(2));

            _cache.Set(QuestionKey(gamePin), questionData, cacheOptions);

            // Lưu correctOptionIndex nếu teacher gửi kèm
            if (questionData is System.Text.Json.JsonElement json &&
                json.TryGetProperty("correctOptionIndex", out var correctProp))
            {
                _cache.Set(CorrectOptionKey(gamePin), correctProp.GetInt32(), cacheOptions);
            }

            await Clients.Group(gamePin).SendAsync("NewQuestion", questionData);
        }

        // ── Student join muộn, lấy câu hỏi đang hiển thị ──
        public async Task RequestCurrentQuestion(string gamePin)
        {
            if (_cache.TryGetValue(QuestionKey(gamePin), out var currentQuestion)
                && currentQuestion != null)
            {
                await Clients.Caller.SendAsync("NewQuestion", currentQuestion);
            }
        }

        // ── Student submit đáp án ──
        public async Task SubmitAnswer(string gamePin, string studentName,
                                        int questionId, int optionId)
        {
            // Tính isCorrect dựa trên cache
            bool isCorrect = false;
            if (_cache.TryGetValue(CorrectOptionKey(gamePin), out int correctOptionId))
            {
                isCorrect = optionId == correctOptionId;
            }

            // Broadcast cho host biết có người trả lời
            await Clients.OthersInGroup(gamePin).SendAsync("AnswerSubmitted", new
            {
                studentName,
                questionId,
                optionId,
                isCorrect,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            // Nếu đúng → cộng điểm vào DB
            if (isCorrect)
            {
                try
                {
                    var session = await _sessionService.GetByPinAsync(gamePin);
                    if (session != null)
                    {
                        var participants = await _participantService
                            .GetBySessionIdAsync(session.Id);

                        var participant = participants
                            .FirstOrDefault(p => p.StudentName == studentName);

                        if (participant != null)
                        {
                            var newScore = (participant.TotalScore ?? 0) + 1000;
                            await _participantService.UpdateAsync(participant.Id,
                                new SessionParticipantUpdateRequest
                                {
                                    StudentName = studentName,
                                    TotalScore = newScore
                                });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Không để lỗi DB crash SignalR connection
                    Console.WriteLine($"[GameHub] SubmitAnswer score update error: {ex.Message}");
                }
            }
        }

        // ── Teacher đóng câu hỏi ──
        public async Task CloseQuestion(string gamePin)
        {
            _cache.Remove(QuestionKey(gamePin));
            _cache.Remove(CorrectOptionKey(gamePin));
            await Clients.Group(gamePin).SendAsync("QuestionClosed");
        }

        // ── Host broadcast leaderboard ──
        public async Task BroadcastScores(string gamePin, object scores)
        {
            await Clients.Group(gamePin).SendAsync("ScoresUpdated", scores);
        }

        // ── Kết thúc game ──
        public async Task EndGame(string gamePin)
        {
            // Dọn cache
            _cache.Remove(QuestionKey(gamePin));
            _cache.Remove(CorrectOptionKey(gamePin));

            // Update status → Ended trong DB
            try
            {
                var session = await _sessionService.GetByPinAsync(gamePin);
                if (session != null)
                {
                    await _sessionService.UpdateAsync(session.Id, new LiveSessionUpdateRequest
                    {
                        Status = "Ended"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameHub] EndGame DB update error: {ex.Message}");
            }

            await Clients.Group(gamePin).SendAsync("GameEnded");
        }
    }
}