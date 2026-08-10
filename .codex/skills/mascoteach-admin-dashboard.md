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

The read-only analytics module adds no database table, migration, runtime configuration, or deployment secret.
The separate Admin Audit module adds the append-only `Admin_Audit_Logs` table but still adds no EF migration,
runtime configuration, or deployment secret.

The Admin Audit module is the deliberate exception to the original read-only module shape:

`AdminAuditController -> IAdminAuditService / IAdminAuditWriter -> AdminAuditService -> IAdminAuditLogRepository -> AdminAuditLogRepository -> MascoteachDbContext`

- `AdminAuditController` is read-only and uses the explicit route `api/Admin/audit-logs`.
- `IAdminAuditWriter` is internal service infrastructure used by Admin mutation services; there is no public audit
  write route.
- `AdminAuditLogRepository` is separate from `AdminRepository` so append-only audit persistence does not turn the
  analytics repository into a mutation repository.

The User command module is separate from analytics reads:

`AdminUserCommandController -> IAdminUserCommandService -> AdminUserCommandService -> IAdminUserCommandRepository / IAdminAuditWriter -> MascoteachDbContext`

- `AdminUserCommandRepository` and `AdminAuditLogRepository` share the same scoped DbContext.
- Role mutation and its audit row commit in one serializable transaction.

The Content command module follows the same separation:

`AdminContentCommandController -> IAdminContentCommandService -> AdminContentCommandService -> IAdminContentCommandRepository / IAdminAuditWriter -> MascoteachDbContext`

- Content mutation and audit use the same scoped DbContext and serializable transaction.
- The command repository loads tracked moderation targets including soft-deleted rows; it does not own analytics reads.

## Authorization

Every Admin endpoint is protected at controller level with:

```csharp
[Authorize(Roles = "Admin")]
```

JWT role authorization works because Mascoteach tokens use `ClaimTypes.Role`.

Admin accounts are provisioned manually through database seed/controlled administration. Public registration must
never grant Admin. `AuthService` only accepts the self-registerable roles `Student`, `Teacher`, and `Parent`,
case-insensitively, and normalizes them to those canonical values.

There is no public API for creating, disabling, or deleting Admin accounts. The Admin-only role mutation can promote
or demote another active account with the safeguards documented below.

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
PATCH /api/Admin/users/{id}/role
PATCH /api/Admin/users/{id}/subscription
PATCH /api/Admin/users/{id}/status
```

- All routes inherit `[Authorize(Roles = "Admin")]`; the two GET routes are read-only.
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
- Role mutation uses a dedicated request body `{ "role": "Teacher|Student|Parent|Admin", "reason": "..." }`;
  values are normalized case-insensitively and `reason` is mandatory with a 500-character maximum.
- Only active targets can be changed. Missing or soft-deleted targets return HTTP 404.
- An Admin cannot change their own role, and the last active Admin cannot be demoted; both return HTTP 409.
- Submitting the current role is an idempotent HTTP 200 no-op and does not create a misleading audit record.
- A successful change writes `User.RoleChanged` at `High` risk with safe role-only before/after JSON in the same
  serializable transaction. If audit persistence fails, the role change is rolled back.
- Subscription mutation uses a dedicated request body with `subscriptionTier`, optional `premiumExpiresAt`, and
  mandatory `reason` (maximum 500 characters). It accepts only `Freemium` or `Premium` case-insensitively.
- Premium requires a future expiry and stores it in UTC. Freemium always clears expiry. The canonical Admin Premium
  invariant remains tier `Premium` plus a non-null future expiry.
- A same-tier/same-expiry request is an HTTP 200 no-op without audit. Successful changes write
  `User.SubscriptionChanged` at `High` risk with safe tier/expiry-only JSON in the same serializable transaction.
- The canonical subscription mutation route is under `/api/Admin/users`; do not add the duplicate backlog route
  `/api/Admin/billing/users/{userId}/subscription`.
- A successful payment webhook arriving later may extend Premium using the existing billing rule; the Admin mutation
  does not alter payment orders or webhook history.
- Status mutation accepts `Active` or `Deleted` case-insensitively plus mandatory `reason`; it maps only to
  `Users.is_deleted` and never hard-deletes account data.
- An Admin cannot lock their own account or the last active Admin. Same-status requests are HTTP 200 no-ops without
  audit. Successful changes write `User.StatusChanged` at `High` risk with status-only before/after JSON in the same
  serializable transaction.
- Deleted accounts cannot use local login, Google login, re-register the same email, or authenticate with an already
  issued JWT. JWT validation also rejects a role claim that no longer matches the database, so role changes require
  a fresh login/token.
- Deferred sequencing decision: add user email notifications for actual lock/restore changes near the end of the
  Admin roadmap. No-op status requests must not send mail. Design delivery retry/outbox and observability first so an
  email-provider failure cannot roll back or obscure the already-committed status/audit transaction.
- Already-established SignalR connections are not forcibly disconnected when an account is locked. Immediate
  realtime kick would require a connection-revocation registry integrated with `GameHub`.
- Manual development smoke testing confirmed lock, blocked same-email registration without creating a replacement,
  and `User.StatusChanged` audit history on 2026-07-15.

Deferred GameHub/SignalR security review:

- `GameHub` currently has no `[Authorize]`, and its methods do not enforce host/student role, session ownership, or
  caller-to-game-PIN membership.
- The frontend supplies an access token through SignalR `accessTokenFactory`, but the backend does not currently
  configure the standard `access_token` query extraction needed by WebSocket/SSE transports.
- Active connections are not closed automatically on JWT expiry or account lock.
- Do not patch this flow piecemeal. When the game flow is revisited, audit join/reconnect/groups and every
  start/question/answer/score/end-game call, then add authorization, ownership, expiry-close, connection revocation,
  and integration/security tests together.
- Project sequencing decision: complete the prioritized Admin flows first and handle this GameHub review last.

### Audit logs

```http
GET /api/Admin/audit-logs?search=&actorUserId=&action=&targetType=&riskLevel=&from=&to=&page=1&pageSize=20
GET /api/Admin/audit-logs/{id}
```

- Both routes require role `Admin`.
- `riskLevel` accepts `Low`, `Medium`, `High`, or `Critical`, case-insensitively.
- `from` is inclusive and `to` is exclusive; `from >= to` returns HTTP 400.
- `page < 1` becomes 1; `pageSize` outside 1 through 100 becomes 20.
- Search covers actor email, action, target type/id, and reason.
- Results sort by `created_at` descending, then id descending.
- List responses omit `beforeJson`, `afterJson`, and `userAgent`; detail includes them for Admin investigation.
- Audit logs are append-only and have no `is_deleted`, update, delete, or public create endpoint.
- `actor_user_id` is nullable with `ON DELETE SET NULL`; `actor_email` is the durable actor snapshot.
- `target_id` is text so it can represent entity ids, PayOS order codes, or future setting keys.
- `reason` is mandatory. Before/after JSON must be valid JSON and must contain only explicitly selected safe fields.
- Never store password/token hashes, S3 keys, checkout/QR data, signatures, or raw webhook payloads in audit JSON.
- Mutation services must use `IAdminAuditWriter` inside the same scoped DbContext transaction as the mutation
  so a data change cannot commit without its audit record.

Schema rollout state:

- Development DB rollout and DB-first scaffold completed on 2026-07-15.
- Production rollout is still required using `Database/admin_audit_logs_rollout.sql` before deploying mutation code.
- Focused Audit tests `12/12`, focused status/Auth/JWT tests `67/67`, focused Document moderation tests `14/14`,
  full suite `288/288`, and solution build passed.
- Manual Swagger smoke test for the list route against the development DB passed with HTTP 200 on 2026-07-15;
  a later role-mutation smoke test also succeeded and appeared as `User.RoleChanged` in audit history.
- Manual subscription smoke testing also changed Premium to Freemium, cleared the current expiry, and appeared as
  `User.SubscriptionChanged` in audit history on 2026-07-15.

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
PATCH /api/Admin/documents/{id}/hide
PATCH /api/Admin/documents/{id}/restore
```

- All routes inherit `[Authorize(Roles = "Admin")]`; the four GET routes are read-only.
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
- Document hide/restore accepts a dedicated request with mandatory `reason` (maximum 500 characters), changes only
  `Documents.is_deleted`, and never deletes the database row or S3 object.
- Same-state requests are HTTP 200 no-ops without audit. Successful changes write `Document.Hidden` or
  `Document.Restored` at `Medium` risk with `isDeleted`-only JSON in the same serializable transaction.
- Document moderation does not cascade changes to `Quizzes.is_deleted`; restore preserves each Quiz/Flashcard's own
  state. Admin restore bypasses upload quota because it is moderation reversal rather than a new upload.
- Normal Quiz, Question, and Option reads require the full hierarchy, Document, and owner to be active; this prevents
  direct-id/list reads from bypassing a hidden Document.
- Quiz/Flashcard hide/restore and processing retry do not exist yet.
- Manual development smoke testing confirmed Document hide/restore and corresponding `Document.Hidden`/
  `Document.Restored` audit history on 2026-07-16.

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
GET /api/Admin/billing/revenue/export?from=&to=&plan=
GET /api/Admin/billing/revenue/series?from=&to=&plan=&granularity=day&timezone=Asia/Ho_Chi_Minh
```

- All five endpoints are read-only and inherit `[Authorize(Roles = "Admin")]`.
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
- Revenue export requires both `from` and `to`, uses `PaidAt` with an inclusive lower bound and exclusive upper
  bound, and limits one export to 366 days. Optional `plan` accepts `PRO_MONTHLY` or `PRO_YEARLY`.
- Revenue export includes only active orders owned by active users with `status == Paid` and non-null `PaidAt`.
  It returns an Excel-compatible UTF-8 BOM CSV ordered by newest payment first and protects text cells from CSV
  formula injection.
- Export columns are limited to order code, user email/name, plan, amount, currency, paid timestamp, and PayOS
  reference. It never exports checkout URL, QR, payment-link id, signature, or raw webhook payload.
- Revenue export is a read-only file operation and does not create an Admin audit row. Dashboard revenue JSON remains
  in `GET /api/Admin/overview`; do not restore the removed legacy `GET /api/Admin/revenue` route.
- Revenue series requires `from` and `to`, accepts an optional paid plan, and limits a request to 366 days. The first
  version supports only `granularity=day`, `timezone=Asia/Ho_Chi_Minh`, and currency `VND`.
- Revenue series uses an inclusive `from`, exclusive `to`, active users/orders, `status == Paid`, and non-null
  `PaidAt`. It buckets payment instants by Vietnam-local calendar day, returns zero-filled missing days, and includes
  total revenue, Paid order count, and rounded average order value for the same filtered rows.
- Revenue series is read-only and does not create an Admin audit row. It is the dynamic chart/filter contract;
  Overview remains the fixed dashboard KPI/12-month series contract and export remains the CSV download contract.
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
