# Plugin — Widget Provider

## Purpose
Create, modify, and review GrandNode widget plugins that implement `IWidgetProvider` and inject rendered output into storefront zone slots.

## When To Use
Use this skill when building a new widget plugin (tracking pixel, banner, slider, chat, analytics), changing which zones a widget targets, adding a new view component, reviewing widget consent behavior, or wiring a widget to a CMS data source.

## When Not To Use
Do not use this skill for admin-only functionality with no storefront output, or for general plugin infrastructure; combine with `plugin-module` when foundational setup is needed.

## Inputs Required
- Repository root.
- Which widget zones the widget should render in.
- Whether the widget needs admin configuration or CMS data.
- Whether consent (GDPR/cookie) gating is required.
- Whether context data from the zone (e.g., product ID, category ID) is needed.

## Instructions

### Mandatory Rules

#### Provider Interface
1. Implement `IWidgetProvider` from `Grand.Business.Core.Interfaces.Cms`. This extends `IProvider` requiring `ConfigurationUrl`, `SystemName`, `FriendlyName`, `Priority`, `LimitedToStores`, `LimitedToGroups`.
2. Implement both `IWidgetProvider` members:

| Member | Requirement |
|---|---|
| `GetWidgetZones()` | Return the list of zone names where this widget renders. Use existing zone name constants from `Defaults` or string literals matching the zones used in storefront views. |
| `GetPublicViewComponentName(widgetZone)` | Return the view component name to invoke for the given zone. Can return different component names per zone, or the same name for all zones. |

3. Resolve `FriendlyName` through `ITranslationService.GetResource(Defaults.FriendlyName)`.
4. Resolve `Priority` from settings `DisplayOrder`.

#### View Component
5. Create a view component class with `[ViewComponent(Name = "YourComponentName")]` inheriting `ViewComponent`.
6. Implement `InvokeAsync(string widgetZone, object additionalData = null)`:
   - Use `widgetZone` when the component handles multiple zones differently.
   - Cast `additionalData` to the expected zone data type when context is needed (e.g., product ID for product page zones).
   - Return `Content("")` or `View(emptyModel)` rather than returning `null` when nothing should render.
7. Place the view component class in the plugin's `Components/` folder.

#### Views
8. Place the default view at `Views/Shared/Components/{ViewComponentName}/Default.cshtml`.
9. Add `_ViewImports.cshtml` under `Views/` with the plugin's namespace, common tag helpers, and `@inject LocService Loc` when localization is used:
   ```cshtml
   @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
   @addTagHelper *, Grand.Web.Common
   @addTagHelper *, Grand.Web
   @using YourPlugin.Models
   @inject LocService Loc
   ```
10. Use `Microsoft.NET.Sdk.Razor` as the project SDK to enable view compilation and output.
11. Set output path to `..\..\Web\Grand.Web\Plugins\{SystemName}\` so views are deployed to the plugin output folder.

#### Consent / GDPR
12. When the widget injects third-party tracking scripts, check user consent via `ICookiePreference.IsEnable(consentCookieName, store, customer)` before rendering the script. Return `Content("")` when consent is denied.
13. Register the cookie consent name in plugin settings and expose it through the admin configuration page.

#### Project Structure
14. Mark all GrandNode project references `Private="false"`.
15. Include `logo.jpg` with `CopyToOutputDirectory = Always`.
16. Copy static assets (CSS, JS, images) owned by the plugin using `<Content CopyToOutputDirectory="Always">`.

#### Manifest, Defaults, Settings
17. Define `[assembly: PluginInfo(...)]` with `Group = "Widgets"`.
18. Define `{Plugin}Defaults` with `ProviderSystemName`, `FriendlyName` (resource key), `ConfigurationUrl`, and zone name constants when the plugin targets specific named zones.
19. Define `{Plugin}Settings : ISettings` with at minimum `DisplayOrder`. Add tracking IDs, container IDs, consent cookie names, or data source settings as needed.

#### Startup and Install
20. Register the provider and any supporting services:
    ```csharp
    services.AddScoped<IWidgetProvider, YourWidgetProvider>();
    services.AddScoped<IYourDataService, YourDataService>();
    ```
21. In `Install()`: save default settings, register localization keys. Call `base.Install()` last.
22. In `Uninstall()`: delete settings, remove resource keys. Call `base.Uninstall()` last.

#### Admin Configuration
23. Create an admin controller with `[AuthorizeAdmin]`, `[Area("Admin")]`, and `[PermissionAuthorize(PermissionSystemName.Widgets)]`.
24. Use `IAdminStoreService.GetActiveStore()`, `LoadSetting`, `SaveSetting`, `ClearCache` for store-scoped config.
25. For widgets with CMS-managed content (e.g., sliders), create full CRUD controllers and views under the plugin's `Areas/Admin/` folder.

### Recommendations
1. Prefer `Widgets.GoogleAnalytics` as a template for tracking-only widgets (no CMS data, single component, consent gating).
2. Prefer `Widgets.Slider` as a template for widgets with admin-managed content and multiple zones.
3. Prefer returning `Content("")` (empty) over rendering empty markup when no data is available.
4. Prefer loading zone-specific context from `additionalData` rather than re-querying by URL or route.
5. Prefer a single view component that handles multiple zones by switching on `widgetZone`, rather than creating separate components per zone.

## Common Widget Zones

| Zone name | Rendered in |
|---|---|
| `"home_page_top"` | Top of home page |
| `"home_page_bottom"` | Bottom of home page |
| `"category_page_top"` | Top of category listing |
| `"product_page_top"` | Top of product detail page |
| `"product_page_bottom"` | Bottom of product detail page |
| `"body_end_html_tag_before"` | Before `</body>` — analytics scripts |
| `"clean_body_end_html_tag_before"` | Before `</body>` on clean layout |
| `"collection_page_top"` / `"brand_page_top"` | Top of collection/brand page |
| `"order_summary_content_after"` | After order summary in checkout |
| `"checkout_completed_top"` | Order confirmation page — for conversion tracking |

Zone names are arbitrary strings agreed upon between the view and the provider. Check the target layout or view for the exact zone string used in `@await Component.InvokeAsync("Widget", new { widgetZone = "..." })`.

## Key Contracts

### IWidgetProvider
```csharp
Task<IList<string>> GetWidgetZones();
Task<string> GetPublicViewComponentName(string widgetZone);
```

### View component invocation from storefront views
```cshtml
@await Component.InvokeAsync("Widget", new { widgetZone = "home_page_top" })
```
The core `Widget` view component discovers all active `IWidgetProvider` plugins and invokes the returned component name for each.

### ICookiePreference (consent)
```csharp
Task<bool> IsEnable(string name, Store store, Customer customer);
```

## File Locations

| Concern | Path |
|---|---|
| IWidgetProvider | `src/Business/Grand.Business.Core/Interfaces/Cms/IWidgetProvider.cs` |
| ICookiePreference | `src/Business/Grand.Business.Core/Interfaces/Common/Security/ICookiePreference.cs` |
| Widget core component | `src/Web/Grand.Web/Components/Widget.cs` |
| Example — tracking pixel | `src/Plugins/Widgets.GoogleAnalytics/` |
| Example — tracking pixel 2 | `src/Plugins/Widgets.FacebookPixel/` |
| Example — CMS slider | `src/Plugins/Widgets.Slider/` |

## Validation Checklist
- [ ] `IWidgetProvider` and `IProvider` members all implemented.
- [ ] `GetWidgetZones()` returns the exact zone strings used in the target views.
- [ ] `GetPublicViewComponentName` returns a name matching the `[ViewComponent(Name = "...")]` attribute.
- [ ] View at `Views/Shared/Components/{Name}/Default.cshtml` exists.
- [ ] `_ViewImports.cshtml` present and includes required tag helpers and namespaces.
- [ ] Tracking widgets check consent before rendering scripts.
- [ ] Output path set to `Grand.Web/Plugins/{SystemName}/`.
- [ ] Provider registered with `AddScoped<IWidgetProvider, ...>`.
- [ ] `Install` and `Uninstall` handle settings and resource keys.
- [ ] Admin controller uses `[PermissionAuthorize(PermissionSystemName.Widgets)]`.

## Examples

### Example 1: Analytics Tracking Widget
Pattern from `Widgets.GoogleAnalytics`: implement single view component, return `"body_end_html_tag_before"` zone, check consent via `ICookiePreference`, render a script tag with the tracking ID from settings, return `Content("")` when consent denied or tracking ID not configured.

### Example 2: CMS Slider Widget
Pattern from `Widgets.Slider`: implement a database-backed slider service, return home/category/brand/collection zones, render slides from the database filtered by zone and store, provide full admin CRUD for slide management.

### Example 3: Context-Aware Widget
Widget that shows related links on a product page: receive `additionalData` as a product ID in the `product_page_bottom` zone, query related data for that product, render the widget with product-specific content.
