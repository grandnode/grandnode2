# Plugin Module

## Purpose
Create, modify, and review GrandNode plugins and modules using the repository's existing extension patterns.

## When To Use
Use this skill when working in `src/Plugins`, `src/Modules`, plugin manifests, provider implementations, plugin settings, plugin install or uninstall logic, plugin controllers, plugin views, module startup, module output paths, or extension-point registrations.

Use this skill when asked to create a new payment, shipping, tax, widget, authentication, discount rule, exchange rate, theme, API, migration, installer, or scheduled task extension.

## When Not To Use
Do not use this skill for ordinary core, business, domain, or web changes that do not add or modify plugins or modules.

Do not use this skill as the primary review for payment correctness, tenant isolation, MongoDB query safety, or security-sensitive behavior; combine it with the relevant skill when those concerns are present.

## Inputs Required
- Repository root.
- Target extension type: plugin or module.
- Existing extension to use as the closest template.
- Desired system name, friendly name, group, and feature scope.
- Required provider interface or module startup behavior.
- Admin or store configuration requirements.
- Settings, localization resources, static assets, views, and tests required by the change.

## Instructions

### Mandatory Rules
1. Identify whether the work is a plugin under `src/Plugins` or a module under `src/Modules`.
2. Identify the closest existing implementation and follow its folder, namespace, project, startup, controller, view, setting, and test patterns.
3. Read `references/plugin-types.md` before creating or changing a plugin.
4. Read `references/module-types.md` before creating or changing a module.
5. Use a stable system name that matches the repository convention, such as `Payments.X`, `Shipping.X`, `Tax.X`, `Widgets.X`, `Authentication.X`, `DiscountRules.X`, `ExchangeRate.X`, or `Theme.X`.
6. Add or update `Manifest.cs` for plugins with `[assembly: PluginInfo(...)]`.
7. Add or update a plugin class that derives from `BasePlugin` when install, uninstall, or configuration behavior is required.
8. Register provider and service implementations in `StartupApplication : IStartupApplication`.
9. Keep `Priority`, `BeforeConfigure`, and `Configure` behavior consistent with nearby extensions unless the feature requires a different startup order.
10. Put plugin build output under `src/Web/Grand.Web/Plugins/{SystemName}` and module build output under `src/Web/Grand.Web/Modules/{ModuleName}`.
11. Set project references to shared GrandNode projects with `Private=false` or the existing local casing and runtime exclusion pattern used by the closest template.
12. Add settings classes only when values must be persisted or configured.
13. Add install logic for default settings and localization resources.
14. Add uninstall logic that removes plugin-owned settings and localization resources.
15. Add admin or store controllers only when configuration or interaction is required.
16. Use `BaseAdminPluginController`, `BasePluginController`, or the existing module controller base that matches the nearest implementation.
17. Add Razor views, `_ViewImports.cshtml`, components, static assets, and route providers only when the extension needs UI.
18. Ensure plugin provider `SystemName` equals the default constant and manifest system name.
19. Ensure provider `ConfigurationUrl`, `FriendlyName`, `Priority`, `LimitedToStores`, and `LimitedToGroups` follow the provider contract.
20. Add tests or update existing tests for provider behavior, startup registration, install or uninstall effects, and domain-specific behavior.
21. Build the specific plugin or module project when execution is available.
22. State any build or test command that was not run.

### Recommendations
1. Prefer copying the nearest existing extension shape before inventing a new structure.
2. Prefer constants in a `Defaults` class for provider system name, friendly name resource key, configuration URL, route names, and asset paths.
3. Prefer `ISettingService` for plugin settings and `IPluginTranslateResource` for plugin-owned localization resources.
4. Prefer store-specific settings only when the existing service resolves store overrides.
5. Prefer small provider methods that delegate complex behavior to plugin services.
6. Prefer adding a new reference file only when a new extension type appears in the repository.

## Constraints
- Never create a plugin or module outside `src/Plugins` or `src/Modules`.
- Never use a system name that conflicts with an existing plugin or provider.
- Never register a provider without a matching provider interface expected by the target subsystem.
- Never put plugin-owned binaries, views, or assets directly into `Grand.Web` source folders.
- Never skip install or uninstall cleanup for settings and localization resources owned by the plugin.
- Never hardcode secrets, API keys, tokens, callback URLs, or environment-specific connection strings.
- Never introduce a package version outside central package management unless the repository pattern explicitly requires it.
- Never change shared build props to make one plugin work unless the change is necessary for all plugins or modules.

## Expected Output
Produce one of these results:
- A new or modified plugin or module that follows GrandNode conventions.
- A review report with findings ordered by severity.
- A creation plan when implementation details are missing and cannot be inferred safely.

Include changed files, selected template, extension type, validation commands, and remaining risks.

## Validation Checklist
- [ ] The correct plugin or module type was selected.
- [ ] The closest existing template was identified.
- [ ] Manifest, system name, defaults, provider, startup, settings, and plugin class are consistent.
- [ ] Build output path targets `src/Web/Grand.Web/Plugins` or `src/Web/Grand.Web/Modules`.
- [ ] Provider interface registration is present and scoped correctly.
- [ ] Install and uninstall handle plugin-owned settings and localization resources.
- [ ] Controllers, routes, views, components, and assets are present only when needed.
- [ ] Store, vendor, group, language, currency, and permission scope were checked where relevant.
- [ ] Domain-specific behavior was checked with the matching review skill when needed.
- [ ] The narrowest relevant build or test command was run or reported as not run.

## Examples

### Example 1: Payment Plugin
Input: Create a redirect payment plugin named `Payments.ExamplePay`.

Output: Create `src/Plugins/Payments.ExamplePay`, add `Manifest.cs`, `ExamplePayDefaults.cs`, `ExamplePayPaymentPlugin`, `ExamplePayPaymentProvider : IPaymentProvider`, `StartupApplication`, settings, configuration controller and view, logo asset, project output path, install and uninstall resources, and payment provider tests.

### Example 2: Widget Plugin
Input: Add a public tracking widget.

Output: Create a widget provider implementing `IWidgetProvider`, register it in startup, define widget zones, add a view component and view under the plugin, add settings and consent behavior when needed, and validate rendering in the selected zone.

### Example 3: Scheduled Task Module Change
Input: Add a task that recalculates stale catalog data.

Output: Add an `IScheduleTask` implementation under `Grand.Module.ScheduledTasks`, register it with `AddKeyedScoped<IScheduleTask, TaskType>("Task name")`, keep the module output path unchanged, and add tests for idempotent task execution.

