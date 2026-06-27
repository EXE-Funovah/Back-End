# Admin Overview V2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the inaccurate legacy Admin overview metrics with a validated, read-only snapshot built only from data currently stored in Mascoteach.

**Architecture:** Keep `AdminController -> IAdminService -> AdminService -> IAdminRepository -> AdminRepository`. Add one Data-layer overview projection, let the repository own soft-delete-aware aggregate queries, and let the service validate range values and shape the public response.

**Tech Stack:** ASP.NET Core 9, EF Core 9 SQL Server, xUnit, Moq.

---

### Task 1: Write failing Overview V2 contract tests

**Files:**
- Create: `Mascoteach.Tests/AdminOverviewServiceTests.cs`
- Modify: `Mascoteach.Tests/AdminControllerTests.cs`

- [x] Test that unknown ranges fail before repository access.
- [x] Test that `7d`, `30d`, and `12m` produce the correct query window.
- [x] Test KPI, role, subscription, content, participant, payment-status, and paid-revenue mapping.
- [x] Test that labels do not claim AI generation or a specific game template.
- [x] Test that the Overview controller returns HTTP 400 for an invalid range.
- [x] Run focused tests and confirm RED because the V2 projection/contract does not exist.

### Task 2: Add soft-delete-aware overview aggregation

**Files:**
- Create: `Mascoteach.Data/Projections/AdminOverviewProjection.cs`
- Modify: `Mascoteach.Data/Interfaces/IAdminRepository.cs`
- Modify: `Mascoteach.Data/Repositories/AdminRepository.cs`

- [x] Aggregate non-deleted users by role and canonical subscription status.
- [x] Count new users and active users in the selected range.
- [x] Count active Documents, Quiz activities, Flashcard activities, LiveSessions, and participant join rows.
- [x] Count non-deleted PaymentOrders by current status.
- [x] Sum non-deleted Paid orders whose `PaidAt` falls inside the selected range.
- [x] Exclude soft-deleted parent users/documents/sessions where applicable.
- [x] Align active Premium with `SubscriptionTier == "Premium"` plus future expiry.
- [x] Filter soft-deleted rows from the existing paid-revenue series and plan attribution queries.

### Task 3: Replace the Overview response contract and service logic

**Files:**
- Modify: `Mascoteach.Service/DTOs/Admin/AdminDtos.cs`
- Modify: `Mascoteach.Service/Implementations/AdminService.cs`

- [x] Add `range`, `from`, and `to` to the response.
- [x] Return KPI cards for total users, new users, active users, and paid revenue in range.
- [x] Return role, subscription, content, and payment-status distributions.
- [x] Rename the Overview series to `PaidRevenueSeries`; leave the separate Revenue endpoint contract unchanged.
- [x] Preserve the 12-month actual-paid-revenue series and label it accurately.

### Task 4: Return proper HTTP errors

**Files:**
- Modify: `Mascoteach.API/Controllers/AdminController.cs`

- [x] Catch invalid Overview range values and return HTTP 400.
- [x] Keep controller-level `[Authorize(Roles = "Admin")]`.
- [x] Run focused tests and confirm GREEN.

### Task 5: Verify and synchronize context

**Files:**
- Modify: `.codex/plans/admin-dashboard-todo.md`
- Modify: `.codex/skills/mascoteach-admin-dashboard.md`

- [x] Run the full test suite and solution build.
- [x] Run `git diff --check`.
- [x] Mark Overview complete only after fresh verification passes.
- [x] Document exact metric semantics and telemetry exclusions.
- [x] Do not commit or push without separate user authorization.
