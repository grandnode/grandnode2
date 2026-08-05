# Repository Agent Instructions

## Purpose
Provide model-agnostic operating instructions for AI agents working in this repository.

## When To Use
Use this file before making or reviewing repository changes.

| Path | Contains | Use it to |
|---|---|---|
| `.ai/principles.md` | why the code is shaped this way | resolve a judgment call |
| `.ai/constraints.md` | hard prohibitions | know what is never acceptable |
| `.ai/prompts/` | task entry points | start work when you know what to build |
| `.ai/workflows/` | multi-phase procedures with gates | start work when you must first find something out |
| `.ai/skills/` | domain procedures with mandatory rules | do or review work in one domain |
| `.ai/knowledge/` | shared context and patterns | understand how the system works |
| `.ai/glossary/` | the domain vocabulary | name things the way this domain names them |
| `.ai/standards/` | binding conventions | name, format, structure, and ship a change |
| `.ai/checklists/` | cross-cutting gates | verify a change before calling it done |
| `.ai/examples/` | worked walkthroughs of shipped code | see the rules applied before writing new code |
| `.ai/templates/` | copy-ready skeletons | scaffold a plugin, theme, or migration |

**Start from a prompt or a workflow.** It names the skills, knowledge, standards, and templates to load. Use a prompt when the goal is known; use a workflow when the cause, bottleneck, or path is not.

`.ai/constraints.md` and `.ai/glossary/renamed-terms.md` apply to every change and are worth reading once in full.

## Prompt Routing
- Use `.ai/prompts/review-change.md` to review a pull request or diff against all applicable skills.
- Use `.ai/prompts/implement-feature.md` to implement or change a feature across layers.
- Use `.ai/prompts/create-plugin.md` to scaffold a new installable plugin.
- Use `.ai/prompts/create-theme.md` to create a storefront theme or add view overrides to one.
- Use `.ai/prompts/add-migration.md` to add an upgrade migration for existing installations.
- Use `.ai/prompts/write-tests.md` to add or extend unit tests.
- Use `.ai/prompts/explore-repository.md` to answer "where does X live" or "how does X work".

## Workflow Routing
Use a workflow when the answer is not known at the start. Each ends by handing off to a prompt.
- Use `.ai/workflows/fix-bug.md` when something is broken and the cause is unknown — reproduce, find the cause, choose a fix, assess risk, test, verify.
- Use `.ai/workflows/investigate-performance.md` when something is slow and the bottleneck is unknown — define, measure, explain, fix, re-measure.
- Use `.ai/workflows/refactor-safely.md` to change structure without changing behavior.
- Use `.ai/workflows/upgrade-dependency.md` to move a NuGet package, framework, or shared version.
- Use `.ai/workflows/respond-to-review.md` to work through review feedback, including automated findings.

## Skill Routing
- Use `.ai/skills/architecture-review.md` for design, layering, dependency, module boundary, public contract, and maintainability reviews.
- Use `.ai/skills/security-review.md` for authentication, authorization, input handling, secrets, cryptography, sensitive data, and trust-boundary reviews.
- Use `.ai/skills/dotnet-review.md` for C#, .NET, ASP.NET Core, MongoDB repository usage, NuGet, MSBuild, and .NET test reviews.
- Use `.ai/skills/database-review.md` for migrations, schema, indexes, queries, transactions, ORM mappings, and data integrity reviews.
- Use `.ai/skills/mongodb-review.md` for MongoDB collections, filters, projections, indexes, repository queries, aggregations, updates, and data migrations.
- Use `.ai/skills/plugin-module.md` for GrandNode plugins and modules, including provider plugins, themes, API modules, migrations, and installer behavior.
- Use `.ai/skills/plugin-shipping.md` for shipping rate calculation plugins (IShippingRateCalculationProvider), GetShippingOptions, ShippingOption, ShippingRateCalculationType, and IShipmentTracker.
- Use `.ai/skills/plugin-payment.md` for payment method plugins (IPaymentProvider), Standard vs Redirection flow, ProcessPayment, Capture, Refund, Void, and PaymentTransaction status.
- Use `.ai/skills/plugin-widget.md` for widget plugins (IWidgetProvider), GetWidgetZones, view components, widget zone names, and GDPR consent gating.
- Use `.ai/skills/plugin-discount-rules.md` for discount rule plugins (IDiscountProvider, IDiscountRule), CheckRequirement, DiscountRule.Metadata, and rule configuration controllers.
- Use `.ai/skills/template-creation.md` for Razor views, layouts, partials, view components, plugin views, theme overrides, Vue-in-Razor templates, PDF templates, and DotLiquid message templates.
- Use `.ai/skills/theme-creation.md` for storefront themes: IThemeView, GetViewLocations fallback, theme view folders, theme _ViewImports, theme Content assets, and theme project setup.
- Use `.ai/skills/frontend-bundle-workflow.md` for Vue/Vite build, theme CSS changes, when to run `npm run build`, bundle output files, and committing bundles alongside source.
- Use `.ai/skills/admin-area-changes.md` for admin-facing changes that may affect Main Admin, Store Owner, Vendor, shared admin models, permissions, navigation, validation, or scoped data access.
- Use `.ai/skills/project-structure.md` to understand repository structure, technology ownership, layer responsibilities, and how to expand GrandNode consistently.
- Use `.ai/skills/settings-and-localization.md` for settings classes, store-scoped overrides, ISettingService, localization resources, ITranslationService, IPluginTranslateResource, localized domain entities, and localized admin models.
- Use `.ai/skills/message-notification.md` for message templates, DotLiquid tokens and drops, IMessageProviderService, queued email lifecycle, LiquidObjectBuilder, MessageTokensAddedEvent plugin extension, and domain event notification handlers.
- Use `.ai/skills/scheduled-task.md` for scheduled task classes, IScheduleTask, AddKeyedScoped registration, ScheduleTask seed, multi-instance distributed locking, error handling, and task migrations.
- Use `.ai/skills/permission-navigation.md` for permissions, PermissionSystemName, PermissionActionName, StandardPermission, PermissionProvider, controller authorization attributes, AdminSiteMap entries, and permission or navigation migrations.
- Use `.ai/knowledge/async.md` for async/await patterns, CancellationToken, Task vs ValueTask, and blocking anti-patterns.
- Use `.ai/knowledge/mongodb.md` for IRepository<T> usage, query patterns, partial updates, and write conventions.
- Use `.ai/knowledge/performance.md` for ICacheBase, CacheKey constants, cache invalidation, pagination, and partial field writes.
- Use `.ai/knowledge/security.md` for authorization checks, FluentValidation patterns, guard clauses, HTML encoding, and safe MongoDB queries.
- Use `.ai/knowledge/architecture.md` for layering rules, DI lifetimes, MediatR commands/queries, and domain events.
- Use `.ai/knowledge/request-lifecycle.md` for startup, IStartupApplication priorities, middleware order, ContextMiddleware, and the controller-to-view path.
- Use `.ai/knowledge/scoping.md` for store, vendor, customer group, language, and currency boundaries, and for code that runs without ambient context.
- Use `.ai/knowledge/caching.md` for ICacheBase, CacheKey constants, key composition, and invalidation including cross-family clearing.
- Use `.ai/knowledge/domain-events.md` for commands vs queries vs notifications, entity events, and notification handler rules.
- Use `.ai/knowledge/tests.md` for MSTest + Moq patterns, test structure, validator testing, and controller test setup.
- Use `.ai/knowledge/dotnet.md` for C# idioms: records, guard clauses, result objects, pattern matching, nullable types, and configuration binding.
- Use multiple skills when a change crosses domains.

## Glossary Routing
Use the domain's own vocabulary. GrandNode renamed much of the nopCommerce terminology; the old word produces types that read as foreign and searches that find nothing.
- Read `.ai/glossary/renamed-terms.md` before naming anything — Brand not Manufacturer, Page not Topic, Customer group not Customer role, Merchandise return not Return request, Loyalty points not Reward points, User field not Generic attribute.
- Use `.ai/glossary/entity-model.md` for base entity types, marker interfaces, user fields, localized properties, and slugs.
- Use `.ai/glossary/catalog.md` for products, product types, category/brand/collection, product vs specification attributes, pricing, and inventory.
- Use `.ai/glossary/sales.md` for cart, order, the three order statuses, payment transactions, shipping, merchandise returns, discounts, and loyalty points.
- Use `.ai/glossary/customers.md` for customers, groups, tags, vendors, sales employees, affiliates, and the four party boundaries.
- Use `.ai/glossary/platform.md` for stores, localization, settings, permissions, SEO, CMS content, media, messaging, and tasks.

## Global Rules
- Read `.ai/principles.md` for the reasoning behind the codebase's shape; use it when two valid approaches conflict.
- Read `.ai/constraints.md` for hard prohibitions. A violation is a defect, not a trade-off to weigh.

## Standards Routing
Standards are binding. When a standard and the closest existing file disagree, follow the existing file and say so.
- Use `.ai/standards/naming.md` for project, type, file, key, and member naming, including plugin system names, setting keys, localization keys, and cache key constants.
- Use `.ai/standards/csharp-style.md` for formatting enforced by `.editorconfig`, file layout, constructor injection, guards, and what not to introduce.
- Use `.ai/standards/razor-frontend.md` for Razor conventions, Vue-in-Razor rules, storefront data attributes, admin tag helpers, and asset placement.
- Use `.ai/standards/git-and-pr.md` for branches, commit format, the pull request template, and the pre-PR checklist.
- Use `.ai/standards/dependencies.md` for central package management, shared MSBuild props, project references, output paths, and SDK selection.

## Checklist Routing
Skills carry their own domain checklists; these cover what no single skill owns.
- Run `.ai/checklists/definition-of-done.md` on every change before reporting it complete.
- Run `.ai/checklists/code-review.md` when reviewing a diff, including your own before opening a PR.
- Run `.ai/checklists/security.md` when the change touches auth, input, scoped data, secrets, payments, or file handling.
- Run `.ai/checklists/performance.md` when the change adds a query, iterates entities, or touches a render path.
- Run `.ai/checklists/data-change.md` when the change touches entities, migrations, settings, resources, permissions, or persisted identities.
- Run `.ai/checklists/plugin-release.md` before shipping a plugin or theme.

## Examples and Templates
- Use `.ai/examples/cached-store-scoped-service.md` for the canonical business service: read-through cache, store scope, invalidation, entity events.
- Use `.ai/examples/payment-plugin-walkthrough.md` for a complete plugin, file by file, from manifest to admin configuration screen.
- Use `.ai/examples/theme-override-walkthrough.md` for how a theme overrides a subset of views and what falls through to defaults.
- Use `.ai/templates/plugin/base-plugin.md` for the files every installable plugin needs.
- Use `.ai/templates/plugin/admin-configuration.md` for a plugin's admin configuration screen.
- Use `.ai/templates/theme/theme-plugin.md` for a storefront theme skeleton.
- Use `.ai/templates/migration.md` for an upgrade migration skeleton.

Templates are the shape; skills are the contract. Read the skill first, then diff the scaffold against the closest shipped plugin in `src/Plugins/`.

## Operating Rules
1. Read the user's goal before inspecting files.
2. Load the matching prompt from `.ai/prompts/` or workflow from `.ai/workflows/`, plus the skills and standards it names.
3. Inspect existing repository patterns before proposing changes.
4. Use the domain vocabulary from `.ai/glossary/` when naming anything.
5. Keep changes limited to the requested scope.
6. Preserve user work and unrelated local changes.
7. Prefer existing abstractions, conventions, and test utilities.
8. Validate changes with the narrowest meaningful build or test command when execution is available.
9. Run the applicable checklists from `.ai/checklists/` before reporting completion.
10. Report commands that were run and any commands that could not be run.
11. Provide concrete file references for findings or changes.

## Constraints
`.ai/constraints.md` holds the full list of hard prohibitions. The ones that govern agent behavior itself:
- Never overwrite unrelated changes.
- Never invent requirements or repository conventions.
- Never broaden scope without a clear reason.
- Never leave generated, temporary, or diagnostic artifacts unless they are part of the requested output.
- Never report a change as verified when it was not.

## Expected Output
Return a concise result that states what changed, what was reviewed, what was validated, and what risk remains.
