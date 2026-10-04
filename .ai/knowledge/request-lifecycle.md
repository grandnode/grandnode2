# Request Lifecycle

How a storefront or admin HTTP request becomes a rendered page. Read this before adding a controller action, middleware, or anything that depends on "the current customer/store".

---

## Startup

`src/Web/Grand.Web/Program.cs` is deliberately thin:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddAppSettingsJsonFile(args);
builder.AddServiceDefaults();
StartupBase.ConfigureServices(builder.Services, builder.Configuration);
builder.ConfigureApplicationSettings();
builder.Services.RegisterTasks(builder.Configuration);
var app = builder.Build();
StartupBase.ConfigureRequestPipeline(app, builder.Environment);
await app.RunAsync();
```

Everything else is discovered. `StartupBase` (`src/Core/Grand.Infrastructure/StartupBase.cs`) scans assemblies — including installed plugins and modules — for `IStartupApplication` and runs them ordered by `Priority`.

**Never add registrations to `Program.cs`.** Add an `IStartupApplication` in the project that owns the service.

### `IStartupApplication`

```csharp
public interface IStartupApplication
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment);
    int Priority { get; }
    bool BeforeConfigure { get; }
}
```

- `ConfigureServices` — DI registrations. Runs for every implementation, ordered by `Priority`.
- `Configure` — middleware. Runs in two passes: all `BeforeConfigure == true` implementations first (ordered by `Priority`), then all `BeforeConfigure == false`.
- `Priority` — lower runs earlier. Observed values in `Grand.Web.Common/Startup/`:

  | Priority | Startup | Purpose |
  |---|---|---|
  | `-50` | `UrlRewriteStartup` | rewrites before anything else sees the path |
  | `-40` | `HostFilteringStartup` | |
  | `-20` | `ForwardedHeadersStartup` | must run before scheme/IP is read |
  | `-10` | `ErrorHandlerStartup` | wraps everything below it |
  | `0` | `StartupApplication` | core service registrations |
  | `100` | `GrandCommonStartup` | static files, powered-by, common middleware |
  | `500` | `AuthenticationStartup` | auth + context middleware |
  | `501` | `LoggerStartup` | |
  | `1000` | `GrandMvcStartup` | endpoints — last |
  | `10` | plugin startups (e.g. `Theme.Modern`) | |

Pick a plugin/module priority by copying the nearest comparable component, not by inventing a number.

## Per-request pipeline

Ordered by the startups above:

1. **Host filtering / forwarded headers / URL rewrite** — the request's real host, scheme, and path are settled.
2. **Error handling** — wraps everything downstream.
3. **Static files, powered-by header** (`GrandCommonStartup`).
4. **Install redirect** — `InstallUrlMiddleware` sends traffic to the installer when the DB is not installed.
5. **Authentication** — `UseGrandAuthentication()`.
6. **`ContextMiddleware`** — the important one, see below.
7. **`CultureSettingMiddleware`** — sets `CultureInfo` from the resolved working language.
8. **Endpoint routing / MVC** (`GrandMvcStartup`, priority 1000).

### ContextMiddleware

`src/Web/Grand.Web.Common/Middleware/ContextMiddleware.cs` resolves the ambient context for the request and stores it on `IContextAccessor`:

```csharp
contextAccessor.StoreContext = await storeContextSetter.InitializeStoreContext();
contextAccessor.WorkContext  = await workContextSetter.InitializeWorkContext(
    contextAccessor.StoreContext.CurrentStore.Id);
```

Consequences:

- **Store is resolved before customer.** Customer resolution depends on the store — see `.ai/knowledge/scoping.md`.
- `IWorkContext` is **not** populated for requests whose route matches the skip list: `/scalar/{documentName}`, `/openapi/{documentName}.json`, `install`. Code reachable from those endpoints must not assume a current customer.
- Anything that runs *before* `ContextMiddleware` (priority < 500) cannot read `IWorkContext`.
- Background work — scheduled tasks, message queue processing — has no HTTP request and therefore no ambient context. Such code must take the store/customer explicitly. See `.ai/skills/scheduled-task.md`.

`IWorkContext` exposes: `CurrentCustomer`, `OriginalCustomerIfImpersonated`, `CurrentVendor`, `WorkingLanguage`, `WorkingCurrency`, `StoreManager`, `TaxDisplayType`.

## Controller → response

```
Controller action
   └─ IMediator.Send(GetSomethingQuery)          ← Grand.Web/Features/Handlers/…
        └─ handler
             └─ business service (Grand.Business.*)
                  └─ ICacheBase.GetAsync(key, …)  ← Grand.Infrastructure/Caching
                       └─ IRepository<T>          ← Grand.Data
                            └─ MongoDB
   └─ View(model)
        └─ theme view-location resolution          ← IThemeView, see theme-creation skill
             └─ Razor view + view components + widget zones
```

Rules that fall out of this:

1. Controllers hold no business logic. They resolve input, send a mediator request, and return a result.
2. Query handlers live in `Features/Handlers/`, command handlers in `Commands/Handler/`, mirroring the request type's folder.
3. Handlers prepare view models. Business services do not know about view models.
4. Caching sits in the business service, wrapping the repository call — not in the handler or controller.
5. Repository access happens through `IRepository<T>`. The business layer never sees a `MongoCollection`.
6. After a write, the service publishes an entity event and clears the affected cache prefix. See `.ai/knowledge/domain-events.md` and `.ai/knowledge/caching.md`.

## View resolution

The active theme's `IThemeView.GetViewLocations()` is consulted before the default locations, so a theme overrides only the views it ships and everything else falls through to `Grand.Web/Views`. Widget zones are expanded by the core `Widget` view component, which asks every installed `IWidgetProvider` whether it renders in that zone.

## Where things go

| You are adding | Put it in |
|---|---|
| A page | controller action + mediator query + handler + view model + view |
| A cross-cutting request concern | middleware registered from an `IStartupApplication` |
| Service registration | `IStartupApplication.ConfigureServices` in the owning project |
| Something that must run before auth | `IStartupApplication` with `BeforeConfigure = true` and a low `Priority` |
| Something with no HTTP request | `IScheduleTask`, with explicit store/customer parameters |
