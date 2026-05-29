---
description: |
  Use this skill when modifying an existing Mascoteach backend feature, adding endpoints to an existing entity,
  changing service/repository logic, adding filters, adding DTO fields, or extending CRUD behavior without creating
  a new database table. Triggers: "add endpoint", "modify existing feature", "update service", "add filter",
  "fix CRUD", "extend Document/Quiz/Question/Option/User/LiveSession/GameTemplate/SessionParticipant".
  Do NOT use for creating a brand-new entity/table; use mascoteach-new-entity.md for that.
---

# Mascoteach - Existing Feature Skill

## Current architecture

Mascoteach is a 3-layer ASP.NET Core 9 backend:

- `Mascoteach.API`: controllers, `Program.cs`, SignalR hubs.
- `Mascoteach.Service`: DTOs, service interfaces/implementations, AutoMapper profiles.
- `Mascoteach.Data`: EF Core models, `MascoteachDbContext`, repository interfaces/implementations.

Default flow:

`Controller -> Service Interface -> Service Implementation -> Repository Interface -> Repository Implementation -> MascoteachDbContext`

## Before editing

1. Read the existing controller, service interface, service implementation, repository interface, repository implementation, DTOs, and mapper for the target feature.
2. Preserve local naming and response patterns.
3. Keep controllers thin. Put business logic in service implementations.
4. Use repository methods for reusable queries instead of embedding EF queries in controllers or services unless the existing feature already does so.

## Change workflow

### Add a read endpoint

1. Add query method to repository interface if the query is not generic.
2. Implement query in repository with `IsDeleted == false` filters.
3. Add service method returning response DTOs.
4. Add controller action.
5. Register nothing in DI unless a new service/repository type was created.

### Add a create/update behavior

1. Add or update request DTOs in `Mascoteach.Service/DTOs`.
2. Add `[Required]` for mandatory fields.
3. Update AutoMapper profile only for create mapping or entity-to-response mapping.
4. Update service method. Validate ownership and parent existence when relevant.
5. Keep persistence grouped into one `SaveChangesAsync` where possible.

### Add a response field

1. Add property to the response DTO.
2. Update AutoMapper profile if names differ or value is computed.
3. If computed from another service (for example S3 presigned URL), set it in the service after mapping.
4. Check all service methods that return the DTO, not only `GetById`.

## Project conventions

- Use `int Id` for model identity.
- Use `bool IsDeleted` soft delete.
- `GenericRepository<T>.GetAllAsync` and `GetByIdAsync` hide deleted records.
- Custom repository queries must explicitly filter deleted records.
- Controllers that need current user data must inherit `BaseController`.
- Prefer `[Authorize]` by default. Use `[AllowAnonymous]` only for public flows such as login/register, student join, or PIN lookup.
- Use `CurrentUserId` for ownership checks when the logged-in teacher owns the resource.
- Use AutoMapper for entity-to-DTO and create-request-to-entity mapping.
- Manual field updates in service implementations are acceptable and common for update requests.

## Validation

Run:

```powershell
dotnet build EXE101-Mascoteach-Backend.sln --no-restore
```

If dependencies changed or restore is required, run normal `dotnet build`.

## Common mistakes to avoid

- Do not add business logic to controllers.
- Do not hard-delete rows unless explicitly requested.
- Do not forget `IsDeleted == false` in custom repository queries.
- Do not return resources owned by another teacher when the endpoint is scoped to the current user.
- Do not store S3 presigned URLs in the database; document storage uses S3 keys.
