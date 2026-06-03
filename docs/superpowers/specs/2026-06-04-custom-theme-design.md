# Custom Theme: Runtime Branding Customizer

**Date:** 2026-06-04  
**Status:** Approved  

## Overview

A runtime branding customizer for GrandNode2 that allows store owners to configure colors and images (logo, favicon, banner) per store through a standalone admin UI. Customizations are stored in MongoDB via the existing `ISettingService` infrastructure and injected into the storefront as CSS custom properties — no code changes, no file generation, no cache invalidation work required.

## Background

GrandNode2 already has a plugin-based theme system (`IThemeView`, `Theme.Modern`). This feature does not replace that system; it adds a branding layer on top of the active theme. The `Theme.Modern` CSS is refactored to consume CSS custom properties with built-in defaults, so stores without branding settings see no visual change.

## Data Model

A new `BrandingSettings` class in `Grand.Domain.Stores`, following the same pattern as `StoreInformationSettings`. Persisted per-store via `ISettingService.SaveSettingAsync<T>(settings, storeId)` — no MongoDB migration needed.

| Property | Type | Purpose |
|---|---|---|
| `PrimaryColor` | `string` | Buttons, links, highlights |
| `SecondaryColor` | `string` | Hover states, secondary buttons |
| `AccentColor` | `string` | Badges, tags, calls to action |
| `BackgroundColor` | `string` | Page background |
| `TextColor` | `string` | Body text |
| `LogoUrl` | `string` | Uploaded logo image URL |
| `FaviconUrl` | `string` | Browser tab icon URL |
| `BannerUrl` | `string` | Homepage hero/banner image URL |

Image URLs reference the existing `IPictureService` / `IStorageService` storage — no new storage infrastructure.

## CSS Variable Injection

A new Razor partial `_BrandingStyles.cshtml` is added to `Grand.Web.Common` and included in the `<head>` of the `Theme.Modern` layout. It reads `BrandingSettings` for the current store (already cached by `ISettingService`) and outputs:

```html
<style>
  :root {
    --brand-primary: #2563eb;
    --brand-secondary: #1e40af;
    --brand-accent: #f59e0b;
    --brand-background: #ffffff;
    --brand-text: #111827;
  }
</style>
```

If a setting is empty/null, it is omitted from the output and the CSS fallback default applies.

Logo, favicon, and banner are rendered as tags in the same partial:
- Logo: `<img>` tag replacing the default layout logo
- Favicon: `<link rel="icon">` tag
- Banner: exposed as a CSS variable `--brand-banner-url: url(...)` for use in the theme CSS

### Theme.Modern CSS changes

All hardcoded color values in `Theme.Modern/Content/css/` are replaced with CSS custom property references and fallback defaults:

```css
/* Before */
.btn-primary { background-color: #2563eb; }

/* After */
.btn-primary { background-color: var(--brand-primary, #2563eb); }
```

This ensures the theme renders identically when no branding settings exist.

## Admin UI

### BrandingController

New controller `BrandingController` in `Grand.Web.Admin`:
- `GET /Admin/Branding` — loads `BrandingSettings` for the selected store, maps to `BrandingSettingsModel`, renders the view
- `POST /Admin/Branding` — maps posted model back to `BrandingSettings`, saves via `ISettingService`, redirects with success notification

Follows the exact same pattern as `SettingController.GeneralCommon()`.

### BrandingSettingsModel

View model in `Grand.Web.AdminShared.Models.Settings`:
- Color string properties matching `BrandingSettings`
- Image URL string properties
- `AvailableStores` for the store selector dropdown

### View: `Areas/Admin/Views/Branding/Index.cshtml`

Two panels:

**Colors panel:**
- Five `<input type="color">` pickers with labels
- Live preview strip showing the selected palette (colored boxes rendered via inline style)

**Images panel:**
- Three upload fields (Logo, Favicon, Banner) using the existing admin image uploader component
- Current image thumbnail displayed below each uploader when a URL is set

**Store selector:** Dropdown at the top of the page (same pattern as other per-store admin pages). Changing the store reloads the page with `?storeId=...`.

**Save:** Single "Save" button POSTs all fields. On success, `ISettingService` invalidates the in-memory cache for that store; the next storefront request picks up new values immediately.

### Admin Sidebar Navigation

New "Design" group added to the admin sidebar menu containing a single "Branding" link pointing to `/Admin/Branding`.

## Data Flow

```
Store Admin saves branding
    → POST /Admin/Branding
    → BrandingController saves BrandingSettings via ISettingService
    → ISettingService invalidates cache for storeId

Storefront page request
    → Layout renders _BrandingStyles.cshtml
    → _BrandingStyles reads BrandingSettings via ISettingService (from cache)
    → Outputs <style> block with --brand-* CSS variables
    → Theme.Modern CSS vars resolve to store's brand colors
```

## What Is Not In Scope

- Typography customization (font family, sizes)
- Layout options (header/footer variants)
- Custom CSS textarea
- Per-theme branding (branding applies to whichever theme is active)
- Customer-facing theme switcher changes

## Files To Create or Modify

| File | Action |
|---|---|
| `src/Core/Grand.Domain/Stores/BrandingSettings.cs` | Create |
| `src/Web/Grand.Web.AdminShared/Models/Settings/BrandingSettingsModel.cs` | Create |
| `src/Web/Grand.Web.Admin/Controllers/BrandingController.cs` | Create |
| `src/Web/Grand.Web.Admin/Areas/Admin/Views/Branding/Index.cshtml` | Create |
| `src/Web/Grand.Web.Common/Views/Shared/Partials/_BrandingStyles.cshtml` | Create |
| `src/Plugins/Theme.Modern/Views/Modern/Shared/_Layout.cshtml` (or equivalent layout) | Modify — include `_BrandingStyles` partial |
| `src/Plugins/Theme.Modern/Content/css/` | Modify — replace hardcoded colors with CSS vars |
| `src/Web/Grand.Web.Admin/Infrastructure/AdminMenuRegistrar.cs` (or equivalent menu builder) | Modify — add Design > Branding item; locate the exact file by searching for where other top-level admin menu groups are registered |
