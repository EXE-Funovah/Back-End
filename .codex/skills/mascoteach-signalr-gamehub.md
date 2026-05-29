---
description: |
  Use this skill when working on Mascoteach realtime live games, SignalR, GameHub, game PIN, host/student join,
  sending questions, submitting answers, leaderboard, session status, score updates, or IMemoryCache game state.
  Triggers: "SignalR", "GameHub", "live game", "game pin", "student join", "host", "SubmitAnswer",
  "leaderboard", "realtime", "StartGame", "EndGame".
---

# Mascoteach - SignalR GameHub Skill

## Current realtime shape

- Hub: `Mascoteach.API/Hubs/GameHub.cs`
- Route: `/hubs/game`
- Registered in `Program.cs` with `builder.Services.AddSignalR()` and `app.MapHub<GameHub>("/hubs/game")`.
- CORS uses `.AllowCredentials()` because SignalR needs it.
- Group name is the `gamePin`.
- Current question and correct option are stored in `IMemoryCache`.

## Hub methods

Current public hub methods:

- `JoinAsHost(string gamePin)`
- `JoinAsStudent(string gamePin, string studentName)`
- `StartGame(string gamePin)`
- `SendQuestion(string gamePin, object questionData)`
- `RequestCurrentQuestion(string gamePin)`
- `SubmitAnswer(string gamePin, string studentName, int questionId, int optionId)`
- `CloseQuestion(string gamePin)`
- `BroadcastScores(string gamePin, object scores)`
- `EndGame(string gamePin)`

## Events sent to clients

- `HostJoined`
- `PlayerJoined`
- `GameStarted`
- `NewQuestion`
- `AnswerSubmitted`
- `QuestionClosed`
- `ScoresUpdated`
- `GameEnded`

Keep event names stable unless the frontend is updated at the same time.

## State rules

- `StartGame` sets live session status to `Active`.
- `EndGame` clears cache and sets status to `Ended`.
- `GetByPinAsync` returns null for ended sessions.
- `SendQuestion` caches current question for late-joining students.
- `CloseQuestion` removes current question and correct option from cache.
- `SubmitAnswer` compares submitted option id to cached correct option id.

## Score update rules

Current score behavior:

- Correct answer gives `+1000`.
- Participant is found by `StudentName` within the session.
- DB update errors are caught so the SignalR connection is not crashed.

When changing scoring:

1. Preserve the broadcast behavior unless asked otherwise.
2. Keep DB updates wrapped in try/catch inside hub methods.
3. Consider duplicate names and duplicate submissions if the task touches fairness or anti-cheat.

## Backend-service boundary

Use `ILiveSessionService` and `ISessionParticipantService` from the hub. Do not inject repositories into the hub unless the service layer lacks required behavior and the user asks for a focused fix.

If a hub change needs reusable business logic, add it to the service layer.

## Validation checklist

- SignalR route remains `/hubs/game`.
- CORS still allows credentials.
- Event names expected by frontend are unchanged or explicitly coordinated.
- Cache keys are scoped by game PIN.
- Ended sessions cannot be joined through PIN lookup.
- `dotnet build EXE101-Mascoteach-Backend.sln --no-restore` succeeds.
