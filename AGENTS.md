# Repository Agent Instructions

## Purpose
Provide model-agnostic operating instructions for AI agents working in this repository.

## When To Use
Use this file before making or reviewing repository changes. Use the skills in `.ai/skills/` when the task matches their domain. Use `.ai/knowledge/` for shared context and coding rules. Use `.ai/prompts/review-change.md` to run a structured review against all applicable skills.

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
- Use `.ai/knowledge/tests.md` for MSTest + Moq patterns, test structure, validator testing, and controller test setup.
- Use `.ai/knowledge/dotnet.md` for C# idioms: records, guard clauses, result objects, pattern matching, nullable types, and configuration binding.
- Use multiple skills when a change crosses domains.

## Operating Rules
1. Read the user's goal before inspecting files.
2. Inspect existing repository patterns before proposing changes.
3. Keep changes limited to the requested scope.
4. Preserve user work and unrelated local changes.
5. Prefer existing abstractions, conventions, and test utilities.
6. Validate changes with the narrowest meaningful build or test command when execution is available.
7. Report commands that were run and any commands that could not be run.
8. Provide concrete file references for findings or changes.

## Constraints
- Never overwrite unrelated changes.
- Never invent requirements or repository conventions.
- Never broaden scope without a clear reason.
- Never leave generated, temporary, or diagnostic artifacts unless they are part of the requested output.

## Expected Output
Return a concise result that states what changed, what was reviewed, what was validated, and what risk remains.
