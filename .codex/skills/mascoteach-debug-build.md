---
description: |
  Use this skill when diagnosing Mascoteach backend build errors, runtime errors, dependency issues, EF Core errors,
  DI registration failures, AutoMapper mapping failures, SQL Server connection issues, Swagger/API startup issues,
  or test/build verification. Triggers: "build failed", "error", "debug", "fix bug", "DI error", "AutoMapper",
  "DbContext", "migration", "scaffold", "SQL Server", "Swagger", "runtime exception", "Docker deploy",
  "GitHub Actions", "Gmail SMTP", "Google ClientId".
---

# Mascoteach - Debug And Build Skill

## First pass

1. Run `git status --short` to see local changes.
2. Read the exact error message.
3. Locate the owning layer:
   - API/controller/Program/Hub
   - Service/DTO/Mapper/business logic
   - Data/model/DbContext/repository
4. Fix the smallest relevant surface.

## Build commands

Preferred quick build:

```powershell
dotnet build EXE101-Mascoteach-Backend.sln --no-restore
```

Use normal build when packages or project files changed:

```powershell
dotnet build EXE101-Mascoteach-Backend.sln
```

## Common failure patterns

### DI resolution failure

Check `Mascoteach.API/Program.cs`:

- Repository interface registered to repository implementation.
- Service interface registered to service implementation.
- Constructor parameters match registered interfaces.

### AutoMapper failure

Check `Mascoteach.Service/Mappers`:

- Entity-to-response map exists.
- Create request-to-entity map exists when service uses `_mapper.Map<Entity>(request)`.
- Computed fields are ignored in mapper and filled in service.
- Nested collections may need manual loading from repositories.

### EF Core query or model failure

Check:

- `Mascoteach.Data/Models/MascoteachDbContext.cs`
- Entity property names and column names.
- Relationship foreign keys.
- Soft-delete filters in custom repository queries.

### Missing namespace or type

Check project references:

- API references Data and Service.
- Service references Data.
- Data should not reference Service or API.

### Auth failure

Check:

- JWT settings in configuration.
- Token includes `"UserId"` claim.
- Endpoint has expected `[Authorize]` or `[AllowAnonymous]`.
- `CurrentUserId` is not `0` unexpectedly.
- Google login has `Google:ClientId` configured.
- Forgot password has `Email:*`, `Frontend:ResetPasswordUrl`, and `Auth:PasswordResetTokenMinutes` configured.
- Google-only users (`Authenticator = "Google"` and `PasswordHash = null`) should not pass email/password login.
- Reset password uses `ResetTokenHash` and `ResetTokenExpiresAt`; the raw token should only exist in the email link.

### Gmail SMTP failure

Check:

- `Email:SmtpHost` is usually `smtp.gmail.com`.
- `Email:SmtpPort` is usually `587`.
- `Email:Password` must be a Gmail App Password, not the normal Gmail password.
- Gmail must have 2-Step Verification enabled to create an App Password.
- Spam folder may receive local/dev emails from a new Gmail sender.

### Google login failure

Check:

- Frontend sends Google `credential` / ID token to `/api/Auth/google-login`.
- Backend `Google:ClientId` matches the frontend Google Client ID.
- Google Cloud Console has the frontend origin, such as `http://localhost:5173`, in authorized JavaScript origins.
- Swagger cannot generate Google credentials; use frontend to obtain `response.credential`, then paste it if testing manually.

### S3 failure

Check:

- `AWS:BucketName`
- `AWS:Region`
- credentials or instance role
- presigned URL expiration
- whether code is storing S3 key instead of presigned URL

## DB-first schema notes

This project is currently DB-first. No EF migrations are present in the repo snapshot. If schema changes are required:

1. Confirm the SQL change and update the dev database first.
2. Keep an idempotent SQL script for production rollout.
3. Scaffold from the updated dev database.
4. Review scaffold diffs carefully; `--force` can overwrite model/context files.
5. Do not use EF Core migrations unless the team explicitly chooses to move away from DB-first.

Current auth-related `Users` columns:

- `password_hash` nullable
- `authenticator`
- `google_subject`
- `reset_token_hash`
- `reset_token_expires_at`

Current document addition:

- `Documents.file_name`

Common scaffold command:

```powershell
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer --project Mascoteach.Data --startup-project Mascoteach.API --context MascoteachDbContext --context-dir Models --output-dir Models --force --no-onconfiguring
```

### Docker/GitHub Actions deploy config

When changing deployment, GitHub Actions, Docker runtime config, appsettings keys, or GitHub Secrets, also read `.codex/skills/mascoteach-deployment.md`.

The Dockerfile does not need changes for normal package/config additions. GitHub Actions passes runtime config through environment variables.

For develop deployments, ensure these GitHub Secrets exist when auth email/Google flows are enabled:

- `DEV_GOOGLE_CLIENT_ID`
- `DEV_FRONTEND_RESET_PASSWORD_URL`
- `DEV_FRONTEND_VERIFY_EMAIL_URL`
- `DEV_AUTH_PASSWORD_RESET_TOKEN_MINUTES`
- `DEV_AUTH_EMAIL_VERIFICATION_TOKEN_HOURS`
- `DEV_EMAIL_SMTP_HOST`
- `DEV_EMAIL_SMTP_PORT`
- `DEV_EMAIL_USERNAME`
- `DEV_EMAIL_PASSWORD`
- `DEV_EMAIL_FROM_EMAIL`
- `DEV_EMAIL_FROM_NAME`

For production/main, use the same names without `DEV_`.

## Validation checklist

- Build succeeds.
- Warning-only output is called out separately from errors.
- No unrelated user changes are reverted.
- Fix follows the 3-layer dependency direction.
- DB-first scaffold changes are reviewed before editing around them.
- Deploy secrets/environment variables are updated when new config keys are introduced.
- The final answer includes the command run and any remaining warnings.
