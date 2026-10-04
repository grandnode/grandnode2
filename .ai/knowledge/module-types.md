# GrandNode Module Types

## Existing Module Inventory
- `Grand.Module.Api`
- `Grand.Module.Installer`
- `Grand.Module.Migration`
- `Grand.Module.ScheduledTasks`

## Common Module Structure
Use modules for application-level features that are not installable provider plugins.

Use this structure unless the closest existing module uses a narrower shape:
- `{ModuleName}.csproj`
- `Startup/StartupApplication.cs` or `Infrastructure/*Startup.cs`
- `Controllers`, `Commands`, `Queries`, `Handlers`, `DTOs`, `Validators`, or services as needed
- module-specific configuration, mapping, endpoints, and tests

## Project Rules
Set Debug and Release output paths to:

```xml
<OutputPath>..\..\Web\Grand.Web\Modules\{ModuleName}\</OutputPath>
<OutDir>$(OutputPath)</OutDir>
```

Use `Microsoft.NET.Sdk` unless Razor views are required.

Use central package versions. Add `<PackageReference Include="PackageName" />` without a version when listed in `Directory.Packages.props`.

Set shared GrandNode project references to `Private=False` and `ExcludeAssets=runtime` when following existing module patterns.

## Startup Rules
Use `IStartupApplication` for module service registration and middleware setup.

Register services in `ConfigureServices`.

Keep `Configure` empty unless the module owns middleware, endpoints, OpenAPI setup, CORS, authentication, or other pipeline behavior.

Set `Priority` deliberately:
- Use nearby module priority for similar behavior.
- Use a higher priority only when dependencies must be registered after core services.
- Use `BeforeConfigure` only when middleware must run before normal application configuration.

## API Module
Use `Grand.Module.Api` for REST API controllers, DTOs, validators, mapping profiles, JWT, OpenAPI, query filters, and API-specific security.

Check:
- controller authorization
- model validation
- DTO mapping
- command and query handler behavior
- OpenAPI metadata
- status codes
- store, vendor, customer, and permission scope

Combine with `api-review`, `security-review`, and `mongodb-review` when relevant.

## Installer Module
Use `Grand.Module.Installer` for installation flow, initial data, permission seeding, settings seeding, store setup, and first-run behavior.

Check:
- idempotency
- default admin and store setup
- seeded permissions
- seeded settings
- sample data safety
- localization resources
- database connection handling

## Migration Module
Use `Grand.Module.Migration` for versioned data and configuration migrations.

Check:
- version ordering
- idempotency
- resumability
- mixed-version compatibility
- data loss risk
- rollback expectations
- batch behavior for large collections

Combine with `mongodb-review` for data migrations.

## Scheduled Tasks Module
Use `Grand.Module.ScheduledTasks` for recurring background tasks.

Register tasks with keyed scoped registration:

```csharp
serviceCollection.AddKeyedScoped<IScheduleTask, TaskType>("Task name");
```

Check:
- task name matches installed or migrated schedule task data
- idempotency
- cancellation behavior
- retry and partial failure behavior
- concurrent execution risk
- store, language, currency, and customer context
- logging and observability

