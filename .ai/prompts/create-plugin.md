# Prompt: Create Plugin

## Purpose
Scaffold a new installable GrandNode plugin end to end, with the correct project shape, manifest, provider registration, and install/uninstall behavior.

## Inputs Required
- Repository root.
- Plugin kind: payment, shipping, tax, widget, external authentication, discount rule, exchange rate, or theme.
- System name, following `{Group}.{Name}` (e.g. `Payments.Adyen`, `Widgets.Chat`).
- Friendly name shown in the admin plugin list.
- Whether the plugin needs admin configuration, storefront UI, persisted settings, or its own data collection.

## Steps

1. Read `.ai/knowledge/plugin-types.md` for the inventory, structure, and manifest rules.
2. Read the skill for the plugin kind:
   - payment → `.ai/skills/plugin-payment.md`
   - shipping → `.ai/skills/plugin-shipping.md`
   - widget → `.ai/skills/plugin-widget.md`
   - discount rule → `.ai/skills/plugin-discount-rules.md`
   - theme → `.ai/skills/theme-creation.md`
   - anything else → `.ai/skills/plugin-module.md`
3. Read `.ai/templates/plugin/` and copy the skeleton files that apply. Read `.ai/examples/` for a worked end-to-end plugin.
4. Pick the closest existing plugin in `src/Plugins/` and diff your scaffold against it. State which one you used.
5. Create the project and add it to `GrandNode.sln`.
6. Wire up in this order:
   1. `.csproj` — SDK, `Grand.Common.props` import, output path, `Private=false` references.
   2. `Manifest.cs` — `[assembly: PluginInfo(...)]` with an existing `Group` value.
   3. `{Feature}Defaults.cs` — system name, friendly-name resource key, configuration URL.
   4. `{Feature}Settings.cs` — `ISettings` implementation, when settings are persisted.
   5. `{Feature}Provider.cs` — the provider interface for the plugin kind.
   6. `{Feature}Plugin.cs` — `BasePlugin`, `Install()` / `Uninstall()`.
   7. `StartupApplication.cs` — `IStartupApplication` registrations.
   8. `Areas/Admin/` controller + `Configure.cshtml`, when configurable.
   9. `Views/`, `Components/`, `Controllers/`, `EndpointProvider.cs`, when the plugin has storefront UI.
   10. `logo.jpg`.
7. Verify the build output lands in `src/Web/Grand.Web/Plugins/{SystemName}/`.
8. Run `.ai/prompts/review-change.md` on the result.

## Mandatory Rules

1. `SystemName` in `Manifest.cs` must equal the value in `{Feature}Defaults` and the output folder name.
2. `Group` must be one of the existing group names in `.ai/knowledge/plugin-types.md`.
3. Import `src/Build/Grand.Plugin.props` for the shared host references; do not repeat them in the plugin. Anything referenced beyond that set must be `Private="false"`.
4. Use `Microsoft.NET.Sdk.Razor` when the plugin contains `.cshtml` files, `Microsoft.NET.Sdk` otherwise.
5. `Install()` saves default settings and adds localization resources, then calls `base.Install()` last.
6. `Uninstall()` deletes settings and removes localization resources, then calls `base.Uninstall()` last.
7. Admin controllers carry `[AuthorizeAdmin]`, `[Area("Admin")]`, and the correct `[PermissionAuthorize(...)]`.
8. Add package references without versions — versions live in `Directory.Packages.props`.

## Output Format

- **Plugin**: system name, group, friendly name.
- **Reference plugin**: which existing plugin the scaffold follows.
- **Files created**: path + purpose.
- **Registration**: services registered and where.
- **Install/Uninstall**: settings and resource keys handled.
- **Validation**: build result and confirmed output path.
- **Remaining work**: what the author still has to fill in (credentials, API calls, views).
