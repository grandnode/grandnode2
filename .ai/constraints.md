# Constraints

Hard prohibitions. Unlike `.ai/principles.md`, these are not judgment calls — a violation is a defect, and a reviewer should reject it without debating trade-offs.

Each entry states the prohibition, the reason, and what to do instead. When a constraint has a legitimate exception, the exception is listed; there are no unlisted exceptions.

---

## Dependencies

### Never add a NuGet package version in a `.csproj`
Central package management is on (`ManagePackageVersionsCentrally=true`). An inline version is a build error, not a style issue.
→ Add `<PackageVersion>` to `Directory.Packages.props`, reference without a version.

### Never add a package for a capability the repository already has
MediatR, FluentValidation, `Grand.Mapping`, `MongoDB.Driver`, `StackExchange.Redis`, DotLiquid, Scryber, ImageSharp, MailKit, Scrutor are already present. Adding a second library for the same job splits the codebase.
→ See the table in `.ai/standards/dependencies.md`.

### Never use Newtonsoft.Json
It is not referenced anywhere. `System.Text.Json` is the serializer.
→ `System.Text.Json`, or MessagePack where the existing code uses it.

### Never use AutoMapper
The package is **not** referenced, despite `Grand.Mapping` exposing an AutoMapper-compatible `Profile` / `CreateMap` / `ForMember` API. Adding it would silently shadow the in-house mapper.
→ `Grand.Mapping`, via an `IAutoMapperProfile` implementation.

### Never reference a plugin from core, business, or web projects
Dependencies point inward. A core project that knows a plugin exists cannot be built without it.
→ Define an interface in core; the plugin registers an implementation.

---

## Async

### Never block on a `Task`
No `.Result`, no `.Wait()`, no `.GetAwaiter().GetResult()` in request or service code. Under load this deadlocks or exhausts the thread pool.
→ `await` all the way down. The handful of existing occurrences are in startup paths and are not a precedent.

### Never write `async void`
There are zero in the codebase. An exception in an `async void` method cannot be caught by the caller and takes down the process.
→ `async Task`. For event handlers, use `INotificationHandler<T>` which is already `Task`-returning.

### Never use `Task.Run` to make sync code look async
It moves work to another thread without removing the blocking, and loses the request context.
→ Make the underlying call async, or leave it synchronous.

### Never swallow a `CancellationToken`
If the surrounding signatures carry one, forward it.

---

## Data and persistence

### Never inject `IMongoDatabase` or a Mongo collection into a business service
It ties the business layer to the driver and makes the service untestable.
→ `IRepository<T>`. Mongo-specific behavior belongs in `Grand.Data`.

### Never build a query from string concatenation of user input
→ Typed LINQ over `IRepository<T>.Table`, or the repository's filter helpers.

### Never filter scoped data in memory after materializing it
Loading every store's records and filtering in C# is both a performance defect and a leak waiting for the filter to be dropped.
→ Filter in the query. See `.ai/knowledge/scoping.md`.

### Never write an entity without invalidating its cache and publishing its event
A cached read that survives its write serves stale — sometimes deleted — data.
→ `RemoveByPrefix(CacheKey.*_PATTERN_KEY)` then `_mediator.EntityInserted/Updated/Deleted`. See `.ai/examples/cached-store-scoped-service.md`.

### Never omit a result-changing variable from a cache key
Store id, language id, customer group, currency, vendor id, page index. A missing store id is a cross-store data leak.

### Never reuse a migration `Identity` GUID
The runner uses it to record what already ran; a duplicate means one migration silently never executes.
→ Generate a new GUID.

### Never let a migration throw
It aborts the whole upgrade.
→ Catch and return `false`.

---

## Time, culture, and formatting

### Never use `DateTime.Now`
Stores span time zones; the server's local time is meaningless. The codebase uses `DateTime.UtcNow` almost everywhere (189 occurrences against 7).
→ `DateTime.UtcNow`, converted for display only.

### Never format or parse machine-readable values with the current culture
Prices, ids, and stored strings must not depend on the request's culture.
→ `CultureInfo.InvariantCulture` for machine-readable values; the working culture only for display.

### Never hardcode a user-facing string
→ A translation resource, added in `Install()` or an upgrade XML, read via `ITranslationService` / `@Loc[...]`.

---

## Web layer

### Never put business logic in a controller
→ A MediatR command or query handler.

### Never trust an id from a request
A posted `vendorId`, `storeId`, or `customerId` is attacker-controlled.
→ Re-check ownership against `IWorkContext` / the resolved store, server-side, before writing.

### Never omit the authorization attribute on an admin controller
`[AuthorizeAdmin]`, `[Area("...")]`, and `[PermissionAuthorize(...)]` together. A missing permission attribute leaves the screen open to any admin.

### Never register services in `Program.cs`
→ `IStartupApplication.ConfigureServices` in the owning project.

### Never read `IWorkContext` from a scheduled task, migration, or plugin `Install()`
There is no request, so there is no ambient context — it is null or stale.
→ Take store, customer, and language as explicit parameters.

### Never `Html.Raw` user-supplied content
→ Encode by default; `Html.Raw` only for content already sanitized or authored by an operator.

### Never remove a widget zone from a view
It silently disables every installed widget on that page and is a breaking change for third-party plugins.

---

## Reflection and dynamic code

### Never add ad-hoc reflection
Reflection in this repository is deliberately confined to the infrastructure that owns it: `Grand.Infrastructure.TypeSearch` (assembly scanning for `IStartupApplication`, providers, mapper profiles, validators), plugin loading, and `Grand.Infrastructure.Roslyn`. That is a platform mechanism, not a general licence.

New feature code must not use `Activator.CreateInstance`, `GetType().GetProperty(...)`, or `Type.GetType(name)` to reach behavior that a DI registration or an interface could express.
→ Register an implementation and inject the interface. If discovery is genuinely needed, use `ITypeSearcher` rather than writing new scanning.

### Never use `dynamic`
It defers every error to runtime and defeats every tool.

---

## Structure and hygiene

### Never introduce static mutable state
It breaks multi-store isolation and makes tests order-dependent.
→ A scoped service.

### Never change a shipped plugin `SystemName`, permission system name, message template name, or schedule task name
They are persisted identities. Renaming one orphans existing data in every installation.

### Never weaken or delete a failing assertion to make a suite pass
→ Find out why it now fails.

### Never leave commented-out code, `#region`, or a `TODO` without an issue number

### Never commit `obj/`, `bin/`, `TestResults/`, `.vs/`, or `.idea/`
Generated frontend bundles **are** committed, alongside the source that produced them.

### Never edit a generated bundle by hand
→ Change the source and rebuild. See `.ai/skills/frontend-bundle-workflow.md`.

---

## When a constraint blocks legitimate work

Say so explicitly in the PR, explain why the alternative does not work, and get agreement before violating it. A documented, argued exception is fine. A silent one is not.
