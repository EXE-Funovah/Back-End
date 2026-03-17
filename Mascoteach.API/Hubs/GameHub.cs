using Mascoteach.Data.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Mascoteach.API.Hubs  
{
    public class GameHub : Hub
    {
     

        // Teacher join với role "host"
        public async Task JoinAsHost(string gamePin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
            await Clients.Caller.SendAsync("HostJoined", gamePin);
        }

        // Student join
        public async Task JoinAsStudent(string gamePin, string studentName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
            await Clients.Group(gamePin).SendAsync("PlayerJoined", new
            {
                studentName,
                connectionId = Context.ConnectionId
            });
        }

        // Teacher start game
        public async Task StartGame(string gamePin)
        {
            await Clients.Group(gamePin).SendAsync("GameStarted");
        }

        // Teacher gửi câu hỏi
        public async Task SendQuestion(string gamePin, object questionData)
        {
            await Clients.Group(gamePin).SendAsync("NewQuestion", questionData);
        }

        // Student submit đáp án
        public async Task SubmitAnswer(string gamePin, string studentName,
                                        int questionId, int optionId)
        {
            await Clients.Group(gamePin).SendAsync("AnswerSubmitted", new
            {
                studentName,
                questionId,
                optionId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }
        public async Task CloseQuestion(string gamePin)
        {
            await Clients.Group(gamePin).SendAsync("QuestionClosed");
        }

        // Host broadcast leaderboard
        public async Task BroadcastScores(string gamePin, object scores)
        {
            await Clients.Group(gamePin).SendAsync("ScoresUpdated", scores);
        }

        // Kết thúc game
        public async Task EndGame(string gamePin)
        {
            await Clients.Group(gamePin).SendAsync("GameEnded");
        }
    }
}