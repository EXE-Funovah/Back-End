---
description: |
  Use this skill when working on Mascoteach admin dashboard APIs, admin analytics, KPI cards, revenue metrics,
  MRR/ARR/ARPU, plan distribution, feature usage, account search/filter/pagination, or Admin role access.
  Triggers: "admin", "admin dashboard", "overview", "revenue", "MRR", "ARR", "ARPU", "accounts",
  "paying accounts", "plan distribution", "feature usage", "AdminController", "AdminService".
---

# Mascoteach - Admin Dashboard Skill

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

## API endpoints

### Overview

```http
GET /api/Admin/overview?range=7d|30d|12m
```

- Default range is `30d`.
- `7d`, `30d`, and `12m` map to 7, 30, and 365 days.
- An unknown value currently falls back to 30 days instead of returning validation error.
- Returns `AdminOverviewResponse` with `kpis`, a 12-month `mrrSeries`, and `featureUsage`.

Current KPI keys:

- `users`: all non-deleted accounts and growth relative to accounts created by the beginning of the range.
- `mau`: non-deleted `User_Stats` rows active during the last 30 days; this remains 30 days regardless of `range`.
- `mrr`: current approximate recurring revenue from the active Premium plan mix.
- `conv`: active Premium accounts divided by all non-deleted accounts.

Feature usage currently counts non-deleted Questions, Documents, and LiveSessions. The response labels these as
AI-created questions, uploaded documents, and Treasure Hunt sessions; the database only provides aggregate row
counts and does not prove a question was AI-created or a live session used one specific template.

### Revenue

```http
GET /api/Admin/revenue?range=7d|30d|12m
```

- Returns `AdminRevenueResponse` with `mrr`, `arr`, `arpu`, `mrrSeries`, `planDistribution`, and `funnel`.
- `churnRate`, `ltv`, and `movement` are not implemented because there is no subscription-event tracking.
- The current implementation accepts `range` but does not use it; the revenue series always covers 12 months.
- Funnel currently contains only total created accounts and active paying accounts.

### Accounts

```http
GET /api/Admin/accounts?search=&tier=&page=1&pageSize=20
```

- Searches non-deleted users by `FullName` or `Email`.
- Optional `tier` filters by exact `SubscriptionTier` value.
- `page < 1` is normalized to 1.
- `pageSize` outside 1 through 100 is normalized to 20.
- Results include `UserStat` and are ordered by `LastActiveDate` descending.
- Response includes system-wide `totalAccounts` and `payingAccounts`, plus filtered `total` and paged `items`.
- `questions` comes from `UserStat.TotalQuestionsAnswered`.
- `minutes` is integer division of `TotalLearningSeconds / 60`.
- Account `status` is `on` when Premium is active or last activity was within two days; otherwise it is `idle`.
  The DTO mentions `trial`, but the service does not currently emit it.

## Revenue and Premium calculations

Admin analytics currently treats Premium as active when:

```text
PremiumExpiresAt != null AND PremiumExpiresAt > DateTime.UtcNow
```

This does not also require `SubscriptionTier == "Premium"`, unlike the canonical Billing/document-quota invariant.
Do not copy the Admin-only definition into Billing or quota code. If Admin is aligned with Billing later, update
dashboard expectations and tests together.

Plan attribution for an active Premium user:

1. Read the user's latest `Paid` payment order by `PaidAt`.
2. `PRO_YEARLY` counts as yearly.
3. Any other plan counts as monthly.
4. An active Premium user with no paid order also counts as monthly.

Approximate current recurring metrics:

```text
MRR = monthly users * 119000 + yearly users * 99000
ARR = MRR * 12
ARPU = MRR / active Premium users
```

`99000` is the monthly equivalent of the `1188000` yearly price.

Despite its name, `mrrSeries` is the actual sum of `Payment_Orders.amount` with status `Paid`, grouped by calendar
month for the current month and previous 11 months. Missing months are returned as zero and labels use `T{month}`.
The overview MRR delta compares the last two values from this paid-revenue series.

Payment aggregate queries currently filter `Status == "Paid"` and `PaidAt`, but do not filter
`PaymentOrder.IsDeleted`.

## DTO contracts

- `AdminKpiDto`: `key`, `label`, `value`, `format`, `deltaPercent`, `up`.
- `AdminNamedValueDto`: `label`, `value`, optional `color`.
- `AdminMonthPointDto`: `label`, `value`.
- `AdminOverviewResponse`: `kpis`, `mrrSeries`, `featureUsage`.
- `AdminRevenueResponse`: recurring metrics, series, plan distribution, funnel, and Phase 2 placeholders.
- `AdminAccountsResponse`: global totals, pagination metadata, filtered total, and account items.

Keep these property names stable unless the Admin frontend is updated at the same time.

## Validation

When changing Admin behavior, add or maintain tests for:

- Non-Admin JWTs are forbidden and Admin JWTs are accepted.
- Soft-deleted users and feature rows are excluded.
- Overview range and user delta behavior.
- Premium plan attribution and manual Premium fallback.
- MRR, ARR, ARPU, conversion, and zero-user cases.
- Twelve-month paid-revenue series including zero-filled months and year boundaries.
- Account search, tier filter, pagination normalization, activity status, and totals.
- Registration cannot create an Admin account.

Current test coverage has no Admin-specific service, repository, or controller tests. Treat this as a coverage gap.

Run:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --no-restore
dotnet build EXE101-Mascoteach-Backend.sln --no-restore
```

## Common mistakes

- Do not weaken `[Authorize(Roles = "Admin")]` to ordinary `[Authorize]`.
- Do not allow clients to self-register or promote themselves to Admin.
- Do not call the 12-month paid-order series normalized recurring MRR without changing its calculation.
- Do not assume `range` currently changes the revenue response.
- Do not add Admin mutation endpoints without a separate authorization and audit design.
- Do not add EF migrations for this DB-first project.
- Do not add feature labels that claim tracking precision the database does not provide.
