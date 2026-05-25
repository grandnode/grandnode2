# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Restore dependencies
dotnet restore GrandNode.sln

# Build entire solution
dotnet build GrandNode.sln

# Run the web app (requires MongoDB running)
dotnet run --project src/Web/Grand.Web/Grand.Web.csproj

# Run all tests
dotnet test GrandNode.sln

# Run a specific test project
dotnet test src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj

# Run a single test class or method
dotnet test src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj --filter "ClassName=ProductServiceTests"
dotnet test src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj --filter "FullyQualifiedName~GetAllProductsDisplayedOnHomePageTest"

# Build plugins (required before running on Linux)
dotnet build src/Plugins/Payments.StripeCheckout
dotnet build src/Plugins/Shipping.FixedRateShipping
# (repeat for each plugin in src/Plugins/)

# Publish for production
dotnet publish src/Web/Grand.Web -c Release -o /var/webapps/grandnode
```

**Docker (quickest local setup):**
```bash
docker run -d -p 27017:27017 --name mongodb mongo
docker run -d -p 80:8080 --name grandnode2 --link mongodb:mongo grandnode/grandnode2
```

**Target framework:** `net10.0` (defined in `src/Build/Grand.Common.props`, imported by all projects).

## Architecture Overview

GrandNode2 is a multi-layer e-commerce platform using ASP.NET Core, MongoDB, and MediatR (CQRS). The solution is organized into five top-level groups:

### Layer Dependency Order (bottom → top)

```
Grand.SharedKernel          ← shared primitives, no dependencies
Grand.Domain                ← entities only
Grand.Data                  ← IRepository<T> abstraction + MongoDB/LiteDB implementations
Grand.Business.Core         ← interfaces, CQRS contracts (queries/commands), events
Grand.Business.*            ← service implementations (Catalog, Checkout, Customers, etc.)
Grand.Infrastructure        ← cross-cutting: DI bootstrap, caching, plugins, events, AutoMapper
Grand.Web.Common / Admin / Vendor / Store / Web  ← presentation layers
```

### Domain (`src/Core/Grand.Domain/`)

All entities inherit from `BaseEntity` → `ParentEntity`. `ParentEntity` auto-generates a string `Id` (MongoDB ObjectId-style). `BaseEntity` adds `CreatedOnUtc`, `UpdatedOnUtc`, `CreatedBy`, `UpdatedBy`, and a `UserFields` bag for extensible custom attributes without schema changes.

### Data Access (`src/Core/Grand.Data/`)

`IRepository<T>` is the sole data-access abstraction. It provides:
- LINQ `Table` queryable
- Standard CRUD (`InsertAsync`, `UpdateAsync`, `DeleteAsync`)
- Subdocument operations (`AddToSet`, `UpdateToSet`, `PullFilter`) — critical for MongoDB's embedded document model
- Bulk field updates (`UpdateField`, `UpdateManyAsync`)

Two implementations exist: `Mongo/` (production) and `LiteDb/` (lightweight/testing). `DataSettingsManager` loads the connection string and selects the provider at startup.

### Business Logic (`src/Business/`)

Each bounded context (`Catalog`, `Checkout`, `Customers`, `Marketing`, `Cms`, `Messages`, `Storage`, `Authentication`, `Common`) is split:
- **`Grand.Business.Core`** — interfaces (in `Interfaces/`) and CQRS contracts (`Queries/`, `Commands/`). No implementation here.
- **`Grand.Business.<Name>`** — concrete service implementations. Services are injected via the interfaces defined in Core.

### CQRS & Events (`MediatR`)

Commands and queries are MediatR `IRequest<T>` records defined in `Grand.Business.Core`. Handlers live alongside the feature they serve:
- Web feature handlers: `src/Web/Grand.Web/Features/Handlers/`
- Business handlers: inside each `Grand.Business.*` project

Domain events use `EntityInserted<T>`, `EntityUpdated<T>`, `EntityDeleted<T>` (MediatR `INotification`). The repository implementations publish these automatically. Subscribe by implementing `INotificationHandler<EntityInserted<T>>`.

### Infrastructure (`src/Core/Grand.Infrastructure/`)

- **Startup discovery**: Any class implementing `IStartupApplication` is auto-discovered via reflection and called during startup, ordered by `Priority`. Use this to register services or middleware from any layer without touching `Program.cs`.
- **Plugin system**: Plugins implement `IPlugin` and are decorated with `[PluginInfoAttribute]`. `PluginManager` loads them at startup. Only installed plugins participate in DI and AutoMapper.
- **Module system**: `src/Modules/` contains first-party modules (Api, Installer, Migration, ScheduledTasks) loaded via `ModuleLoader`. Modules differ from plugins in that they are always loaded.
- **Caching**: `ICacheBase` (memory) and Redis variant. Cache invalidation is event-driven — handlers for entity events call `RemoveByPrefix` on related cache keys.
- **AutoMapper**: Profiles implement `IAutoMapperProfile` and are discovered via type scanning. Only profiles from installed plugins are registered.

### Web Layers (`src/Web/`)

- `Grand.Web` — customer-facing storefront (MVC + Vue.js frontend in `vueapp/`)
- `Grand.Web.Admin` — admin panel
- `Grand.Web.Vendor` — vendor portal
- `Grand.Web.Store` — store-specific entry point
- `Grand.Web.Common` — shared web utilities (filters, model binders, validators)
- `Grand.Web.AdminShared` / `Grand.SharedUIResources` — shared Razor components and static assets

Controllers are thin: they delegate to MediatR queries/commands or service interfaces. Complex view-model assembly is done in Feature Handlers.

### REST API (`src/Modules/Grand.Module.Api/`)

JWT-secured API module. Controllers follow the same CQRS pattern. `BackendAPIConfig` and `FrontendAPIConfig` sections in `appsettings.json` control authentication behavior.

### Plugins (`src/Plugins/`)

Installable extensions for payments, shipping, tax, authentication, widgets, and themes. Each plugin is an independent project that references `Grand.Business.Core` and `Grand.Infrastructure`. To add a new plugin: implement `IPlugin`, add `[PluginInfoAttribute]`, and implement `IStartupApplication` for service registration.

### Testing (`src/Tests/`)

- **Framework**: MSTest + Moq
- **Repository mocking**: `MongoDBRepositoryTest<T>` (from `Grand.Data.Tests`) is an in-memory repository used instead of a real database. Do not mock `IRepository<T>` directly — use this class.
- **Cache mocking**: `MemoryCacheTest.Get()` from `Grand.Infrastructure.Tests`.
- **Pattern**: `[TestInitialize]` sets up real repository instances and Moq mocks for external dependencies (IMediator, IContextAccessor). Tests insert data directly via the repository and assert service outputs.
