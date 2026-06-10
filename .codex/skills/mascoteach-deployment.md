---
description: |
  Use this skill when changing Mascoteach deployment, GitHub Actions, Docker runtime configuration,
  environment variables, appsettings keys, GitHub Secrets, branch deploy behavior, DB rollout scripts,
  or CI/CD failures. Triggers: "deploy", "deployment", "CI/CD", "GitHub Actions", "workflow",
  "secrets", "environment variables", "Docker", "develop deploy", "production deploy".
---

# Mascoteach - Deployment Skill

## Core rule

Any new runtime configuration key requires all three surfaces to be checked:

1. `appsettings.json` / `appsettings.Development.json`
2. `.github/workflows/auto-build-deploy-dotnet.yml`
3. GitHub Secrets for the target branch/environment

Do not assume adding `appsettings` is enough. Docker deploy reads values from environment variables passed in the workflow.

## Branch rules

- `develop` deploy uses `DEV_*` GitHub Secrets.
- `main` / production deploy uses the same secret names without `DEV_`.
- Pull requests must validate only; they must never stop/remove/run deployment containers.
- Deploy containers only on `push` events after code is merged into `develop` or `main`.
- Runtime .NET environment variable names use double underscores, for example:
  - `Frontend__ResetPasswordUrl`
  - `Frontend__VerifyEmailUrl`
  - `Auth__PasswordResetTokenMinutes`
  - `Auth__EmailVerificationTokenHours`

## Pull request safety rule

Never use one deploy job for both `push` and `pull_request`.

Correct shape:

```yaml
jobs:
  validate-pr:
    if: github.event_name == 'pull_request'
    # build/test only, no docker stop/rm/run

  build-and-deploy:
    if: github.event_name == 'push'
    # docker deploy is allowed here
```

Reason:

- On `push` to `develop`, `github.ref_name == 'develop'`.
- On `pull_request` targeting `develop`, `github.ref_name` is not `develop`; it is a PR ref such as a merge ref.
- If a workflow checks `github.ref_name == 'develop'` inside a PR deploy job, the condition can be false and fall back to production secrets.
- A PR like `validateEmail -> develop` must not be able to deploy production.
- A PR like `develop -> main` must not deploy production until it is merged and creates a `push` to `main`.

If branch selection is needed for PR-only validation, use `github.base_ref` for the target branch and `github.head_ref` for the source branch. Do not use those values to deploy.

## Deployment checklist for new config

When adding a config key such as `Section:Key`:

1. Add a safe placeholder/default to `Mascoteach.API/appsettings.json`.
2. Add the development value to `Mascoteach.API/appsettings.Development.json` only if the file is local/gitignored or safe.
3. Add a job-level selector in `.github/workflows/auto-build-deploy-dotnet.yml`:
   - `KEY_NAME: ${{ github.ref_name == 'develop' && secrets.DEV_KEY_NAME || secrets.KEY_NAME }}`
4. Pass it into `docker run` using .NET key syntax:
   - `-e Section__Key="$KEY_NAME"`
5. Tell the user exactly which GitHub Secrets to create for `develop`.
6. If production will need the same feature, also list the future non-`DEV_` secret names.
7. Run `dotnet build EXE101-Mascoteach-Backend.sln --no-restore`.

## DB rollout checklist

Mascoteach is DB-first. For schema changes:

- Write idempotent SQL scripts that can run safely on dev and production.
- Do not hardcode `USE MascoteachDB_Dev` in production-ready scripts.
- Avoid rerun side effects. If updating existing rows during first rollout, guard the update so reruns do not mutate future data incorrectly.
- Run the script on dev before scaffold.
- Scaffold from dev after the schema is updated.
- Review `Mascoteach.Data/Models/User.cs` and `Mascoteach.Data/Models/MascoteachDbContext.cs` or any affected model/context files before editing business logic.
- Production deployment requires the SQL rollout to happen before the new backend container starts using the new columns.

## Current auth/email deployment keys

Develop secrets:

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

Production secrets use the same names without `DEV_`.

## Common mistakes

- Adding a key to `appsettings.Development.json` but forgetting `docker run -e`.
- Creating GitHub Secrets but forgetting the workflow job-level selector.
- Using `Section:Key` as an environment variable name instead of `Section__Key`.
- Letting `pull_request` run `docker stop`, `docker rm`, or `docker run`.
- Using `github.ref_name == 'develop'` in a PR deploy job and accidentally falling back to production secrets.
- Adding dev secrets only, then later merging to `main` without production secret names.
- Scaffold before running the DB script.
- Starting the deployed app before production DB has the new columns.
