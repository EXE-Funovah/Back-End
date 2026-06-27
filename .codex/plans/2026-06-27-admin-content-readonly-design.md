# Admin Content Monitoring Read-only Design

## Goal

Add Admin-only, read-only APIs for monitoring Documents and Quiz/Flashcard metadata without exposing storage
locations, download links, questions, answers, or other teacher content.

## Scope

The first Content Monitoring slice provides four endpoints:

```http
GET /api/Admin/documents
GET /api/Admin/documents/{id}
GET /api/Admin/quizzes
GET /api/Admin/quizzes/{id}
```

This slice does not add hide, restore, retry, delete, or content-view actions. Those mutations require
`Admin_Audit_Logs`, dedicated request DTOs, and a reason policy.

## API contract

### Documents

`GET /api/Admin/documents` supports:

- `search`: trimmed, case-insensitive match against file name, owner name, or owner email.
- `ownerId`: exact owner id.
- `deletion`: `Active`, `Deleted`, or `All`; default `Active`.
- `from`: inclusive upload timestamp.
- `to`: exclusive upload timestamp.
- `page`: values below 1 normalize to 1.
- `pageSize`: values outside 1 through 100 normalize to 20.

Items are ordered by `UploadedAt` descending, then `Id` descending. Each item contains document id, file name,
upload timestamp, deletion state, owner id/name/email/deletion state, and active Quiz/Flashcard counts.

`GET /api/Admin/documents/{id}` returns the same metadata and counts. It can return active or deleted documents.
Missing ids return HTTP 404.

### Quizzes and flashcards

`GET /api/Admin/quizzes` supports:

- `search`: trimmed, case-insensitive match against title, source file name, owner name, or owner email.
- `ownerId`: exact owner id.
- `activityType`: `Quiz` or `Flashcard`, case-insensitively.
- `status`: `AI_Drafted`, `Teacher_Approved`, or `Published`, case-insensitively.
- `deletion`: `Active`, `Deleted`, or `All`; default `Active`.
- `from`: inclusive creation timestamp.
- `to`: exclusive creation timestamp.
- `page` and `pageSize`: same normalization as Documents.

Items are ordered by `CreatedAt` descending, then `Id` descending. Each item contains quiz id, title, activity
type, status, creation timestamp, deletion state, active question count, source document id/file name/deletion
state, and owner id/name/email/deletion state.

`GET /api/Admin/quizzes/{id}` returns the same metadata. It does not return question or option content. It can
return active or deleted quizzes. Missing ids return HTTP 404.

## Validation and errors

- Unknown `deletion`, `activityType`, or `status` values return HTTP 400.
- `from >= to` returns HTTP 400.
- Details return HTTP 404 when the requested row does not exist.
- Empty search values behave as no search filter.

## Architecture

Keep the existing dependency direction:

```text
AdminController
  -> IAdminService
  -> AdminService
  -> IAdminRepository
  -> AdminRepository
  -> MascoteachDbContext
```

Create dedicated Admin Content DTO and projection files rather than reusing `DocumentResponse`,
`QuizResponse`, or `QuizDetailResponse`. The existing DTOs may expose S3 keys, presigned URLs, or teacher-owned
content and do not match the Admin metadata contract.

Repository queries use `AsNoTracking`, SQL-side filtering/counting/pagination, and projections. The repository
does not generate S3 links and does not load question/option text.

## Security and privacy

- All routes inherit `[Authorize(Roles = "Admin")]`.
- Responses omit `Document.FileUrl`, S3 keys, presigned URLs, question text, option text, and correct answers.
- Owner data is limited to id, display name, email, and soft-delete state.
- Deleted content remains metadata-visible only through Admin detail or an explicit `deletion` filter.
- No database table, migration, secret, or external service call is added.

## Testing

- Service tests cover normalization, valid filters, invalid filters, date validation, pagination, and DTO mapping.
- Controller tests cover HTTP 400 and HTTP 404 behavior.
- Route/authorization tests ensure the new endpoints remain under the Admin-only controller.
- Full tests and Release build must pass before the roadmap item is marked complete.
- SQL Server smoke testing remains required because repository projections are mocked in unit tests.

