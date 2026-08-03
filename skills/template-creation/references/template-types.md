# GrandNode Template Types

## Public Storefront Razor Views
Use for customer-facing pages under:
- `src/Web/Grand.Web/Views`
- `src/Web/Grand.Web/Views/Shared`
- `src/Web/Grand.Web/Views/Shared/Partials`
- `src/Web/Grand.Web/Views/Shared/Components`

Follow existing storefront patterns:
- Use `@model` where nearby views use strongly typed models.
- Use `Loc[...]` or `@Loc[...]` for localized text.
- Use route names such as `Url.RouteUrl(...)` when nearby templates use named routes.
- Preserve `data-cart-action`, product IDs, quick view URLs, wishlist, compare, and add-to-cart attributes.
- Preserve image `alt`, `title`, `loading`, and priority behavior.
- Keep widget zones through `Component.InvokeAsync("Widget", ...)` where they already exist.

## Admin Views
Use for admin pages under:
- `src/Web/Grand.Web.Admin/Areas/Admin/Views`
- `src/Web/Grand.Web.AdminShared`
- plugin `Areas/Admin/Views`

Follow existing admin patterns:
- Use admin tag helpers for labels, inputs, validation, cards, tabs, and grids.
- Use existing Kendo grid and AJAX conventions.
- Call `addAntiForgeryToken(data)` for AJAX mutations when nearby views do.
- Preserve permission assumptions from controllers and surrounding templates.
- Keep partial names like `CreateOrUpdate.TabInfo.cshtml`, `CreateOrUpdate.cshtml`, and popup views when nearby views use them.

## Store Area Views
Use for store management pages under:
- `src/Web/Grand.Web.Store/Areas/Store/Views`

Follow existing store-area patterns:
- Keep store-specific permissions and scope visible in forms and grids.
- Reuse partial names and tab structure from nearby entities.
- Preserve store-scoped service behavior by not adding client-only filtering.

## Vendor Area Views
Use for vendor pages under:
- `src/Web/Grand.Web.Vendor/Areas/Vendor/Views`

Follow existing vendor patterns:
- Preserve vendor-specific layout and navigation partials.
- Do not expose admin-only actions.
- Keep vendor data filtered by vendor-owned records.
- Use vendor components under `Views/Shared/Components` when the UI is reusable.

## Plugin Views
Use for plugin-owned UI under:
- `src/Plugins/{SystemName}/Views`
- `src/Plugins/{SystemName}/Areas/Admin/Views`
- `src/Plugins/{SystemName}/Areas/Store/Views`

Follow existing plugin patterns:
- Add `_ViewImports.cshtml` where the plugin area requires tag helpers or namespaces.
- Use `BaseAdminPluginController` or the existing plugin controller base for admin views.
- Keep plugin configuration views under the plugin folder.
- Ensure the plugin project uses Razor support when views are present.
- Ensure plugin project output copies views and assets into `src/Web/Grand.Web/Plugins/{SystemName}`.
- Add or update plugin localization resources during install and uninstall.

## Theme Views
Use for theme-specific storefront overrides under:
- `src/Plugins/Theme.Modern/Views/Modern`

Follow existing theme patterns:
- Keep `_ViewStart.cshtml` and `_ViewImports.cshtml` behavior intact.
- Match base storefront model types.
- Override only the views needed by the theme.
- Keep theme assets under the theme plugin `Content` folder.
- Preserve shared component view paths such as `Views/Modern/Shared/Components/{Component}/Default.cshtml`.

## View Components
Use when markup needs independently loaded data or existing component extension points.

Follow existing component patterns:
- Component class lives near the owning web or plugin project.
- Default view path is `Views/Shared/Components/{ComponentName}/Default.cshtml` or the plugin equivalent.
- Use view components for widgets, navigation blocks, product blocks, footer, menus, and externally supplied provider UI.
- Keep component names aligned with provider methods such as `GetPublicViewComponentName`.

## Razor Partials
Use for reusable fragments under `Partials` folders.

Follow existing partial patterns:
- Name private layout fragments with a leading underscore when nearby views do.
- Name entity edit tabs as `CreateOrUpdate.Tab{Name}.cshtml`.
- Pass explicit models to `<partial name="..." model="..."/>`.
- Avoid hidden dependencies on parent `ViewData` unless nearby partials use the same convention.

## Vue-In-Razor Templates
Use for in-page Vue component templates embedded in `.cshtml`.

Follow existing patterns:
- Use `<script type="text/html" id="...">` for template markup.
- Use `<script type="application/json" data-grand-vm="inDomComponents">` for component registration.
- Escape Razor/Vue collisions correctly, including `@@click` when Razor would parse `@click`.
- Preserve Vue props, component names, and model shape.
- Keep generated bundle requirements in sync with `src/Web/Grand.Web/vueapp`.

## PDF Templates
Use for PDF views under:
- `src/Web/Grand.Web/Views/PdfTemplates`

Follow existing PDF patterns:
- Keep markup simple and deterministic.
- Avoid interactive JavaScript.
- Use inline or existing PDF-compatible styling.
- Preserve order, shipment, address, tax, and currency formatting.

## DotLiquid Message Templates
Use for email and notification templates stored as `MessageTemplate` data.

Relevant files:
- `src/Modules/Grand.Module.Installer/Services/InstallDataMessageTemplates.cs`
- `src/Business/Grand.Business.Messages/Services/MessageTemplateNames.cs`
- `src/Business/Grand.Business.Messages/Services/MessageTokenProvider.cs`
- `src/Business/Grand.Business.Core/Utilities/Messages/DotLiquidDrops`

Follow existing message patterns:
- Use names like `OrderPlaced.CustomerNotification` or `Service.ContactUs`.
- Use `{{Token.Property}}` for values.
- Use `{% if ... %}` and `{% for ... %}` for conditions and loops.
- Use only tokens exposed by `Liquid*` drop classes.
- Add a constant in `MessageTemplateNames` when sending code refers to the template.
- Add seed data in installer when a template must exist on new installations.
- Preserve `IsActive`, `EmailAccountId`, store limitations, and localized template behavior.
- Avoid including sensitive data that the recipient should not receive.

## Frontend Source Templates
Use for Vue and theme asset source under:
- `src/Web/Grand.Web/vueapp`
- `src/Web/Grand.Web/wwwroot/theme/css`

Follow existing frontend patterns:
- Run `npm run build` from `src/Web/Grand.Web/vueapp` when source changes require regenerated committed bundles.
- Commit regenerated files under `src/Web/Grand.Web/wwwroot/bundles` when required by the build output.
- Run `npm run lint` when JavaScript or Vue source changes are significant.

