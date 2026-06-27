# Admin Content Monitoring Read-only Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add four Admin-only APIs for safely searching and reading Document and Quiz/Flashcard metadata.

**Architecture:** Extend the existing `AdminController -> IAdminService -> AdminService -> IAdminRepository ->
AdminRepository` slice. Data projections perform SQL-side aggregation without loading content bodies; dedicated
Admin DTOs keep S3 locations, questions, options, and correct answers out of the HTTP contract.

**Tech Stack:** ASP.NET Core 9, EF Core 9 SQL Server, xUnit, Moq.

---

### Task 1: Write failing Admin Content contract tests

**Files:**
- Create: `Mascoteach.Tests/AdminContentServiceTests.cs`
- Modify: `Mascoteach.Tests/AdminControllerTests.cs`

- [x] Add service tests using the wished-for `IAdminService` and `IAdminRepository` contracts.

```csharp
var result = await sut.GetDocumentsAsync(
    "  lesson  ", 7, "deleted", from, to, 0, 500);

repo.Verify(repository => repository.GetDocumentsPageAsync(
    "lesson", 7, "Deleted", from, to, 1, 20), Times.Once);
Assert.Equal(1, result.Page);
Assert.Equal(20, result.PageSize);
```

- [x] Add validation tests for unknown `deletion`, `activityType`, and `status`, plus `from >= to`.
- [x] Add mapping tests proving owner/document/count metadata is returned without storage or content fields.
- [x] Add controller tests proving invalid filters return HTTP 400 and missing details return HTTP 404.
- [x] Run focused tests in Release and confirm RED because Admin Content contracts do not exist.

Run:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~AdminContent|FullyQualifiedName~AdminControllerTests"
```

Expected: compilation fails on missing Admin Content methods/types.

### Task 2: Add Data projections and repository queries

**Files:**
- Create: `Mascoteach.Data/Projections/AdminContentProjections.cs`
- Modify: `Mascoteach.Data/Interfaces/IAdminRepository.cs`
- Modify: `Mascoteach.Data/Repositories/AdminRepository.cs`

- [x] Create `AdminDocumentProjection` with:

```csharp
int Id;
string? FileName;
DateTime? UploadedAt;
bool IsDeleted;
int OwnerId;
string OwnerName;
string OwnerEmail;
bool OwnerIsDeleted;
int QuizCount;
int FlashcardCount;
```

- [x] Create `AdminQuizProjection` with:

```csharp
int Id;
string Title;
string ActivityType;
string Status;
DateTime? CreatedAt;
bool IsDeleted;
int QuestionCount;
int DocumentId;
string? DocumentFileName;
bool DocumentIsDeleted;
int OwnerId;
string OwnerName;
string OwnerEmail;
bool OwnerIsDeleted;
```

- [x] Add repository contracts:

```csharp
Task<(List<AdminDocumentProjection> Items, int Total)> GetDocumentsPageAsync(
    string? search, int? ownerId, string deletion, DateTime? from, DateTime? to, int page, int pageSize);
Task<AdminDocumentProjection?> GetDocumentDetailAsync(int id);
Task<(List<AdminQuizProjection> Items, int Total)> GetQuizzesPageAsync(
    string? search, int? ownerId, string? activityType, string? status, string deletion,
    DateTime? from, DateTime? to, int page, int pageSize);
Task<AdminQuizProjection?> GetQuizDetailAsync(int id);
```

- [x] Implement `AsNoTracking` Document filtering, newest-first ordering, SQL pagination, and active
Quiz/Flashcard counts.
- [x] Implement `AsNoTracking` Quiz filtering, newest-first ordering, SQL pagination, and active question count.
- [x] Project only the approved metadata fields; never select `Document.FileUrl`, question text, option text, or
correct answers.

### Task 3: Add Admin Content DTOs and service behavior

**Files:**
- Create: `Mascoteach.Service/DTOs/Admin/AdminContentDtos.cs`
- Modify: `Mascoteach.Service/Interfaces/IAdminService.cs`
- Modify: `Mascoteach.Service/Implementations/AdminService.cs`

- [x] Create paginated response and metadata item DTOs:

```csharp
AdminDocumentsResponse;
AdminDocumentItemDto;
AdminQuizzesResponse;
AdminQuizItemDto;
```

- [x] Add service contracts:

```csharp
Task<AdminDocumentsResponse> GetDocumentsAsync(
    string? search, int? ownerId, string deletion, DateTime? from, DateTime? to, int page, int pageSize);
Task<AdminDocumentItemDto?> GetDocumentByIdAsync(int id);
Task<AdminQuizzesResponse> GetQuizzesAsync(
    string? search, int? ownerId, string? activityType, string? status, string deletion,
    DateTime? from, DateTime? to, int page, int pageSize);
Task<AdminQuizItemDto?> GetQuizByIdAsync(int id);
```

- [x] Normalize deletion values to `Active|Deleted|All`, activity types to `Quiz|Flashcard`, and statuses to
`AI_Drafted|Teacher_Approved|Published`, case-insensitively.
- [x] Reject unknown filters and `from >= to` with `ArgumentException`.
- [x] Trim empty search to null and normalize pagination to page 1/page size 20.
- [x] Map Data projections into dedicated metadata DTOs.
- [x] Run focused tests and confirm the service tests are GREEN.

### Task 4: Expose HTTP routes and errors

**Files:**
- Modify: `Mascoteach.API/Controllers/AdminController.cs`
- Modify: `Mascoteach.Tests/AdminControllerTests.cs`

- [x] Add:

```http
GET /api/Admin/documents
GET /api/Admin/documents/{id:int}
GET /api/Admin/quizzes
GET /api/Admin/quizzes/{id:int}
```

- [x] Forward query filters to the service and convert `ArgumentException` into HTTP 400.
- [x] Return HTTP 404 for missing Document/Quiz details.
- [x] Keep all routes inside the controller protected by `[Authorize(Roles = "Admin")]`.
- [x] Run focused tests and confirm all Admin Content and controller tests are GREEN.

### Task 5: Verify security, behavior, and durable context

**Files:**
- Modify: `.codex/plans/admin-dashboard-todo.md`
- Modify: `.codex/skills/mascoteach-admin-dashboard.md`
- Modify: `.codex/plans/2026-06-27-admin-content-readonly-implementation.md`

- [x] Search the Admin Content DTO/projection/response contract and confirm none of these fields appear:

```text
FileUrl
S3Key
PresignedUrl
QuestionText
OptionText
IsCorrect
```

- [x] Run focused tests:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~AdminContent|FullyQualifiedName~AdminControllerTests"
```

- [x] Run the full suite:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj -c Release --no-restore
```

- [x] Run the solution build:

```powershell
dotnet build EXE101-Mascoteach-Backend.sln -c Release --no-restore
```

- [x] Run `git diff --check` and review the complete diff.
- [x] Mark the backend Content roadmap item complete only after fresh verification passes.
- [x] Record the four routes, filter semantics, privacy exclusions, and SQL Server smoke-test limitation in the
durable Admin skill.
- [x] Do not commit or push; the user handles repository history.
