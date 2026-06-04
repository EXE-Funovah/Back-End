---
description: |
  Use this skill when working on Mascoteach authentication, authorization, JWT claims, user identity, roles,
  ownership checks, CurrentUserId, BaseController, login/register, BCrypt password handling, or [Authorize] /
  [AllowAnonymous] endpoint access. Triggers: "auth", "permission", "role", "JWT", "login", "register",
  "Google login", "forgot password", "reset password", "Gmail SMTP", "CurrentUserId", "only owner",
  "teacher only", "student join".
---

# Mascoteach - Auth And Permission Skill

## Current auth shape

- Authentication is JWT Bearer, configured in `Mascoteach.API/Program.cs`.
- `AuthService` hashes passwords with `BCrypt.Net.BCrypt.HashPassword`.
- Login verifies passwords with `BCrypt.Net.BCrypt.Verify`.
- Google login verifies Google ID tokens through `IGoogleTokenValidator`.
- Forgot/reset password sends reset links through `IEmailService`.
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
  - `AuthController.GoogleLogin`
  - `AuthController.ForgotPassword`
  - `AuthController.ResetPassword`
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
- Local users have `Authenticator = "Local"` and a BCrypt `PasswordHash`.
- Google-only users have `Authenticator = "Google"`, `PasswordHash = null`, and `GoogleSubject` set from Google `sub`.
- If a Google-only user attempts email/password login, return a clear message telling them to sign in with Google.
- Google-created users default to role `Teacher`.

## Google login rules

- Frontend sends Google `credential` / ID token to `POST /api/Auth/google-login`.
- Backend must verify the token server-side with configured `Google:ClientId`.
- Use Google `sub` as `User.GoogleSubject`; do not trust email/name from the request body.
- Match existing users by `GoogleSubject` first, then email for linking.
- If an existing local user logs in with Google, set `GoogleSubject` and use `Authenticator = "Both"`.
- Return normal Mascoteach `AuthResponse` with the internal JWT after successful Google verification.

## Forgot/reset password rules

- `POST /api/Auth/forgot-password` returns the same generic success message whether the email exists or not.
- Do not return reset tokens in API responses.
- Store only `ResetTokenHash`, never the raw reset token.
- `ResetTokenExpiresAt` controls expiry; default config is `Auth:PasswordResetTokenMinutes`.
- Send reset links using `Frontend:ResetPasswordUrl` plus `?token=...`.
- Password reset emails should be Vietnamese HTML emails with a button/anchor, plus text fallback.
- Do not display the raw reset token as visible body text; keep it inside the reset link `href`.
- Skip reset email for Google-only accounts.
- `POST /api/Auth/reset-password` must verify token hash, expiry, and password confirmation.
- The new password must be different from the current password when a current BCrypt hash exists.
- On successful reset, BCrypt hash the new password and clear `ResetTokenHash` and `ResetTokenExpiresAt`.
- If a Google account later sets/resets a password, use `Authenticator = "Both"`.

## Auth configuration

- Local config lives in `appsettings.Development.json`; do not commit secrets.
- Deploy config should come from environment variables / GitHub Secrets:
  - `Google__ClientId`
  - `Frontend__ResetPasswordUrl`
  - `Auth__PasswordResetTokenMinutes`
  - `Email__SmtpHost`
  - `Email__SmtpPort`
  - `Email__Username`
  - `Email__Password`
  - `Email__FromEmail`
  - `Email__FromName`

## Validation checklist

- Protected endpoints have `[Authorize]`.
- Public endpoints have a deliberate `[AllowAnonymous]`.
- Current user id comes from JWT, not the client body.
- Owner-scoped operations cannot update/delete another teacher's resource.
- Password handling remains BCrypt-based.
- Google tokens are verified with Google before issuing Mascoteach JWTs.
- Reset tokens are hashed, expiring, and cleared after use.
- Reset password rejects reuse of the current password.
- Gmail/SMTP and Google settings are supplied by config, not hardcoded secrets.
- `dotnet build EXE101-Mascoteach-Backend.sln --no-restore` succeeds.
