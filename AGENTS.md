# Repository Agent Instructions

Instructions for any AI agent that changes or reviews code in this repository. They do not depend on a particular tool: Claude Code loads this file through `CLAUDE.md`, and GitHub Copilot through `.github/copilot-instructions.md`. Everything under `.ai/` is indexed here.

## How to start

1. Pick a **prompt** when the goal is known, or a **workflow** when the cause, bottleneck, or path still has to be found. Each one names the skills, knowledge, standards, and templates to load.
2. Read `.ai/constraints.md` (hard prohibitions) and `.ai/glossary/vocabulary.md` (the domain's words) once in full. Both apply to every change.
3. Find the closest existing code of the same shape and follow it. When a standard and that code disagree, follow the code and say so.
4. Finish with the checklists that match the change.

| Folder | Holds | Reach for it to |
|---|---|---|
| `.ai/principles.md` | the reasons the code looks the way it does | settle a judgment call between two valid options |
| `.ai/constraints.md` | hard prohibitions | know what is never acceptable |
| `.ai/prompts/` | entry points for a known task | start building |
| `.ai/workflows/` | procedures with gates | start finding out |
| `.ai/skills/` | domain procedures with mandatory rules | do or review work in one domain |
| `.ai/knowledge/` | how the system works | get context before touching it |
| `.ai/glossary/` | domain vocabulary | name things the way the code does |
| `.ai/standards/` | binding conventions | name, format, structure, and ship |
| `.ai/checklists/` | cross-cutting gates | verify before calling it done |
| `.ai/examples/` | walkthroughs of shipped code | see the rules applied |
| `.ai/templates/` | copy-ready skeletons | scaffold a plugin, theme, or migration |
| `.ai/agents/` | thin wrappers around a prompt or workflow | hand off a whole recurring task |

## Prompts — the goal is known

| Prompt | For |
|---|---|
| `.ai/prompts/implement-feature.md` | a feature or change across layers |
| `.ai/prompts/review-change.md` | reviewing a pull request or diff against every applicable skill |
| `.ai/prompts/create-plugin.md` | a new installable plugin |
| `.ai/prompts/create-theme.md` | a storefront theme, or view overrides in one |
| `.ai/prompts/add-migration.md` | a migration for existing installations |
| `.ai/prompts/write-tests.md` | new or extended unit tests |
| `.ai/prompts/explore-repository.md` | "where does X live" and "how does X work" |
| `.ai/prompts/convert-admin-screens.md` | one admin module (list, edit, tabs, popups) moved to the component layer across Admin, Store and Vendor |
| `.ai/prompts/upgrade-kendo-view.md` | an admin or plugin view moved off Kendo UI, Bootstrap 4 and Font Awesome onto `<admin-grid>`, `window.GrandAdmin` and Bootstrap 5 |

## Workflows — the answer is not known yet

Each workflow ends by handing off to a prompt.

| Workflow | When |
|---|---|
| `.ai/workflows/fix-bug.md` | something is broken and the cause is unknown: reproduce, find the cause, choose a fix, assess risk, test, verify |
| `.ai/workflows/investigate-performance.md` | something is slow and the bottleneck is unknown: define, measure, explain, fix, re-measure |
| `.ai/workflows/refactor-safely.md` | structure changes, behavior must not |
| `.ai/workflows/upgrade-dependency.md` | moving a NuGet package, the framework, or a shared version |
| `.ai/workflows/respond-to-review.md` | working through review feedback, automated findings included |

## Skills — one domain each

A change that crosses domains loads every skill it touches.

| Skill | Covers |
|---|---|
| `.ai/skills/architecture-review.md` | design, layering, dependencies, module boundaries, public contracts, maintainability |
| `.ai/skills/security-review.md` | authentication, authorization, input, secrets, cryptography, sensitive data, trust boundaries |
| `.ai/skills/dotnet-review.md` | C#, .NET, ASP.NET Core, repository usage, NuGet, MSBuild, .NET tests |
| `.ai/skills/database-review.md` | migrations, schema, indexes, queries, transactions, mappings, data integrity |
| `.ai/skills/mongodb-review.md` | collections, filters, projections, indexes, repository queries, aggregations, updates, data migrations |
| `.ai/skills/project-structure.md` | repository layout, technology ownership, layer responsibilities, growing GrandNode consistently |
| `.ai/skills/plugin-module.md` | plugins and modules in general: provider plugins, themes, API modules, migrations, installer behavior |
| `.ai/skills/plugin-payment.md` | `IPaymentProvider`, Standard vs Redirection flow, ProcessPayment, Capture, Refund, Void, `PaymentTransaction` status |
| `.ai/skills/plugin-shipping.md` | `IShippingRateCalculationProvider`, GetShippingOptions, `ShippingOption`, `ShippingRateCalculationType`, `IShipmentTracker` |
| `.ai/skills/plugin-widget.md` | `IWidgetProvider`, GetWidgetZones, view components, widget zone names, GDPR consent gating |
| `.ai/skills/plugin-discount-rules.md` | `IDiscountProvider`, `IDiscountRule`, CheckRequirement, `DiscountRule.Metadata`, rule configuration controllers |
| `.ai/skills/theme-creation.md` | `IThemeView`, GetViewLocations fallback, theme view folders, theme `_ViewImports`, theme Content assets, theme project setup |
| `.ai/skills/template-creation.md` | Razor views, layouts, partials, view components, plugin views, theme overrides, Vue-in-Razor, PDF templates, DotLiquid message templates |
| `.ai/skills/frontend-bundle-workflow.md` | Vue/Vite build, theme CSS, when to run `npm run build`, bundle outputs, committing bundles with their source |
| `.ai/skills/admin-area-changes.md` | anything admin-facing that may reach Main Admin, Store Owner or Vendor: shared models, permissions, navigation, validation, scoped data |
| `.ai/skills/admin-ui-component-layer.md` | building or converting an admin screen, tab or popup: `<admin-page>`, `<admin-card>`, `<admin-field>`, `<admin-filters>`, `<admin-popup>`, button hierarchy, Add new placement, known CSS traps |
| `.ai/skills/settings-and-localization.md` | settings classes, store-scoped overrides, `ISettingService`, translation resources, `ITranslationService`, `IPluginTranslateResource`, translated entities and admin models |
| `.ai/skills/message-notification.md` | message templates, DotLiquid tokens and drops, `IMessageProviderService`, queued email lifecycle, `LiquidObjectBuilder`, `MessageTokensAddedEvent`, notification handlers |
| `.ai/skills/scheduled-task.md` | `IScheduleTask`, `AddKeyedScoped` registration, `ScheduleTask` seed, distributed locking across instances, error handling, task migrations |
| `.ai/skills/permission-navigation.md` | `PermissionSystemName`, `PermissionActionName`, `StandardPermission`, `PermissionProvider`, controller authorization attributes, `AdminSiteMap` entries and their migrations |

## Knowledge — how the system works

| File | Explains |
|---|---|
| `.ai/knowledge/repository-map.md` | which project owns what, and where a new file goes |
| `.ai/knowledge/architecture.md` | layering, DI lifetimes, mediator commands and queries, domain events |
| `.ai/knowledge/request-lifecycle.md` | startup, `IStartupApplication` priorities, middleware order, `ContextMiddleware`, controller to view |
| `.ai/knowledge/scoping.md` | store, vendor, customer group, language and currency boundaries, and code that runs without ambient context |
| `.ai/knowledge/admin-areas.md` | the three admin panels (Admin, Store, Vendor), what each owns and how they share code |
| `.ai/knowledge/plugin-types.md` | shipped plugins by kind, to pick the closest one to copy |
| `.ai/knowledge/module-types.md` | shipped modules (API, installer, migration) and how they differ from plugins |
| `.ai/knowledge/template-types.md` | which kind of view (storefront, admin, plugin, theme, PDF, message template) a change belongs in |
| `.ai/knowledge/mongodb.md` | `IRepository<T>`, query patterns, partial updates, write conventions |
| `.ai/knowledge/caching.md` | `ICacheBase`, `CacheKey` constants, key composition, invalidation including cross-family clearing |
| `.ai/knowledge/performance.md` | pagination, partial field writes, and the cost side of caching |
| `.ai/knowledge/domain-events.md` | commands vs queries vs notifications, entity events, handler rules |
| `.ai/knowledge/security.md` | authorization checks, FluentValidation, guard clauses, HTML encoding, safe queries |
| `.ai/knowledge/async.md` | async/await, `CancellationToken`, `Task` vs `ValueTask`, blocking anti-patterns |
| `.ai/knowledge/dotnet.md` | records, guard clauses, result objects, pattern matching, nullable types, configuration binding |
| `.ai/knowledge/tests.md` | MSTest + Moq, test structure, validator tests, controller test setup |

## Glossary — use the domain's words

GrandNode names many concepts its own way. A generic e-commerce word in a type name reads as foreign to the codebase, and a search for it finds nothing. `.ai/glossary/vocabulary.md` lists the words to use and the ones to avoid: Brand, Page, Customer group, Merchandise return, Loyalty points, User field.

| File | Covers |
|---|---|
| `.ai/glossary/entity-model.md` | base entity types, marker interfaces, user fields, translated properties, slugs |
| `.ai/glossary/catalog.md` | products and product types, category/brand/collection, product vs specification attributes, pricing, inventory |
| `.ai/glossary/sales.md` | cart, order, the three order statuses, payment transactions, shipping, merchandise returns, discounts, loyalty points |
| `.ai/glossary/customers.md` | customers, groups, tags, vendors, sales employees, affiliates, the four party boundaries |
| `.ai/glossary/platform.md` | stores, localization, settings, permissions, SEO, CMS content, media, messaging, tasks |

## Standards — binding

| Standard | Governs |
|---|---|
| `.ai/standards/naming.md` | projects, types, files, members, plugin system names, setting keys, resource keys, cache key constants |
| `.ai/standards/csharp-style.md` | `.editorconfig` formatting, file layout, constructor injection, guards, what not to introduce |
| `.ai/standards/razor-frontend.md` | Razor conventions, Vue-in-Razor, storefront data attributes, admin tag helpers, asset placement |
| `.ai/standards/git-and-pr.md` | branches, commit format, the pull request template, the pre-PR checklist |
| `.ai/standards/dependencies.md` | central package management, shared MSBuild props, project references, output paths, SDK selection |

## Checklists — before calling it done

Skills carry their own domain checklists; these cover what no single skill owns.

| Checklist | Run it |
|---|---|
| `.ai/checklists/definition-of-done.md` | on every change |
| `.ai/checklists/code-review.md` | on any diff under review, your own included before a PR |
| `.ai/checklists/security.md` | when the change touches auth, input, scoped data, secrets, payments, or files |
| `.ai/checklists/performance.md` | when the change adds a query, iterates entities, or sits on a render path |
| `.ai/checklists/persisted-data.md` | when the change leaves something in the database: entities, settings, resources, permissions, migrations, persisted names |
| `.ai/checklists/plugin-release.md` | before shipping a plugin or theme |

## Examples and templates

Templates give the shape; skills are the contract. Read the skill first, then compare the scaffold with the closest shipped plugin in `src/Plugins/`.

| File | Shows |
|---|---|
| `.ai/examples/cached-store-scoped-service.md` | the reference business service: read-through cache, store scope, invalidation, entity events |
| `.ai/examples/payment-plugin-walkthrough.md` | a complete plugin, file by file, from manifest to admin configuration screen |
| `.ai/examples/theme-override-walkthrough.md` | a theme overriding some views while the rest fall through to the defaults |
| `.ai/templates/plugin/base-plugin.md` | the files every installable plugin needs |
| `.ai/templates/plugin/admin-configuration.md` | a plugin's admin configuration screen |
| `.ai/templates/theme/theme-plugin.md` | a storefront theme skeleton |
| `.ai/templates/migration.md` | a migration skeleton |

## Agents

Each file in `.ai/agents/` wraps one prompt or workflow, using the GitHub custom agent frontmatter (`name`, `description`, `tools`). The files live here so they stay indexed with the rest. Copilot only discovers custom agents in `.github/agents/`, so treat these as task descriptions to follow until they are mirrored there.

| Agent | Hands off |
|---|---|
| `.ai/agents/plugin-creator.md` | a new installable plugin, end to end |
| `.ai/agents/bug-fixer.md` | broken behavior with an unknown cause |
| `.ai/agents/test-writer.md` | new or extended unit tests |
| `.ai/agents/reviewer.md` | a pull request or diff against every applicable skill and checklist |
| `.ai/agents/dotnet-expert.md` | idiomatic C#/.NET, async/await, .NET test patterns |
| `.ai/agents/mongodb-expert.md` | MongoDB queries, indexes, aggregations, data migrations |
| `.ai/agents/security-reviewer.md` | authentication, authorization, input, secrets, trust boundaries |
| `.ai/agents/architecture-reviewer.md` | layering, module boundaries, public contracts |
| `.ai/agents/theme-builder.md` | a storefront theme or view overrides |
| `.ai/agents/migration-writer.md` | a migration for existing installations |
| `.ai/agents/admin-ui-specialist.md` | admin-facing changes across Main Admin, Store Owner and Vendor |
| `.ai/agents/performance-investigator.md` | slow queries or render paths |
| `.ai/agents/permission-navigation-specialist.md` | new or changed permissions and admin navigation |
| `.ai/agents/notification-specialist.md` | message templates, DotLiquid tokens, notification handlers |
| `.ai/agents/scheduled-task-writer.md` | a new scheduled task |
| `.ai/agents/dependency-upgrader.md` | a NuGet package, framework, or shared version move |

## Operating rules

1. Understand the user's goal before opening files.
2. Load the matching prompt or workflow, and the skills and standards it names.
3. Look at existing patterns before proposing a change. Prefer the abstractions, conventions and test utilities that are already there.
4. Name things with the glossary's words.
5. Stay inside the requested scope. Never overwrite unrelated changes or someone's local work.
6. Work on a feature branch. In Claude Code, the hook in `.claude/settings.json` blocks commits and pushes on `develop` and `main`.
7. Validate with the narrowest build or test command that proves the change.
8. Run the matching checklists before reporting completion.

## Constraints on the agent itself

The full list is in `.ai/constraints.md`. These govern how an agent behaves:

- Never invent requirements or repository conventions.
- Never broaden scope without a clear reason.
- Never leave generated, temporary, or diagnostic files behind unless they are part of the requested output.
- Never report a change as verified when it was not.

## Reporting

State what changed (with file references), what was reviewed, which commands ran and which could not, and what risk remains.
