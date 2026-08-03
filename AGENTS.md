# Repository Agent Instructions

## Purpose
Provide model-agnostic operating instructions for AI agents working in this repository.

## When To Use
Use this file before making or reviewing repository changes. Use the review skills in `skills/` when the task matches their domain.

## Skill Routing
- Use `skills/reviews/architecture-review/SKILL.md` for design, layering, dependency, module boundary, public contract, and maintainability reviews.
- Use `skills/reviews/security-review/SKILL.md` for authentication, authorization, input handling, secrets, cryptography, sensitive data, and trust-boundary reviews.
- Use `skills/reviews/dotnet-review/SKILL.md` for C#, .NET, ASP.NET Core, MongoDB repository usage, NuGet, MSBuild, and .NET test reviews.
- Use `skills/reviews/database-review/SKILL.md` for migrations, schema, indexes, queries, transactions, ORM mappings, and data integrity reviews.
- Use `skills/reviews/mongodb-review/SKILL.md` for MongoDB collections, filters, projections, indexes, repository queries, aggregations, updates, and data migrations.
- Use `skills/plugins/plugin-module/SKILL.md` for GrandNode plugins and modules, including provider plugins, themes, API modules, migrations, and installer behavior.
- Use `skills/plugins/plugin-shipping/SKILL.md` for shipping rate calculation plugins (IShippingRateCalculationProvider), GetShippingOptions, ShippingOption, ShippingRateCalculationType, and IShipmentTracker.
- Use `skills/plugins/plugin-payment/SKILL.md` for payment method plugins (IPaymentProvider), Standard vs Redirection flow, ProcessPayment, Capture, Refund, Void, and PaymentTransaction status.
- Use `skills/plugins/plugin-widget/SKILL.md` for widget plugins (IWidgetProvider), GetWidgetZones, view components, widget zone names, and GDPR consent gating.
- Use `skills/plugins/plugin-discount-rules/SKILL.md` for discount rule plugins (IDiscountProvider, IDiscountRule), CheckRequirement, DiscountRule.Metadata, and rule configuration controllers.
- Use `skills/template-creation/SKILL.md` for Razor views, layouts, partials, view components, plugin views, theme overrides, Vue-in-Razor templates, PDF templates, and DotLiquid message templates.
- Use `skills/frontend-bundle-workflow/SKILL.md` for Vue/Vite build, theme CSS changes, when to run `npm run build`, bundle output files, and committing bundles alongside source.
- Use `skills/admin-area-changes/SKILL.md` for admin-facing changes that may affect Main Admin, Store Owner, Vendor, shared admin models, permissions, navigation, validation, or scoped data access.
- Use `skills/project-structure/SKILL.md` to understand repository structure, technology ownership, layer responsibilities, and how to expand GrandNode consistently.
- Use `skills/settings-and-localization/SKILL.md` for settings classes, store-scoped overrides, ISettingService, localization resources, ITranslationService, IPluginTranslateResource, localized domain entities, and localized admin models.
- Use `skills/message-notification/SKILL.md` for message templates, DotLiquid tokens and drops, IMessageProviderService, queued email lifecycle, LiquidObjectBuilder, MessageTokensAddedEvent plugin extension, and domain event notification handlers.
- Use `skills/scheduled-task/SKILL.md` for scheduled task classes, IScheduleTask, AddKeyedScoped registration, ScheduleTask seed, multi-instance distributed locking, error handling, and task migrations.
- Use `skills/permission-navigation/SKILL.md` for permissions, PermissionSystemName, PermissionActionName, StandardPermission, PermissionProvider, controller authorization attributes, AdminSiteMap entries, and permission or navigation migrations.
- Use `skills/best-practices/async.md` for async/await patterns, CancellationToken, Task vs ValueTask, and blocking anti-patterns.
- Use `skills/best-practices/mongodb.md` for IRepository<T> usage, query patterns, partial updates, and write conventions.
- Use `skills/best-practices/performance.md` for ICacheBase, CacheKey constants, cache invalidation, pagination, and partial field writes.
- Use `skills/best-practices/security.md` for authorization checks, FluentValidation patterns, guard clauses, HTML encoding, and safe MongoDB queries.
- Use `skills/best-practices/architecture.md` for layering rules, DI lifetimes, MediatR commands/queries, and domain events.
- Use `skills/best-practices/tests.md` for MSTest + Moq patterns, test structure, validator testing, and controller test setup.
- Use `skills/best-practices/dotnet.md` for C# idioms: records, guard clauses, result objects, pattern matching, nullable types, and configuration binding.
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
