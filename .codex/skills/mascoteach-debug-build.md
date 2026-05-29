---
description: |
  Use this skill when diagnosing Mascoteach backend build errors, runtime errors, dependency issues, EF Core errors,
  DI registration failures, AutoMapper mapping failures, SQL Server connection issues, Swagger/API startup issues,
  or test/build verification. Triggers: "build failed", "error", "debug", "fix bug", "DI error", "AutoMapper",
  "DbContext", "migration", "SQL Server", "Swagger", "runtime exception".
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

### S3 failure

Check:

- `AWS:BucketName`
- `AWS:Region`
- credentials or instance role
- presigned URL expiration
- whether code is storing S3 key instead of presigned URL

## Migration notes

No migrations are currently present in the repo snapshot. If schema changes are required:

1. Confirm the user wants a migration.
2. Use EF Core migration commands against the correct startup project.
3. Do not hand-edit generated migration files unless necessary.

## Validation checklist

- Build succeeds.
- Warning-only output is called out separately from errors.
- No unrelated user changes are reverted.
- Fix follows the 3-layer dependency direction.
- The final answer includes the command run and any remaining warnings.
