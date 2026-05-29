---
description: |
  Use this skill when working on Mascoteach authentication, authorization, JWT claims, user identity, roles,
  ownership checks, CurrentUserId, BaseController, login/register, BCrypt password handling, or [Authorize] /
  [AllowAnonymous] endpoint access. Triggers: "auth", "permission", "role", "JWT", "login", "register",
  "CurrentUserId", "only owner", "teacher only", "student join".
---

# Mascoteach - Auth And Permission Skill

## Current auth shape

- Authentication is JWT Bearer, configured in `Mascoteach.API/Program.cs`.
- `AuthService` hashes passwords with `BCrypt.Net.BCrypt.HashPassword`.
- Login verifies passwords with `BCrypt.Net.BCrypt.Verify`.
- JWT claims include:
  - `"FullName"`
  - `ClaimTypes.Email`
  - `ClaimTypes.Role`
  - `"UserId"`
- `BaseController.CurrentUserId` reads the `"UserId"` claim.
- `BaseController.CurrentUserRole` reads `ClaimTypes.Role`.

## Endpoint access rules

- Use `[Authorize]` by default for controllers/endpoints.
- Use `[AllowAnonymous]` only for flows that must work without login:
  - `AuthController.Register`
  - `AuthController.Login`
  - student live-session lookup by PIN
  - student session-participant creation
- If an endpoint creates teacher-owned data, set owner fields from `CurrentUserId`, not request body.

## Ownership rules

For teacher-owned resources:

1. Get current teacher id from `CurrentUserId`.
2. Fetch the resource through service/repository.
3. Compare resource owner id to current teacher id.
4. Return `false`/`null` from the service when the resource is missing or not owned.
5. Controller should return `Forbid(...)` for ownership failures when existing local pattern does so.

Existing example: `DocumentService.UpdateDocumentAsync` checks `doc.TeacherId != teacherId`.

## Role checks

Role values are strings from the database/request, currently including `Teacher`, `Parent`, `Student`, and sometimes `Admin` in comments.

When adding role restrictions:

- Prefer explicit checks in service or controller using `CurrentUserRole`.
- Keep messages simple.
- Do not trust role or user id fields from request bodies.
- If using `[Authorize(Roles = "...")]`, verify the JWT uses `ClaimTypes.Role` as it currently does.

## Register/login rules

- Do not store raw passwords.
- Do not expose `PasswordHash` in DTOs.
- Check email uniqueness through `IUserRepository`.
- Soft-deleted users must not be able to login.
- Default subscription tier is currently `Freemium`.

## Validation checklist

- Protected endpoints have `[Authorize]`.
- Public endpoints have a deliberate `[AllowAnonymous]`.
- Current user id comes from JWT, not the client body.
- Owner-scoped operations cannot update/delete another teacher's resource.
- Password handling remains BCrypt-based.
- `dotnet build EXE101-Mascoteach-Backend.sln --no-restore` succeeds.
