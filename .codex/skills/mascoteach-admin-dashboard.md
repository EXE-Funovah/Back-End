---
description: |
  Use this skill when working on Mascoteach admin dashboard APIs, admin analytics, KPI cards, revenue metrics,
  MRR/ARR/ARPU, plan distribution, feature usage, account search/filter/pagination, or Admin role access.
  Triggers: "admin", "admin dashboard", "overview", "revenue", "MRR", "ARR", "ARPU", "accounts",
  "paying accounts", "plan distribution", "feature usage", "AdminController", "AdminService".
---

# Mascoteach - Admin Dashboard Skill

## Required roadmap workflow

The canonical living roadmap is `.codex/plans/admin-dashboard-todo.md`.

Before any Admin task:

1. Read the roadmap and the relevant domain rules in `.codex/skills/`.
2. Verify the requested todo against current code and DB-first schema; the roadmap may be older than the code.
3. Mark only the selected item `In Progress`.

After implementation:

1. Run focused tests, the full test suite, and the solution build.
2. Mark an item complete only after fresh verification passes.
3. Record the implemented routes or behavior, verification commands, and any remaining limitation under that item.
4. Update this skill only with durable, verified architecture and business rules, not temporary progress notes.
5. Leave partial or blocked items unchecked and state the exact blocker.

Never store secrets in the roadmap or mark frontend-only work as backend-complete.

## Current module shape

The Admin dashboard is a read-only analytics module following the existing dependency direction:

`AdminController -> IAdminService -> AdminService -> IAdminRepository -> AdminRepository -> MascoteachDbContext`

Files:

- Controller: `Mascoteach.API/Controllers/AdminController.cs`
- Service DTOs: `Mascoteach.Service/DTOs/Admin/AdminDtos.cs`
- Service interface/implementation: `IAdminService` / `AdminService`
- Repository interface/implementation: `IAdminRepository` / `AdminRepository`
- DI registrations: `Mascoteach.API/Program.cs`

`AdminRepository` intentionally does not extend `IGenericRepository<T>`. It owns read-only aggregate queries such
as count, sum, grouping, filtering, and pagination over several existing tables.

The module adds no database table, migration, runtime configuration, or deployment secret.

## Authorization

Every Admin endpoint is protected at controller level with:

```csharp
[Authorize(Roles = "Admin")]
```

JWT role authorization works because Mascoteach tokens use `ClaimTypes.Role`.

Admin accounts are provisioned manually through database seed/controlled administration. Public registration must
never grant Admin. `AuthService` only accepts the self-registerable roles `Student`, `Teacher`, and `Parent`,
case-insensitively, and normalizes them to those canonical values.

There is currently no public API for creating, promoting, demoting, disabling, or deleting Admin accounts.

Related User API boundary:

- `GET /api/User` and `GET /api/User/{id}` require role `Admin`.
- `GET /api/User/me` remains available to any authenticated user.
- `PUT /api/User/{id}` is profile-only and can change only `FullName` and `Email`.
- `Role` and `SubscriptionTier` are not part of `UserUpdateRequest` and must not be reintroduced.
- Future privileged role/subscription changes require dedicated Admin DTOs, endpoints, validation, and audit logs.

## API endpoints

### Users

```http
GET /api/Admin/users?search=&role=&subscription=&page=1&pageSize=20
GET /api/Admin/users/{id}
```

- Both endpoints are read-only and inherit `[Authorize(Roles = "Admin")]`.
- `role` accepts `Teacher`, `Student`, `Parent`, or `Admin`, case-insensitively.
- `subscription` accepts `Freemium`, `Premium`, or `Expired`, case-insensitively.
- `Premium` requires `SubscriptionTier == "Premium"` and a future `PremiumExpiresAt`.
- `Expired` means stored tier `Premium` with missing or elapsed expiry; other tiers classify as `Freemium`.
- Unknown role/subscription filters return HTTP 400.
- `page < 1` becomes 1; `pageSize` outside 1 through 100 becomes 20.
- List results are ordered by `CreatedAt` descending, then `Id` descending.
- Soft-deleted users are excluded; detail returns HTTP 404 for missing/deleted users.
- List responses include document, Quiz, Flashcard, and hosted live-session counts.
- Detail adds learning stats and a non-sensitive latest-payment summary.
- Responses never expose password/token hashes, S3 keys, checkout/QR data, signatures, or webhook payloads.
- Legacy `GET /api/Admin/accounts`, `AdminAccountsResponse`, and their dedicated service/repository methods were
  removed because no frontend consumed them. `/api/Admin/users` is the only Admin user-list contract.
- Repository reads use `AsNoTracking` SQL projections and filter deleted related rows.

There are no Admin role/subscription/status mutations yet. Do not add them before `Admin_Audit_Logs` and dedicated
request DTOs are designed.

### Overview

```http
GET /api/Admin/overview?range=7d|30d|12m
```

- Default range is `30d`; values are normalized case-insensitively.
- `7d` and `30d` use exact day windows; `12m` uses `DateTime.AddMonths(-12)`.
- Unknown values return HTTP 400.
- Response includes `range`, `from`, `to`, KPI cards, role/subscription/content/payment distributions, and
  `paidRevenueSeries`.

Current KPI keys:

- `totalUsers`: all non-deleted accounts; delta is new users divided by users that existed before the range.
- `newUsers`: non-deleted accounts created inside the selected range.
- `activeUsers`: non-deleted `User_Stats` with `LastActiveDate` inside the selected range.
- `paidRevenue`: sum of non-deleted `Paid` Payment Orders whose `PaidAt` is inside the selected range.

Distributions:

- Users: exact stored roles `Teacher`, `Student`, `Parent`, `Admin`.
- Subscription: `Freemium`, active `Premium`, and expired/missing-expiry stored Premium.
- Content: active Documents, Quiz activities, Flashcard activities, LiveSessions, and participant join rows.
- Payment statuses: `Pending`, `Paid`, `Cancelled`, `Expired`, `Failed`.

`ParticipantJoinCount` counts active `Session_Participants` rows. Participants currently store names without user
ids, so this is not a unique-student metric.

`PaidRevenueSeries` is actual paid-order revenue grouped over the current month and previous 11 months. It is not
normalized MRR.

Overview excludes soft-deleted users and related Documents, Quizzes, LiveSessions, SessionParticipants, and
PaymentOrders. AI failures, processing stalls, realtime reconnects, and quota-abuse alerts are excluded because
the current schema has no reliable telemetry for them.

### Content monitoring

```http
GET /api/Admin/documents?search=&ownerId=&deletion=Active&from=&to=&page=1&pageSize=20
GET /api/Admin/documents/{id}
GET /api/Admin/quizzes?search=&ownerId=&activityType=&status=&deletion=Active&from=&to=&page=1&pageSize=20
GET /api/Admin/quizzes/{id}
```

- All four endpoints are read-only and inherit `[Authorize(Roles = "Admin")]`.
- Document search covers file name, owner name, and owner email.
- Quiz search covers title, source file name, owner name, and owner email.
- `deletion` accepts `Active`, `Deleted`, or `All`, case-insensitively; default is `Active`.
- `activityType` accepts `Quiz` or `Flashcard`, case-insensitively.
- `status` accepts `AI_Drafted`, `Teacher_Approved`, or `Published`, case-insensitively.
- `from` is inclusive and `to` is exclusive. `from >= to` returns HTTP 400.
- `page < 1` becomes 1; `pageSize` outside 1 through 100 becomes 20.
- Lists sort by content timestamp descending and then id descending.
- Detail routes can return active or soft-deleted rows and return HTTP 404 for missing ids.
- Document metadata includes owner metadata and active Quiz/Flashcard counts.
- Quiz/Flashcard metadata includes source-document metadata, owner metadata, and active question count.
- Responses never include `Document.FileUrl`, S3 keys, presigned URLs, question/option text, or correct answers.
- Owner metadata is limited to id, name, email, and soft-delete state.
- Repository reads use `AsNoTracking`, SQL-side filters/counts/pagination, and dedicated projections.
- No hide, restore, retry, delete, or content-view action exists yet. Add mutations only after
  `Admin_Audit_Logs`, dedicated request DTOs, and a reason policy are designed.

### Session monitoring

```http
GET /api/Admin/sessions?search=&teacherId=&templateId=&status=&deletion=Active&from=&to=&page=1&pageSize=20
GET /api/Admin/sessions/{id}
GET /api/Admin/sessions/{id}/participants?search=&deletion=Active&page=1&pageSize=20
```

- All three endpoints are read-only and inherit `[Authorize(Roles = "Admin")]`.
- Session search covers game PIN, teacher name/email, quiz title, and template name.
- `status` accepts `Waiting`, `Active`, or `Ended`, case-insensitively.
- `deletion` accepts `Active`, `Deleted`, or `All`, case-insensitively; default is `Active`.
- `from` is inclusive and `to` is exclusive. `from >= to` returns HTTP 400.
- `page < 1` becomes 1; `pageSize` outside 1 through 100 becomes 20.
- Session lists sort by `CreatedAt` descending, then id descending.
- Session metadata includes PIN, teacher, quiz, template, soft-delete states, and active participant-row count.
- Detail can return active or deleted sessions and returns HTTP 404 for missing ids.
- Participant search covers display name. Participants sort by id ascending because no join timestamp exists.
- Participants expose display name and total score. They have no user id, so they are join rows rather than
  verified or unique student accounts.
- Responses omit template JS/thumbnail URLs, quiz content, storage fields, and realtime data.
- The schema has no `ended_at`, participant join timestamp/user id, or reconnect/event history. Do not infer
  duration, unique students, or realtime health.
- No Admin end/delete/restore action exists yet. Add mutation endpoints only after `Admin_Audit_Logs`, dedicated
  request DTOs, and a reason policy are designed.

### Billing monitoring

```http
GET /api/Admin/billing/orders?search=&userId=&status=&plan=&deletion=Active&from=&to=&page=1&pageSize=20
GET /api/Admin/billing/orders/{id}
GET /api/Admin/billing/webhook-events?search=&processed=&hasError=&from=&to=&page=1&pageSize=20
```

- All three endpoints are read-only and inherit `[Authorize(Roles = "Admin")]`.
- Order search covers numeric order code, PayOS reference, user name, and user email.
- `status` accepts `Pending`, `Paid`, `Cancelled`, `Expired`, or `Failed`, case-insensitively.
- `plan` accepts `PRO_MONTHLY` or `PRO_YEARLY`, case-insensitively.
- `deletion` accepts `Active`, `Deleted`, or `All`, case-insensitively; default is `Active`.
- `from` is inclusive and `to` is exclusive. `from >= to` returns HTTP 400.
- `page < 1` becomes 1; `pageSize` outside 1 through 100 becomes 20.
- Order lists sort by `CreatedAt` descending, then id descending.
- Order metadata includes safe payment fields plus user/subscription metadata and canonical active-Premium state.
- Order detail can return active or deleted orders and returns HTTP 404 for missing ids.
- Webhook search covers numeric order code and provider reference.
- `processed` filters `IsProcessed`; `hasError` filters presence of non-empty `ProcessingError`.
- Webhook events sort by `ProcessedAt` descending, then id descending.
- Stored `ProcessingError` is returned for support/debugging.
- Orders never expose payment-link id, checkout URL, or QR code.
- Webhook events never expose payment-link id, signature, or raw payload.
- No sync, retry, cancellation, or manual subscription mutation exists. Add mutations only after
  `Admin_Audit_Logs`, dedicated request DTOs, and a reason policy are designed.

## Revenue and Premium semantics

`GET /api/Admin/overview` is the canonical Admin revenue-summary contract. The legacy
`GET /api/Admin/revenue` route and its approximate MRR/ARR/ARPU vertical slice were removed before frontend
integration because no client consumed them and their range/series semantics were inaccurate.

Admin analytics treats Premium as active when:

```text
SubscriptionTier == "Premium"
AND PremiumExpiresAt != null
AND PremiumExpiresAt > DateTime.UtcNow
```

This matches the canonical Billing/document-quota invariant.

Payment aggregate queries filter `Status == "Paid"`, require `PaidAt`, and exclude soft-deleted payment orders
and their soft-deleted users. Overview returns paid revenue in the selected range plus an actual paid-revenue
series covering the current month and previous 11 months.

## DTO contracts

- `AdminKpiDto`: `key`, `label`, `value`, `format`, `deltaPercent`, `up`.
- `AdminNamedValueDto`: `label`, `value`, optional `color`.
- `AdminMonthPointDto`: `label`, `value`.
- `AdminOverviewResponse`: range window, `kpis`, `userDistribution`, `subscriptionDistribution`,
  `contentTotals`, `paymentStatusDistribution`, and `paidRevenueSeries`.
- `AdminUsersResponse`: pagination metadata, filtered total, and Admin user list items.
- `AdminUserDetailResponse`: user-list fields plus learning and non-sensitive payment summary.
- `AdminDocumentsResponse` / `AdminDocumentItemDto`: paginated Document operational metadata.
- `AdminQuizzesResponse` / `AdminQuizItemDto`: paginated Quiz/Flashcard operational metadata.
- `AdminSessionsResponse` / `AdminSessionItemDto`: paginated session/teacher/quiz/template metadata.
- `AdminSessionParticipantsResponse` / `AdminSessionParticipantDto`: paginated participant display-name/score
  metadata.
- `AdminPaymentOrdersResponse` / `AdminPaymentOrderItemDto`: paginated safe order/user/subscription metadata.
- `AdminWebhookEventsResponse` / `AdminWebhookEventItemDto`: paginated safe webhook processing metadata.

Keep these property names stable unless the Admin frontend is updated at the same time.

## Validation

When changing Admin behavior, add or maintain tests for:

- Non-Admin JWTs are forbidden and Admin JWTs are accepted.
- Soft-deleted users and feature rows are excluded.
- Overview range and user delta behavior.
- Twelve-month paid-revenue series including zero-filled months and year boundaries.
- Account search, tier filter, pagination normalization, activity status, and totals.
- Registration cannot create an Admin account.

Admin User, Content, Session, and Billing service/controller contracts have focused tests. Repository projections
are not currently executed against a real SQL Server in the test suite; smoke-test these Admin routes against
the development database after deployment or when a relational integration-test fixture is added.

Run:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --no-restore
dotnet build EXE101-Mascoteach-Backend.sln --no-restore
```

## Common mistakes

- Do not weaken `[Authorize(Roles = "Admin")]` to ordinary `[Authorize]`.
- Do not allow clients to self-register or promote themselves to Admin.
- Do not expose collection or arbitrary-id User reads to ordinary authenticated users.
- Do not reuse the profile update DTO for role or subscription changes.
- Do not return authentication secrets, storage keys, payment-link data, signatures, or raw webhook payloads from
  Admin User responses.
- Do not reintroduce the removed legacy `/api/Admin/revenue` contract; design Billing read models from current
  product requirements instead.
- Do not add Admin mutation endpoints without a separate authorization and audit design.
- Do not add EF migrations for this DB-first project.
- Do not add feature labels that claim tracking precision the database does not provide.
