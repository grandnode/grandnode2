# Readiness Health Check Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `/health/ready` endpoint, distinct from the existing `/health/live`, that reports whether the application process has finished starting and is configured — without probing MongoDB or Redis.

**Architecture:** Two tags (`live`, `ready`) partition the ASP.NET Core `HealthCheckService` registry. `/health/live` keeps mapping to the existing always-healthy `"self"` check (tag `live`). A new `StartupHealthCheck` (tag `ready`) is healthy only once `IHostApplicationLifetime.ApplicationStarted` has fired and `DataSettingsManager.DatabaseIsInstalled()` reports a configured connection string — both are in-memory/process-local checks, no network I/O. `/health/ready` is mapped with a `Predicate` that filters to the `ready` tag.

**Tech Stack:** ASP.NET Core `Microsoft.Extensions.Diagnostics.HealthChecks` (already referenced), MSTest + Moq (existing test stack in `Grand.Web.Common.Tests`).

## Global Constraints

- Do not add any MongoDB or Redis connectivity check — readiness here covers the application process only. (Explicit scope decision; tracked as a future extension under `OBS-011` in `docs/architecture/grandnode-architecture-roadmap.md`.)
- `/health/live` behavior must not change (always returns `Healthy`).
- No new NuGet packages — `Microsoft.Extensions.Diagnostics.HealthChecks` and `IHostApplicationLifetime` (part of `Microsoft.Extensions.Hosting.Abstractions`) are already available transitively.
- Follow existing folder→namespace convention in `Grand.Web.Common` (e.g. `Infrastructure/Middleware` → `Grand.Web.Common.Middleware`).
- Follow existing MSTest + Moq test conventions (see `src/Tests/Grand.Web.Common.Tests/Infrastructure/BackgroundServiceTaskTests.cs`).

---

### Task 1: `StartupHealthCheck`

**Files:**
- Create: `src/Web/Grand.Web.Common/Infrastructure/HealthChecks/StartupHealthCheck.cs`
- Test: `src/Tests/Grand.Web.Common.Tests/Infrastructure/HealthChecks/StartupHealthCheckTests.cs`

**Interfaces:**
- Consumes: `Grand.Data.DataSettingsManager.DatabaseIsInstalled()` (static, existing), `Microsoft.Extensions.Hosting.IHostApplicationLifetime.ApplicationStarted` (framework-provided `CancellationToken` property).
- Produces: `Grand.Web.Common.Infrastructure.HealthChecks.StartupHealthCheck`, a public class implementing `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck` with a public constructor `StartupHealthCheck(IHostApplicationLifetime applicationLifetime)`. Task 2 registers this type with `hcBuilder.AddCheck<StartupHealthCheck>(...)`.

`DataSettingsManager` is a process-wide static singleton whose `DatabaseIsInstalled()` result is cached after the first call and `ResetCache()` can only force it to `false` (never back to `true`) — see `src/Core/Grand.Data/DataSettingsManager.cs:74-91`. To get independent, order-safe `true`/`false` results per test, each test resets the private static `_instance` field via reflection and re-initializes against its own temp settings file, mirroring the fresh-`Initialize` pattern already used in `src/Tests/Grand.Domain.Tests/Data/DataSettingsManagerTests.cs` and `src/Tests/Grand.Web.Common.Tests/AuthorizeMenuAttributeTests.cs:33-35`, but going one step further (full instance reset) so the "installed" and "not installed" cases don't leak into each other within the same test process.

- [ ] **Step 1: Write the failing tests**

Create `src/Tests/Grand.Web.Common.Tests/Infrastructure/HealthChecks/StartupHealthCheckTests.cs`:

```csharp
using System.Reflection;
using Grand.Data;
using Grand.Web.Common.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Infrastructure.HealthChecks;

[TestClass]
public class StartupHealthCheckTests
{
    // DataSettingsManager caches DatabaseIsInstalled() on first call and its public ResetCache()
    // can only force the cached value to false, never back to true - see DataSettingsManager.cs.
    // Resetting the private static instance per test keeps "installed" and "not installed" cases
    // independent instead of depending on test execution order.
    private static readonly FieldInfo InstanceField = typeof(DataSettingsManager)
        .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)!;

    private string _settingsPath = null!;

    [TestInitialize]
    public void Setup()
    {
        InstanceField.SetValue(null, null);
        _settingsPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
        DataSettingsManager.Initialize(_settingsPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_settingsPath))
            File.Delete(_settingsPath);
    }

    private static Mock<IHostApplicationLifetime> MockLifetime(bool started)
    {
        var cts = new CancellationTokenSource();
        if (started) cts.Cancel();

        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.Setup(l => l.ApplicationStarted).Returns(cts.Token);
        return lifetime;
    }

    [TestMethod]
    public async Task CheckHealthAsync_ApplicationNotStarted_ReturnsUnhealthy()
    {
        DataSettingsManager.Instance.LoadDataSettings(
            new DataSettings { ConnectionString = "mongodb://localhost/test", DbProvider = DbProvider.MongoDB });

        var check = new StartupHealthCheck(MockLifetime(started: false).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
    }

    [TestMethod]
    public async Task CheckHealthAsync_StartedButDatabaseNotConfigured_ReturnsUnhealthy()
    {
        // no connection string loaded - DatabaseIsInstalled() evaluates to false on first call
        var check = new StartupHealthCheck(MockLifetime(started: true).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
    }

    [TestMethod]
    public async Task CheckHealthAsync_StartedAndDatabaseConfigured_ReturnsHealthy()
    {
        DataSettingsManager.Instance.LoadDataSettings(
            new DataSettings { ConnectionString = "mongodb://localhost/test", DbProvider = DbProvider.MongoDB });

        var check = new StartupHealthCheck(MockLifetime(started: true).Object);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.AreEqual(HealthStatus.Healthy, result.Status);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail to compile**

Run: `dotnet test src/Tests/Grand.Web.Common.Tests/Grand.Web.Common.Tests.csproj --filter StartupHealthCheckTests`
Expected: build error — `Grand.Web.Common.Infrastructure.HealthChecks.StartupHealthCheck` does not exist.

- [ ] **Step 3: Write the implementation**

Create `src/Web/Grand.Web.Common/Infrastructure/HealthChecks/StartupHealthCheck.cs`:

```csharp
using Grand.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Grand.Web.Common.Infrastructure.HealthChecks;

/// <summary>
///     Reports whether the application has finished starting and is configured with a database
///     connection. Intentionally does not probe MongoDB or Redis - see OBS-011 in
///     docs/architecture/grandnode-architecture-roadmap.md for that extension.
/// </summary>
public class StartupHealthCheck : IHealthCheck
{
    private readonly IHostApplicationLifetime _applicationLifetime;

    public StartupHealthCheck(IHostApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_applicationLifetime.ApplicationStarted.IsCancellationRequested)
            return Task.FromResult(HealthCheckResult.Unhealthy("Application has not finished starting."));

        return Task.FromResult(DataSettingsManager.DatabaseIsInstalled()
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Database connection is not configured."));
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test src/Tests/Grand.Web.Common.Tests/Grand.Web.Common.Tests.csproj --filter StartupHealthCheckTests`
Expected: 3 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Web/Grand.Web.Common/Infrastructure/HealthChecks/StartupHealthCheck.cs src/Tests/Grand.Web.Common.Tests/Infrastructure/HealthChecks/StartupHealthCheckTests.cs
git commit -m "Add StartupHealthCheck for application readiness"
```

---

### Task 2: Register the readiness check with tags

**Files:**
- Modify: `src/Web/Grand.Web.Common/Infrastructure/ServiceCollectionExtensions.cs:267-271`

**Interfaces:**
- Consumes: `Grand.Web.Common.Infrastructure.HealthChecks.StartupHealthCheck` (Task 1).
- Produces: two tagged health checks (`"self"` tagged `"live"`, `"startup"` tagged `"ready"`) in the DI-registered `HealthCheckService`, consumed by Task 3's endpoint mapping via `HealthCheckOptions.Predicate`.

- [ ] **Step 1: Update `AddGrandHealthChecks`**

Replace (`src/Web/Grand.Web.Common/Infrastructure/ServiceCollectionExtensions.cs:267-271`):

```csharp
    public static void AddGrandHealthChecks(this IServiceCollection services)
    {
        var hcBuilder = services.AddHealthChecks();
        hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());
    }
```

with:

```csharp
    public static void AddGrandHealthChecks(this IServiceCollection services)
    {
        var hcBuilder = services.AddHealthChecks();
        //liveness: process can respond to a request - never touches an external dependency
        hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);
        //readiness: application finished starting and is configured - intentionally does not
        //probe MongoDB/Redis, see OBS-011 in docs/architecture/grandnode-architecture-roadmap.md
        hcBuilder.AddCheck<StartupHealthCheck>("startup", tags: ["ready"]);
    }
```

Add the using at the top of the file (alongside the existing `using Microsoft.Extensions.Diagnostics.HealthChecks;` on line 19):

```csharp
using Grand.Web.Common.Infrastructure.HealthChecks;
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/Web/Grand.Web.Common/Grand.Web.Common.csproj`
Expected: build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add src/Web/Grand.Web.Common/Infrastructure/ServiceCollectionExtensions.cs
git commit -m "Tag health checks as live/ready and register StartupHealthCheck"
```

---

### Task 3: Map `/health/ready`

**Files:**
- Modify: `src/Web/Grand.Web.Common/Infrastructure/ApplicationBuilderExtensions.cs:192-199`

**Interfaces:**
- Consumes: the `"live"`/`"ready"` tags registered in Task 2.
- Produces: `/health/live` and `/health/ready` HTTP endpoints, mapped by `GrandMvcStartup`/`GrandCommonStartup` (unchanged call site — `application.UseGrandHealthChecks()` in `src/Web/Grand.Web.Common/Startup/GrandCommonStartup.cs:97`).

- [ ] **Step 1: Update `UseGrandHealthChecks`**

Replace (`src/Web/Grand.Web.Common/Infrastructure/ApplicationBuilderExtensions.cs:196-199`):

```csharp
    public static void UseGrandHealthChecks(this WebApplication application)
    {
        application.UseHealthChecks("/health/live");
    }
```

with:

```csharp
    public static void UseGrandHealthChecks(this WebApplication application)
    {
        application.UseHealthChecks("/health/live", new HealthCheckOptions {
            Predicate = check => check.Tags.Contains("live")
        });

        //intentionally does not probe MongoDB/Redis - see OBS-011 in
        //docs/architecture/grandnode-architecture-roadmap.md for that extension
        application.UseHealthChecks("/health/ready", new HealthCheckOptions {
            Predicate = check => check.Tags.Contains("ready")
        });
    }
```

Add the using at the top of the file (alongside the existing `using Microsoft.AspNetCore.Builder;` on line 8):

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build src/Web/Grand.Web.Common/Grand.Web.Common.csproj`
Expected: build succeeds with no errors.

- [ ] **Step 3: Manual verification against a running host**

Run: `dotnet run --project src/Web/Grand.Web/Grand.Web.csproj` (Kestrel, per `reference_running_the_storefront` — IIS Express needs a `web.config` this repo doesn't ship).

Once the app is listening:

```bash
curl -i http://localhost:<port>/health/live
curl -i http://localhost:<port>/health/ready
```

Expected: both return `200 OK` with body `Healthy` once the app and (if applicable) the install wizard have completed. If the instance has no database configured yet, `/health/ready` returns `503 Service Unavailable` while `/health/live` still returns `200 OK`.

- [ ] **Step 4: Commit**

```bash
git add src/Web/Grand.Web.Common/Infrastructure/ApplicationBuilderExtensions.cs
git commit -m "Map /health/ready alongside /health/live"
```

---

## Definition of Done

- [ ] All three `StartupHealthCheck` unit tests pass (Task 1).
- [ ] `Grand.Web.Common` builds with no warnings introduced.
- [ ] `/health/live` still returns `200` unconditionally (unchanged behavior, verified manually in Task 3).
- [ ] `/health/ready` returns `503` before startup completes / before the database is configured, `200` once both hold (verified manually in Task 3).
- [ ] No MongoDB or Redis check was added anywhere in this change.
- [ ] Design spec (`docs/superpowers/specs/2026-08-13-readiness-health-check-design.md`) and this plan are committed alongside the code changes.
