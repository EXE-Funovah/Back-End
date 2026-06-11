---
description: |
  Use this skill when modifying Mascoteach gamification, XP, streak, user stats, quiz attempts,
  student practice quiz submission, or any endpoint touching User_Stats / Quiz_Attempts.
  Triggers: "gamification", "XP", "streak", "UserStats", "QuizAttempt", "student practice",
  "submit quiz", "farm XP", "leaderboard".
---

# Mascoteach - Gamification Skill

## Current phase

Phase 1 gamification is for logged-in students practicing quizzes from their own uploaded PDFs.

Out of scope unless the user explicitly asks:

- Teacher assignment workflow.
- `attempt_type` / `assignment_id`.
- Live-session XP.
- `Documents.visibility`.
- Refactoring `Documents.teacher_id` to `owner_id`.
- Teacher/student or parent/student relationship checks.

## Database shape

Gamification currently depends on:

- `User_Stats`: one row per user for total XP, streak, last activity, and aggregate quiz counters.
- `Quiz_Attempts`: one row per submitted quiz attempt.
- Existing `Quizzes`, `Questions`, `Options`, `Documents`.

`Documents.teacher_id` is a legacy owner column. Until the DB is refactored, treat it as the document owner id for both teachers and students.

## Submit quiz rules

Never trust the client to submit score fields.

`QuizAttemptSubmitRequest` must receive selected answers only:

- `quizId`
- `durationSeconds`
- `answers[]` with `questionId` and `optionId`

Do not accept client-supplied `correctCount`, `totalQuestions`, or `xpEarned`.

The backend must compute:

- `CorrectCount`
- `TotalQuestions`
- `XpEarned`

Required validation before awarding XP:

1. Quiz exists and is not deleted.
2. Quiz document exists and is not deleted.
3. Current user owns the quiz document: `quiz.DocumentId -> Documents.teacher_id == CurrentUserId`.
4. Quiz has questions.
5. Submitted answer count equals quiz question count.
6. No duplicate submitted `questionId`.
7. Every submitted question belongs to the quiz.
8. Every selected option belongs to the submitted question.
9. Correctness is read from `Options.is_correct` on the server.

Persist the quiz attempt and the user stats update in one transaction so XP/streak cannot be partially saved.

## User stats endpoint rules

Expose only:

- `GET /api/UserStats/me`

Do not expose `GET /api/UserStats/{userId}` until the product has real teacher/student or parent/student relationship rules. Role-only access is not enough because it leaks unrelated users' stats.

## Subscription rule

Do not add mock subscription upgrade endpoints in backend feature branches.

Freemium/Premium should not be user-controlled through a public API because it can bypass document quota rules. Only add subscription changes when there is a real payment/admin workflow and authorization model.

## Tests required

When changing gamification logic, keep or add tests for:

- Backend-side answer scoring.
- XP/streak update after a valid attempt.
- Rejecting quiz attempts for another user's document.
- Rejecting question ids outside the quiz.
- Rejecting option ids outside the question.
- Rejecting duplicate question ids.
- Same-day streak does not increment twice.
- Consecutive-day streak increments.
- Broken streak resets.

Run:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --filter "QuizAttemptServiceTests|UserStatServiceTests" --no-restore
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --no-restore
dotnet build EXE101-Mascoteach-Backend.sln --no-restore
```
