# Principles

Why the code is shaped the way it is. These are judgment calls, not mechanical rules — when two principles conflict, the one earlier in this list wins.

Hard prohibitions live in `.ai/constraints.md`. Mechanical conventions live in `.ai/standards/`.

---

## 1. Correctness across boundaries beats everything

GrandNode is multi-store, multi-vendor, multi-language, multi-currency. A feature that works for one store and leaks another's data is not a working feature.

Every query, cache key, and setting read carries its scope. When in doubt about where a scope belongs, put it in the query. See `.ai/knowledge/scoping.md`.

## 2. The domain has no dependencies

`Grand.Domain` knows nothing about MongoDB, HTTP, Razor, or MediatR. Business logic that belongs to the domain lives with the entity; business logic about a use case lives in `Grand.Business.*`.

Infrastructure never leaks inward. A `MongoDB.Driver` type in a business service signature, a `HttpContext` in a domain method, or a view model in a service is the same mistake in three places.

## 3. Prefer explicit over implicit

- Pass the store id; do not reach for ambient context in a service.
- Name the resource key; do not compose it at runtime from fragments.
- Declare the dependency in the constructor; do not resolve it from `IServiceProvider`.
- State the cache key's parameters in the constant's `<remarks>`; do not leave callers guessing what `{0}` is.

Explicit code is greppable. Implicit code is only discoverable by running it.

## 4. Composition over inheritance

Extend through interfaces and registration, not through base classes. A plugin adds behavior by registering a provider or an `INotificationHandler`, not by subclassing core services.

The base classes that do exist (`BasePlugin`, `BaseEntity`, `Base*Controller`) supply mechanics, not behavior. When a new capability is needed, add an interface and register an implementation.

## 5. Expected failures are values; unexpected failures are exceptions

A declined payment, a failed login, an out-of-stock item, and a rejected validator are all normal outcomes. They return result objects — `PlaceOrderResult`, `CustomerLoginResults`, `ProcessPaymentResult`.

Exceptions are for conditions no caller can sensibly handle. Using an exception for a business outcome makes the happy path unreadable and the failure path untestable.

## 6. Extension points over forks

Everything an integrator might want to change should be reachable without editing core: providers, notification handlers, widget zones, view-location fallback, settings, message tokens.

When a plugin cannot do something without a core change, the right fix is usually a new extension point in core — not a bigger plugin, and not a fork. A theme that copies every view has stopped being an extension.

## 7. Optimize for the next reader

The next reader is someone debugging a store outage with no context. Favour:

- one obvious path over a clever general one
- a longer name over an abbreviation
- a flat sequence over nested conditionals
- deleting dead code over keeping it commented

Performance work is justified by a measurement, not by intuition. An unmeasured optimization that costs readability is a net loss.

## 8. Consistency with the neighbourhood beats personal preference

The strongest signal for how to write something is the closest existing file that does the same job. Match its structure, naming, comment density, and error handling.

If the local pattern is genuinely wrong, fix it deliberately and separately — not as a silent side effect of another change.

## 9. Changes are safe for existing installations

Every installation upgrades in place. A new setting needs a default that preserves current behavior. A new permission needs a migration. A renamed system name breaks a plugin that shipped.

The question is always: what happens to a store that already has data and is running the previous version?

## 10. Small, reversible, and stated

One logical change per commit. A refactor and a behavior change never travel together. If the diff cannot be explained in two sentences, it is doing more than one thing.

Say what was validated and what was not. An unverified claim in a PR description costs more than the work it describes.
