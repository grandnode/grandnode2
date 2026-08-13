# Readiness health check — design

**Date:** 2026-08-13
**Related:** `docs/architecture/grandnode-architecture-roadmap.md` → `OBS-001`, `OBS-011`

## Problem

`AddGrandHealthChecks` registers a single check (`"self"`) that always returns `Healthy`, and
`UseGrandHealthChecks` maps only `/health/live` to it
(`src/Web/Grand.Web.Common/Infrastructure/ServiceCollectionExtensions.cs:267-271`,
`src/Web/Grand.Web.Common/Infrastructure/ApplicationBuilderExtensions.cs:196-199`). There is no
`/health/ready` endpoint, so an orchestrator has no way to tell "process is alive" apart from
"instance has finished starting and is configured to serve traffic".

The architecture roadmap (`OBS-011`) recommends a `/health/ready` that also pings MongoDB and
Redis. **Explicit scope decision for this change: do not probe MongoDB or Redis.** Readiness here
checks only the application process itself — no network calls to external dependencies. Checking
those is left as a future extension (tracked by `OBS-011`).

## Design

Two tags distinguish the two endpoints, following the standard ASP.NET Core
`Microsoft.Extensions.Diagnostics.HealthChecks` pattern:

- **`live`** — existing `"self"` check, unchanged behavior (always `Healthy`). Tagged `"live"`.
- **`ready`** — new `"startup"` check, tagged `"ready"`. `Healthy` only when both hold:
  1. `IHostApplicationLifetime.ApplicationStarted.IsCancellationRequested` is `true` — every
     `IStartupApplication.Configure` and hosted service has completed startup. Guards against an
     orchestrator routing traffic to an instance that is still initializing.
  2. `DataSettingsManager.DatabaseIsInstalled()` is `true` — the instance has a connection string
     configured (via the install wizard's `App_Data/Settings.txt` or via
     `ConnectionStrings`/environment configuration read by `StartupBase.InitDatabase`). This is a
     read of already-loaded in-memory state, not a network call — no DB/Redis is contacted. It
     distinguishes a freshly-deployed instance still waiting on the install wizard from a
     configured one.

### Implementation

- `ServiceCollectionExtensions.AddGrandHealthChecks`:
  - `hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])`
  - `hcBuilder.AddCheck<StartupHealthCheck>("startup", tags: ["ready"])`
  - New `StartupHealthCheck : IHealthCheck` (constructor-injects `IHostApplicationLifetime`)
    implementing the two conditions above.
- `ApplicationBuilderExtensions.UseGrandHealthChecks`:
  - `application.UseHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });`
  - `application.UseHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });`
  - A short comment on the `/health/ready` mapping states the check intentionally does not probe
    MongoDB/Redis and points to `OBS-011` for the tracked extension.

### Testing

- Unit test for `StartupHealthCheck` in `src/Tests/Grand.Web.Common.Tests`:
  - Unhealthy when application has not finished starting.
  - Unhealthy when started but `DatabaseIsInstalled()` is false.
  - Healthy when both conditions are true.

### Non-goals

- No MongoDB/Redis connectivity check (explicit user decision — future work under `OBS-011`).
- No change to `/health/live` behavior.
- No health check UI/dashboard.

## Acceptance criteria

- [ ] `/health/live` still returns 200 unconditionally (unchanged).
- [ ] `/health/ready` returns 503 before `ApplicationStarted` fires.
- [ ] `/health/ready` returns 503 when the database/install is not configured.
- [ ] `/health/ready` returns 200 once both conditions hold.
- [ ] Unit tests for `StartupHealthCheck` cover all three states above.
