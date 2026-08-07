# Prompt: Implement Feature

## Purpose
Implement a new feature or change an existing one in GrandNode without breaking layering, scoping, or existing conventions.

## Inputs Required
- Repository root.
- Feature goal stated in user terms (what a customer, store owner, vendor, or admin should be able to do).
- Target area: storefront, admin, store area, vendor area, API module, plugin, or background task.
- Whether the feature needs persisted settings, localization resources, permissions, or new domain entities.

## Steps

1. Read `AGENTS.md` and identify every skill that applies. A feature that touches UI, data, and permissions needs all three.
2. Read `.ai/knowledge/repository-map.md` to place each new file in the owning project.
3. Read `.ai/knowledge/architecture.md` and `.ai/knowledge/request-lifecycle.md` before adding a controller action, handler, or service.
4. Read `.ai/knowledge/scoping.md` when the feature exposes data that belongs to a store, vendor, customer group, language, or currency.
5. Locate the closest existing feature of the same shape and follow it. Name the file you are copying from in your output.
6. Implement in this order, stopping when a step does not apply:
   1. Domain entity or settings class in `Grand.Domain`.
   2. Repository access through `IRepository<T>` in the business layer.
   3. Business service + interface in `Grand.Business.Core` / `Grand.Business.*`.
   4. Mediator command/query + handler for the web layer.
   5. FluentValidation validator.
   6. Controller action with the correct authorization attribute.
   7. View model, Razor view, and localization keys.
   8. Permission entry and admin sitemap entry, when the feature is admin-facing.
   9. Migration that seeds settings, resources, permissions, or sitemap entries.
   10. Unit tests.
7. Follow `.ai/standards/naming.md` for every new type, file, setting key, and localization key.
8. Build the narrowest affected project. Run the matching test project in `src/Tests/`.
9. Run `.ai/prompts/review-change.md` against your own diff before reporting.

## Mandatory Rules

1. Do not add business logic to controllers — delegate to the mediator or a business service.
2. Do not reference concrete MongoDB types from the business layer; use `IRepository<T>`.
3. Do not register services in `Program.cs`; use `IStartupApplication` in the owning project.
4. Do not add a NuGet package version inline; add it to `Directory.Packages.props`.
5. Do not hardcode user-facing strings; add localization resources and a migration that imports them.
6. Do not add a new permission without a `PermissionProvider` entry and a migration.
7. Do not widen scope beyond the stated goal.

## Output Format

- **Goal**: one sentence restating what was built.
- **Files changed**: path + one-line reason for each.
- **Pattern followed**: the existing file(s) used as the template.
- **Migration**: what the migration seeds, or why none is needed.
- **Validation**: build and test commands run, with results. Name any command that could not be run.
- **Risk**: what is untested or assumed.
