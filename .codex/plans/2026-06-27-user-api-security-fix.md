# User API Security Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent authenticated users from enumerating other accounts or changing their own role and subscription tier.

**Architecture:** Keep `/api/User/me` and self-service profile updates available to authenticated users, while requiring the `Admin` role for collection and arbitrary-id reads. Enforce the privileged-field boundary in `UserUpdateRequest` and `UserService`, not only in the controller, so later callers cannot reintroduce mass assignment.

**Tech Stack:** ASP.NET Core 9, C#, xUnit, Moq.

---

### Task 1: Encode the vulnerable behavior with failing tests

**Files:**
- Create: `Mascoteach.Tests/UserControllerSecurityTests.cs`
- Modify: `Mascoteach.Tests/UserServiceTests.cs`

- [x] Add reflection tests requiring `GetAll` and `GetById` to declare `[Authorize(Roles = "Admin")]`.
- [x] Add a service regression test proving profile updates preserve the stored `Role`.
- [x] Add a service regression test proving profile updates preserve the stored `SubscriptionTier`.
- [x] Add positive coverage proving `FullName` and `Email` still update.
- [x] Run:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --filter "UserControllerSecurityTests|UserServiceTests" --no-restore
```

Expected result: the new security tests fail against the vulnerable implementation.

### Task 2: Apply the minimal authorization and mass-assignment fixes

**Files:**
- Modify: `Mascoteach.API/Controllers/UserController.cs`
- Modify: `Mascoteach.Service/DTOs/UserUpdateRequest.cs`
- Modify: `Mascoteach.Service/Implementations/UserService.cs`

- [x] Add `[Authorize(Roles = "Admin")]` to `GetAll` and `GetById`; keep `GetCurrentUser` under ordinary authentication.
- [x] Remove `Role` and `SubscriptionTier` from `UserUpdateRequest`.
- [x] Remove privileged-field assignments from `UserService.UpdateAsync`.
- [x] Run the focused tests and confirm they pass.

### Task 3: Verify the original paths and normal behavior

**Files:**
- Inspect: `Mascoteach.API/Controllers/UserController.cs`
- Inspect: `Mascoteach.Service/Implementations/UserService.cs`

- [x] Search for any remaining caller-controlled assignment to `User.Role` or `User.SubscriptionTier` outside the trusted auth/billing flows.
- [x] Run:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --no-restore
dotnet build EXE101-Mascoteach-Backend.sln --no-restore
```

- [x] Confirm the two original vulnerable paths no longer reproduce.

### Task 4: Synchronize durable context and roadmap

**Files:**
- Modify: `.codex/plans/admin-dashboard-todo.md`
- Modify: `.codex/skills/mascoteach-admin-dashboard.md`
- Modify: `.codex/skills/mascoteach-auth-permission.md`

- [x] Mark both security blockers complete only after focused tests, full tests, and build pass.
- [x] Record the verified User API contract in the Auth/Admin rules.
- [x] Do not commit or push; Git operations require separate user authorization.
