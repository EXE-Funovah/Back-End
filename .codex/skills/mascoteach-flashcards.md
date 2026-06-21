---
description: |
  Use this skill when working on Mascoteach flashcards, activity types, whole-set publishing, flashcard library
  filters, card ordering, Quiz detail responses, or Questions/Options used as card front/back. Triggers:
  "flashcard", "the on tap", "activityType", "activity_type", "position", "publish quiz",
  "Quiz/publish", "Quiz/me", "quiz detail", "front", "back".
---

# Mascoteach - Flashcard Skill

## Current storage shape

Do not create separate flashcard tables for the current phase.

- `Quizzes` is the shared activity container.
- `Quizzes.activity_type` is `Quiz` or `Flashcard`.
- `Questions.question_type` is `MultipleChoice` or `Flashcard`.
- For a flashcard, `Questions.question_text` is the front.
- For a flashcard, exactly one active `Option` is the back and has `is_correct = true`.
- `Questions.position` is zero-based and preserves UI ordering.

Do not store card difficulty or study progress unless a later feature explicitly introduces those schemas.

## Database rules

- Mascoteach remains DB-first; do not add EF migrations.
- `Quizzes.activity_type` is non-null with default `Quiz` and a check constraint for `Quiz`/`Flashcard`.
- `Questions.question_type` is non-null with default `MultipleChoice` and a check constraint for
  `MultipleChoice`/`Flashcard`.
- `Questions.position` is non-null, non-negative, and defaults to `0`.
- Existing data is classified as `Quiz`.
- Custom reads always filter `is_deleted = 0`.
- Keep indexes for quiz owner/activity lookup, ordered questions, and question options.

Current rollout state:

- Development DB has the flashcard columns, constraints, and indexes.
- Production rollout must be verified before a production backend using these columns starts.
- Use the same idempotent SQL on dev and production; never hardcode `USE MascoteachDB_Dev`.

## Backend endpoints

All endpoints require JWT and derive ownership from `CurrentUserId`.

### Publish a whole activity

`POST /api/Quiz/publish`

Request fields:

- `documentId`
- `title`
- `activityType`: `Quiz` or `Flashcard`
- `questions[]`: `questionText`, `questionType`, `position`, `options[]`

Publish rules:

1. Verify the current user owns the document.
2. Validate the entire request before persistence.
3. Require unique, non-negative positions.
4. For `Flashcard`, require every question type to be `Flashcard` and exactly one non-empty correct option.
5. For `Quiz`, require every question type to be `MultipleChoice`, at least two non-empty options, and exactly
   one correct option.
6. Save Quiz -> Questions -> Options as one entity graph in one transaction.
7. Use status `Teacher_Approved` for successful whole-set publishing.
8. Roll back the whole activity if any save or commit step fails.

Do not let the frontend create a Quiz and then loop over separate Question calls for the new publish flow.

### List current user's activities

`GET /api/Quiz/me?activityType=Quiz|Flashcard`

- Query through document ownership.
- Omit the filter to return both activity types.
- Reject unknown activity types.
- Return `activityType` and `questionCount`.
- Do not return soft-deleted quizzes, documents, or questions.

### Get whole activity detail

`GET /api/Quiz/{id}/detail`

- Scope the query to the current owner.
- Return Quiz with Questions and Options.
- Sort Questions by `position`.
- Filter deleted Quiz, Document, Questions, and Options.
- Load nested content in repository code; do not perform one option query per question.

## Existing CRUD compatibility

Keep the existing endpoints:

- `POST /api/Quiz` creates an `AI_Drafted` container and defaults missing `activityType` to `Quiz`.
- `PUT /api/Quiz/{id}` updates title/status.
- `DELETE /api/Quiz/{id}` soft-deletes through `GenericRepository.Delete`.
- `PATCH /api/Quiz/{id}/toggle-delete` soft-deletes or restores.
- Question and Option CRUD/toggle-delete endpoints remain available.
- Question create accepts optional `position`; when omitted, assign the next position within the quiz.
- Question update changes `position` only when it is supplied.

Do not change `activityType` through the normal Quiz update flow. Converting between Quiz and Flashcard requires
content validation and is outside the current scope.

## Layer boundaries

Follow the existing dependency direction:

`QuizController -> IQuizService -> QuizService -> IQuizRepository -> QuizRepository -> MascoteachDbContext`

- Controller maps HTTP responses and reads `CurrentUserId`.
- Service owns validation, ownership, entity graph construction, and transaction behavior.
- Repository owns EF filtering, includes, ordering, and owner-scoped queries.
- DTOs live in `Mascoteach.Service/DTOs`; use AutoMapper for entity responses.
- No new DI registration is needed when extending existing Quiz/Question repositories and services.

## Frontend and AI contract

- Frontend may keep internal values `quiz`/`flashcards`, but API values must be exact: `Quiz`/`Flashcard`.
- Frontend publish should call `POST /api/Quiz/publish` once.
- Library should call `GET /api/Quiz/me`, not fetch quizzes once per document.
- Detail views should call `GET /api/Quiz/{id}/detail`, not infer activity type from questions.
- AI service already returns flashcards as Question/Option payloads and needs no backend-driven change for this phase.
- Do not send Flashcard activities into QuizAttempt or GameHub behavior unless a separate product rule is designed.

## Validation

Run:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --filter "QuizServiceTests|QuestionServiceTests|QuizControllerTests" --no-restore
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --no-restore
dotnet build EXE101-Mascoteach-Backend.sln --no-restore
```

Required coverage includes valid publish, ownership rejection, invalid card backs/options, duplicate positions,
transaction rollback, activity filtering, ordered detail, JWT user propagation, and compatibility with existing Quiz CRUD.

## Common mistakes

- Do not create `Flashcards`/`Flashcard_Sets` tables for this phase.
- Do not infer a set type by inspecting its first question; use `Quizzes.activity_type`.
- Do not trust user id, status, score, or correctness from an unvalidated client flow.
- Do not save each card in a separate transaction.
- Do not omit `position` when publishing a whole set.
- Do not use lowercase/plural API values such as `flashcards`.
- Do not deploy production backend before production schema rollout.

