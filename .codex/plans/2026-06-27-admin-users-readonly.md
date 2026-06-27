# Admin Users Read-Only API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Admin-only paginated user list and detail APIs with operational aggregates, using `/api/Admin/users` as the single Admin user-management contract.

**Architecture:** Extend the existing `AdminController -> IAdminService -> AdminService -> IAdminRepository -> AdminRepository` flow. Repository queries project directly to Data-layer read models with `AsNoTracking`, service validates canonical filters and maps to Admin DTOs, and controller only translates service outcomes to HTTP responses.

**Tech Stack:** ASP.NET Core 9, EF Core 9 SQL Server, xUnit, Moq.

---

### Task 1: Define tests for the Admin Users contract

**Files:**
- Create: `Mascoteach.Tests/AdminUserServiceTests.cs`
- Create: `Mascoteach.Tests/AdminControllerTests.cs`

- [x] Test that list requests normalize role/subscription casing and invalid pagination.
- [x] Test that unknown role and subscription filters throw `ArgumentException` before repository access.
- [x] Test that list projections map aggregate counts and subscription status.
- [x] Test that detail returns `null` when the repository cannot find an active user.
- [x] Test that Admin controller routes are `GET /api/Admin/users` and `GET /api/Admin/users/{id}` and remain covered by `[Authorize(Roles = "Admin")]`.
- [x] Run:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --filter "AdminUserServiceTests|AdminControllerTests" --no-restore
```

Expected: tests fail because the new service/repository methods and routes do not exist.

### Task 2: Add Data-layer read projections and queries

**Files:**
- Create: `Mascoteach.Data/Projections/AdminUserProjection.cs`
- Modify: `Mascoteach.Data/Interfaces/IAdminRepository.cs`
- Modify: `Mascoteach.Data/Repositories/AdminRepository.cs`

- [x] Add one read projection containing identity, subscription, activity, content counts, learning stats, and latest non-deleted payment summary.
- [x] Add paginated query with optional name/email search, canonical role, and canonical subscription status.
- [x] Treat active Premium as `SubscriptionTier == "Premium"` plus a future `PremiumExpiresAt`.
- [x] Classify stored Premium with missing/elapsed expiry as `Expired`; classify other tiers as `Freemium`.
- [x] Filter soft-deleted Users, UserStats, Documents, Quizzes, LiveSessions, and PaymentOrders.
- [x] Order list results by `CreatedAt` descending, then `Id` descending.
- [x] Use `AsNoTracking` and project counts in SQL rather than issuing one query per user.

### Task 3: Add Service DTOs, validation, and mapping

**Files:**
- Create: `Mascoteach.Service/DTOs/Admin/AdminUserDtos.cs`
- Modify: `Mascoteach.Service/Interfaces/IAdminService.cs`
- Modify: `Mascoteach.Service/Implementations/AdminService.cs`

- [x] Add `AdminUsersResponse`, `AdminUserListItemDto`, and `AdminUserDetailResponse`.
- [x] Accept roles `Teacher`, `Student`, `Parent`, `Admin` case-insensitively and normalize their values.
- [x] Accept subscriptions `Freemium`, `Premium`, `Expired` case-insensitively and normalize their values.
- [x] Normalize `page < 1` to 1 and `pageSize` outside 1 through 100 to 20.
- [x] Map repository projections without exposing password hashes, token hashes, S3 keys, payment links, QR data, or webhook payloads.

### Task 4: Add thin Admin controller actions

**Files:**
- Modify: `Mascoteach.API/Controllers/AdminController.cs`

- [x] Add `GET /api/Admin/users`.
- [x] Add `GET /api/Admin/users/{id:int}`.
- [x] Return 400 for invalid filters and 404 for a missing/deleted user.
- [x] Remove legacy `GET /api/Admin/accounts` and its dedicated DTO/service/repository code because no frontend consumes it.
- [x] Run the focused tests and confirm they pass.

### Task 5: Verify and synchronize context

**Files:**
- Modify: `.codex/plans/admin-dashboard-todo.md`
- Modify: `.codex/skills/mascoteach-admin-dashboard.md`

- [x] Run:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --no-restore
dotnet build EXE101-Mascoteach-Backend.sln --no-restore
```

- [x] Search response DTOs and query projections for sensitive authentication, S3, PayOS, and webhook fields.
- [x] Mark roadmap items complete only after verification passes.
- [x] Record the stable API contract and known limitations in the Admin skill.
- [x] Do not commit or push without separate user authorization.
