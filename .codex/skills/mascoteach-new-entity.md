---
description: |
  Use this skill when adding a new entity, database table, domain object, feature, or CRUD module to the
  Mascoteach backend. Triggers: "add new entity", "create CRUD for X", "add [EntityName] feature",
  "tao entity moi", "them tinh nang", "scaffold [EntityName]", any task that requires a new database table
  or new domain object. Do NOT use for modifying existing entities, SignalR/GameHub work, or S3/document
  upload tasks.
---

# Mascoteach - New Entity Skill

## Project overview

Mascoteach is a 3-layer ASP.NET Core 9 solution:

- `Mascoteach.Data`: EF Core models, DbContext, repository interfaces and implementations.
- `Mascoteach.Service`: DTOs, service interfaces and implementations, AutoMapper profiles.
- `Mascoteach.API`: controllers, hubs, `Program.cs`.

Default dependency direction:

`Mascoteach.API -> Mascoteach.Service -> Mascoteach.Data`

## Conventions

- Models use `int Id` as primary key.
- Database columns use snake_case via `HasColumnName`.
- Soft-delete is standard: add `bool IsDeleted`.
- Never hard-delete unless explicitly requested.
- `GenericRepository<T>` filters `IsDeleted == false` in `GetAllAsync` and `GetByIdAsync`.
- Custom repository queries must filter `IsDeleted == false`.
- Controllers inherit `BaseController` when current user data is relevant.
- Use `[Authorize]` by default.
- Use `[AllowAnonymous]` only when explicitly needed.
- Keep controllers thin. Put business logic in service implementations.
- Use AutoMapper for entity-to-DTO and create-request-to-entity mapping.

## Step-by-step workflow

Follow these steps in order.

### 1. Model

Add model in `Mascoteach.Data/Models/EntityName.cs`:

```csharp
namespace Mascoteach.Data.Models;

public partial class EntityName
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
}
```

Register in `Mascoteach.Data/Models/MascoteachDbContext.cs`:

- Add `DbSet<EntityName> EntityNames`.
- Configure in `OnModelCreating`.
- Map table and columns with current EF Core style.
- Add relationships if needed.

### 2. Repository interface

Add `Mascoteach.Data/Interfaces/IEntityNameRepository.cs`:

```csharp
using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces;

public interface IEntityNameRepository : IGenericRepository<EntityName>
{
    Task<EntityName?> GetByIdIncludingDeletedAsync(int id);
}
```

Add custom query methods only when generic CRUD is not enough.

### 3. Repository implementation

Add `Mascoteach.Data/Repositories/EntityNameRepository.cs`:

```csharp
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories;

public class EntityNameRepository : GenericRepository<EntityName>, IEntityNameRepository
{
    public EntityNameRepository(MascoteachDbContext context) : base(context)
    {
    }

    public async Task<EntityName?> GetByIdIncludingDeletedAsync(int id)
    {
        return await _context.EntityNames.FindAsync(id);
    }
}
```

For custom filtered queries:

```csharp
return await _context.EntityNames
    .Where(e => e.ParentId == parentId && e.IsDeleted == false)
    .ToListAsync();
```

### 4. DTOs

Create DTOs in `Mascoteach.Service/DTOs`:

- `EntityNameCreateRequest.cs`
- `EntityNameUpdateRequest.cs`
- `EntityNameResponse.cs`

Use `[Required]` for mandatory request fields.

```csharp
using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class EntityNameCreateRequest
{
    [Required]
    public string Name { get; set; } = null!;
}
```

```csharp
namespace Mascoteach.Service.DTOs;

public class EntityNameResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsDeleted { get; set; }
}
```

### 5. AutoMapper profile

Add `Mascoteach.Service/Mappers/EntityNameProfile.cs`:

```csharp
using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class EntityNameProfile : Profile
{
    public EntityNameProfile()
    {
        CreateMap<EntityName, EntityNameResponse>();
        CreateMap<EntityNameCreateRequest, EntityName>();
    }
}
```

Add `.ForMember(...)` only when property names differ or fields are computed.

### 6. Service interface

Add `Mascoteach.Service/Interfaces/IEntityNameService.cs`:

```csharp
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface IEntityNameService
{
    Task<IEnumerable<EntityNameResponse>> GetAllAsync();
    Task<EntityNameResponse?> GetByIdAsync(int id);
    Task<EntityNameResponse> CreateAsync(EntityNameCreateRequest request);
    Task<bool> UpdateAsync(int id, EntityNameUpdateRequest request);
    Task<bool> DeleteAsync(int id);
    Task<EntityNameResponse?> ToggleDeleteAsync(int id);
}
```

### 7. Service implementation

Add `Mascoteach.Service/Implementations/EntityNameService.cs`.

Pattern:

- Query via repository.
- Map responses with AutoMapper.
- For updates, fetch entity first and return `false` when missing.
- For deletes, call repository `Delete` for soft-delete.
- For toggle-delete, use `GetByIdIncludingDeletedAsync`.
- Use transaction when one operation updates multiple aggregates.

### 8. Controller

Add `Mascoteach.API/Controllers/EntityNameController.cs`.

Pattern:

```csharp
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class EntityNameController : BaseController
{
    private readonly IEntityNameService _entityNameService;

    public EntityNameController(IEntityNameService entityNameService)
    {
        _entityNameService = entityNameService;
    }
}
```

Add CRUD actions following existing controllers:

- `GET /api/EntityName`
- `GET /api/EntityName/{id}`
- `POST /api/EntityName`
- `PUT /api/EntityName/{id}`
- `DELETE /api/EntityName/{id}`
- `PATCH /api/EntityName/{id}/toggle-delete` when needed.

### 9. Register DI

In `Mascoteach.API/Program.cs`, add:

```csharp
builder.Services.AddScoped<IEntityNameRepository, EntityNameRepository>();
builder.Services.AddScoped<IEntityNameService, EntityNameService>();
```

Keep repository registrations in the data layer section and service registrations in the business layer section.

### 10. Migration

If the schema changed, create a migration only when the user asks or the task clearly requires it.

Use the correct startup project if running EF commands.

## Validation checklist

- Model added.
- DbSet and `OnModelCreating` mapping added.
- Repository interface extends `IGenericRepository<T>`.
- Repository implementation extends `GenericRepository<T>`.
- Custom queries filter `IsDeleted == false`.
- DTOs created with required validation attributes.
- AutoMapper profile created.
- Service interface and implementation created.
- Controller inherits `BaseController` when current user data is relevant.
- `[Authorize]` is present unless endpoint is deliberately public.
- Repository and service are registered in `Program.cs`.
- `dotnet build EXE101-Mascoteach-Backend.sln --no-restore` succeeds.

## Common mistakes

- Do not put business logic in controllers.
- Do not manually map response DTOs in controllers.
- Do not hard-delete rows.
- Do not forget DI registration.
- Do not forget `GetByIdIncludingDeletedAsync` if adding toggle-delete.
- Do not touch CORS, JWT, SignalR, or S3 config unless the task asks for it.
