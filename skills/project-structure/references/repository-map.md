# GrandNode Repository Map

## Root
- `GrandNode.sln`: main solution.
- `global.json`: pinned .NET SDK behavior.
- `Directory.Packages.props`: central NuGet package versions.
- `src/Build`: common MSBuild props and targets.
- `src/Tests`: test projects.
- `src/Core`: domain, data, infrastructure, mapping, shared kernel.
- `src/Business`: business interfaces and implementations.
- `src/Web`: public site, admin panels, shared web libraries, frontend app.
- `src/Plugins`: installable provider plugins and themes.
- `src/Modules`: application modules.
- `src/Aspire`: local orchestration and service defaults.

## Technologies In Use
- .NET and ASP.NET Core.
- MongoDB through repository/data abstractions.
- Redis for caching and data protection integration.
- MediatR for commands, queries, and events.
- FluentValidation for validators.
- Custom mapping framework in `Grand.Mapping` with an AutoMapper-compatible profile API (`Profile`, `CreateMap`, `ForMember`). AutoMapper package is not used.
- Razor views, partials, view components, and tag helpers.
- Vue 3 and Vite for storefront frontend assets.
- Bootstrap and Bootstrap Icons.
- DotLiquid for message templates.
- OpenAPI and Scalar in the API module.
- Aspire and OpenTelemetry for local orchestration and observability.
- Docker and Azure Pipelines for build and deployment workflows.

## Core Projects

### `src/Core/Grand.Domain`
Use for domain entities, settings objects, enums, and domain-level contracts.

Examples:
- `Catalog`
- `Orders`
- `Customers`
- `Vendors`
- `Stores`
- `Payments`
- `Shipping`
- `Tax`
- `Permissions`
- `Messages`

Do not put UI models, persistence details, or controller behavior here.

### `src/Core/Grand.Data`
Use for repository abstractions and MongoDB/LiteDB data infrastructure.

Use this area for:
- repository contracts and implementations
- MongoDB context behavior
- data provider configuration
- audit provider integration

Do not put business workflow rules or UI preparation here.

### `src/Core/Grand.Infrastructure`
Use for cross-cutting runtime infrastructure.

Use this area for:
- startup abstractions
- caching
- plugin infrastructure
- module infrastructure
- endpoint provider infrastructure
- type scanning
- migrations infrastructure
- model binding
- validators infrastructure

### `src/Core/Grand.Mapping`
Use for shared mapping infrastructure and mapping support.

### `src/Core/Grand.SharedKernel`
Use for low-level shared primitives, attributes, extensions, and small cross-cutting helpers.

## Business Projects

### `src/Business/Grand.Business.Core`
Use for business interfaces, commands, queries, utilities, and cross-business contracts.

Put interfaces here when implementations live in a feature-specific business project.

### Feature Business Projects
Use these for implementations:
- `Grand.Business.Authentication`
- `Grand.Business.Catalog`
- `Grand.Business.Checkout`
- `Grand.Business.Cms`
- `Grand.Business.Common`
- `Grand.Business.Customers`
- `Grand.Business.Marketing`
- `Grand.Business.Messages`
- `Grand.Business.Storage`

Use the closest feature project. Register implementations in that project's `Startup/StartupApplication.cs`.

## Web Projects

### `src/Web/Grand.Web`
Use for public storefront controllers, views, view models, commands, handlers, validators, components, and static web assets.

Important folders:
- `Controllers`
- `Views`
- `Views/Shared`
- `Views/PdfTemplates`
- `Components`
- `Commands`
- `Features`
- `Models`
- `Validators`
- `wwwroot`
- `vueapp`

### `src/Web/Grand.Web.Admin`
Use for main Admin panel controllers and views.

Controllers are in:
- `src/Web/Grand.Web.Admin/Controllers`

Views are in:
- `src/Web/Grand.Web.Admin/Areas/Admin/Views`

### `src/Web/Grand.Web.Store`
Use for Store Owner panel controllers and views.

Controllers are in:
- `src/Web/Grand.Web.Store/Controllers`

Views are in:
- `src/Web/Grand.Web.Store/Areas/Store/Views`

### `src/Web/Grand.Web.Vendor`
Use for Vendor panel controllers, views, models, services, components, and vendor-specific access behavior.

Controllers are in:
- `src/Web/Grand.Web.Vendor/Controllers`

Views are in:
- `src/Web/Grand.Web.Vendor/Areas/Vendor/Views`

### `src/Web/Grand.Web.AdminShared`
Use for shared admin models, validators, mapper profiles, extensions, and contracts used by Admin, Store Owner, and sometimes Vendor panels.

Check this project before adding duplicate admin models.

### `src/Web/Grand.Web.Common`
Use for shared web infrastructure, base controllers, base components, themes, tag helpers, and common MVC helpers.

### `src/Web/Grand.SharedUIResources`
Use for shared static UI resources.

## Plugins
Plugins live under `src/Plugins`.

Existing plugin categories:
- `Authentication.*`
- `DiscountRules.*`
- `ExchangeRate.*`
- `Payments.*`
- `Shipping.*`
- `Tax.*`
- `Theme.*`
- `Widgets.*`

Use plugins for installable providers, integrations, widgets, payment methods, shipping methods, tax providers, authentication providers, discount rules, exchange rates, and themes.

Plugin build output goes to:
- `src/Web/Grand.Web/Plugins/{SystemName}`

Use the `plugin-module` skill for plugin work.

## Modules
Modules live under `src/Modules`.

Existing modules:
- `Grand.Module.Api`
- `Grand.Module.Installer`
- `Grand.Module.Migration`
- `Grand.Module.ScheduledTasks`

Use modules for application-level features that are not installable provider plugins.

Module build output goes to:
- `src/Web/Grand.Web/Modules/{ModuleName}`

Use the `plugin-module` skill for module structure.

## API Module
API functionality lives in:
- `src/Modules/Grand.Module.Api`

Use for:
- REST controllers
- DTOs
- validators
- command and query handlers
- JWT
- OpenAPI metadata
- API mapping profiles

## Installer And Migration
Installer behavior lives in:
- `src/Modules/Grand.Module.Installer`

Migration behavior lives in:
- `src/Modules/Grand.Module.Migration`

Use installer for initial seed data. Use migration for versioned updates to existing installations.

Common seeded areas:
- permissions
- admin sitemap
- settings
- message templates
- stores
- schedule tasks
- localization resources

## Scheduled Tasks
Scheduled task module:
- `src/Modules/Grand.Module.ScheduledTasks`

Register tasks in:
- `Startup/StartupApplication.cs`

Use keyed scoped registration:
- `AddKeyedScoped<IScheduleTask, TaskType>("Task name")`

## Frontend Assets
Storefront frontend source:
- `src/Web/Grand.Web/vueapp`

Committed frontend output:
- `src/Web/Grand.Web/wwwroot/bundles`

Theme CSS source:
- `src/Web/Grand.Web/wwwroot/theme/css`

Run frontend build when changing Vue source or theme CSS that affects committed bundles.

## Tests
Tests live under:
- `src/Tests`

Choose the closest test project by target ownership:
- Business changes: `Grand.Business.*.Tests`
- Data changes: `Grand.Data.Tests`
- Domain changes: `Grand.Domain.Tests`
- Infrastructure changes: `Grand.Infrastructure.Tests`
- Mapping changes: `Grand.Mapping.Tests`
- Module changes: `Grand.Modules.Tests` or module-specific tests
- API changes: `Grand.Module.Api.Tests`
- Web common changes: `Grand.Web.Common.Tests`
- Admin changes: `Grand.Web.Admin.Tests`
- Storefront changes: `Grand.Web.Tests`
- Store Owner changes: `Grand.Web.Store.Tests`
- Shared kernel changes: `Grand.SharedKernel.Tests`

## Expansion Rules

### Add Domain Data
1. Add or update entity or settings class in `Grand.Domain`.
2. Update data access behavior only if repository or MongoDB configuration requires it.
3. Update business services and interfaces.
4. Update UI/API models, mappers, validators, and tests.

### Add Business Logic
1. Add interface to `Grand.Business.Core` when used across projects.
2. Add implementation to the closest `Grand.Business.*` project.
3. Register implementation in `StartupApplication`.
4. Add or update tests in the matching business test project.

### Add Public Storefront UI
1. Update `Grand.Web` controller, command, query, handler, model, validator, component, or view.
2. Update localization resources.
3. Update frontend bundles when Vue or theme CSS source changes.
4. Add web tests when behavior changes.

### Add Admin UI
1. Use `admin-area-changes`.
2. Update `Grand.Web.AdminShared` if shared models, validators, or mapper profiles are involved.
3. Update Admin, Store Owner, and Vendor panels only where the role should own the workflow.

### Add Plugin Or Module
1. Use `plugin-module`.
2. Start from the closest existing plugin or module.
3. Keep output paths aligned with `Grand.Web/Plugins` or `Grand.Web/Modules`.

### Add Message Template
1. Use `template-creation`.
2. Update `MessageTemplateNames` when code references the template.
3. Update installer seed data for new installations.
4. Add DotLiquid tokens only through `Liquid*` drops and token provider patterns.

### Add Permission Or Menu Item
1. Update permission constants and seed data using existing installer utilities.
2. Update `StandardAdminSiteMap` when navigation is required.
3. Enforce permission server-side in controllers or services.
4. Check Admin, Store Owner, and Vendor differences.

