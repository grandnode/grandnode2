# Workflow: Upgrade a Dependency

For moving a NuGet package, the target framework, or a shared version. Versions are central (`Directory.Packages.props`), so **one bump affects every project**.

---

## Phase 1 — Scope the blast radius

1. Identify what is moving and from which version to which.
2. Find every consumer: `Directory.Packages.props` declares the version; grep the `PackageReference` entries to see which projects use it.
3. Classify the jump: patch, minor, or major. A major version means breaking changes are expected, not possible.
4. Read the release notes between the two versions — all of them, not just the newest.
5. Check whether plugins depend on it. A plugin built against the old version and shipped separately may break at load time.
6. State why the upgrade is happening: a CVE, a needed feature, framework alignment, or routine maintenance. "It is newer" is not a reason.

**Gate:** you can list every project affected and name the breaking changes in the range.

## Phase 2 — Check the alternatives

1. Is a smaller jump enough? A patch that fixes a CVE beats a major version bump.
2. Does the repository already have another way to do this? See the capability table in `.ai/standards/dependencies.md` — the answer for a *new* package is often "we already have one".
3. For a framework or SDK move, is `src/Build/Grand.Common.props` the right place? `TargetFramework` and `LangVersion` are set there and must not be overridden per project.

**Gate:** the chosen version is the smallest change that solves the stated reason.

## Phase 3 — Apply

1. Change the version in `Directory.Packages.props` only. Never add a version to a `.csproj`.
2. Restore and build the **whole** solution — not just the project you care about. Central versions mean the break can surface anywhere.
3. Fix compilation errors without changing behavior. If an API was removed, adapt at the call site; do not redesign around it in the same change.
4. If a transitive dependency now conflicts, resolve it explicitly rather than pinning a second version.
5. Keep the upgrade in its own commit. Do not bundle it with a feature.

**Gate:** the whole solution builds.

## Phase 4 — Verify behavior, not just compilation

A dependency upgrade compiles and then behaves differently. Check the areas that library actually touches:

| Library | Verify |
|---|---|
| MongoDB driver | queries, serialization of `string` ids, date handling, aggregations |
| MediatR | handler discovery, notification ordering, registration in plugins |
| FluentValidation | validator discovery, message text, rule chaining semantics |
| Serialization | round-trip of persisted documents, API request/response shapes |
| ASP.NET Core / SDK | middleware order, routing, model binding, antiforgery, auth |
| Redis / caching | invalidation propagation between instances |
| DotLiquid | message template rendering and token resolution |
| Image / PDF | generated output, not just the absence of an exception |

Then:

- [ ] All test projects run, not just the nearest one.
- [ ] The application starts and serves a storefront page.
- [ ] The admin loads.
- [ ] Plugins still load and their providers still resolve — assembly scanning is version-sensitive.
- [ ] A message template renders.
- [ ] An order can be placed, if the change touches anything in that path.

**Gate:** the library's actual behavior verified, not just a green build.

## Phase 5 — Assess the deployment risk

- Does an existing installation need anything, or is it drop-in?
- Does the serialized shape of any persisted document change? If so, this is a data change — run `.ai/checklists/data-change.md`.
- Do third-party plugins compiled against the old version still load?
- What is the rollback: revert the version, or is there persisted state to undo?

## Phase 6 — Report

- **Package**: name, old version, new version, jump type.
- **Reason**: the specific driver for the upgrade.
- **Projects affected**: from phase 1.
- **Breaking changes**: from the release notes, and how each was handled.
- **Verification**: what was actually exercised, per the phase 4 table.
- **Plugin impact**: whether externally built plugins still load.
- **Rollback**: how to undo it.
- **Not verified**: stated plainly.
