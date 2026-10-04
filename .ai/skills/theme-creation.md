# Theme Creation

## Purpose
Create, modify, and review GrandNode storefront themes — plugins in the `Themes` group that register an `IThemeView` and override a subset of storefront Razor views.

## When To Use
Use this skill when building a new theme, adding or removing view overrides in an existing theme, changing theme-owned CSS/JS, changing the theme preview, or reviewing why a themed page renders the default view instead of the theme's.

## When Not To Use
Do not use this skill for changes to `Grand.Web/Views` themselves — that is `.ai/skills/template-creation.md`. Do not use it for widget plugins that inject markup into zones — that is `.ai/skills/plugin-widget.md`. Combine with `.ai/skills/plugin-module.md` for the plugin scaffolding a theme shares with every other plugin.

## Inputs Required
- Repository root.
- Theme name — used for `Theme.{Name}` system name, `IThemeView.ThemeName`, and the `Views/{Name}/` folder.
- Which pages the theme changes, and whether it is a full theme or a small override set.
- Whether the theme ships its own CSS/JS and whether the Vite bundle is affected.
- Whether the theme supports RTL.

## Instructions

### Mandatory Rules

#### Theme registration
1. Implement `IThemeView` from `Grand.Web.Common.Themes`:

   | Member | Requirement |
   |---|---|
   | `AreaName` | `""` for storefront themes. |
   | `ThemeName` | The theme's display key. **Must match the folder name under `Views/`.** |
   | `ThemeInfo` | `record ThemeInfo(string Title, string PreviewImageUrl, string PreviewText, bool SupportRtl)`. |
   | `GetViewLocations()` | Ordered list of view-location format strings. |

2. Register it as scoped in the theme's `IStartupApplication`:
   ```csharp
   services.AddScoped<IThemeView, ModernThemeView>();
   ```
3. `GetViewLocations()` must place theme locations first and end with the default fallbacks, so uncopied views still resolve:
   ```csharp
   return new List<string> {
       "/Views/Modern/{1}/{0}.cshtml",
       "/Views/Modern/Shared/{0}.cshtml",
       "/Views/{1}/{0}.cshtml",
       "/Views/Shared/{0}.cshtml"
   };
   ```
   `{0}` is the view name, `{1}` is the controller name. Dropping the last two entries makes every view the theme has not copied fail to resolve.
4. `ThemeInfo.PreviewImageUrl` must point at a real file, by convention `~/Plugins/Theme.{Name}/Content/theme.jpg`.
5. Set `SupportRtl` honestly — it drives which themes the admin offers for RTL stores.

#### Plugin scaffolding
6. `Manifest.cs` with `Group = "Themes"` and `SystemName = "Theme.{Name}"`.
7. A `BasePlugin, IPlugin` class. A theme with no settings needs no more than a declaration:
   ```csharp
   public class ModernThemePlugin : BasePlugin, IPlugin;
   ```
8. `Install()` / `Uninstall()` are only needed when the theme persists settings or resource keys. If overridden, call `base` last.

#### Project file
9. SDK `Microsoft.NET.Sdk.Razor`, importing `..\..\Build\Grand.Common.props`.
10. Set both properties — omitting either breaks view compilation or leaks static web assets into the host:
    ```xml
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>
    ```
11. Set the output path for **both** Debug and Release to `..\..\Web\Grand.Web\Plugins\Theme.{Name}\`. A missing Release path means the theme vanishes from release builds.
12. Reference GrandNode projects with `<Private>false</Private>`. `Grand.Web` and `Grand.Web.Common` additionally need `<ExcludeAssets>all</ExcludeAssets>` — a theme references them for compilation only.
13. Copy content with `PreserveNewest`:
    ```xml
    <None Update="Content\**\*.*"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>
    <None Update="logo.jpg"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>
    ```

#### Views
14. Theme views live under `Views/{ThemeName}/`, mirroring the `Grand.Web/Views` structure: `Views/{ThemeName}/{Controller}/{Action}.cshtml` and `Views/{ThemeName}/Shared/`.
15. Add `Views/{ThemeName}/_ViewImports.cshtml` — the theme does **not** inherit `Grand.Web`'s. It must include:
    ```cshtml
    @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
    @removeTagHelper Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper, Microsoft.AspNetCore.Mvc.TagHelpers
    @addTagHelper *, Grand.Web.Common
    @addTagHelper *, Grand.Web
    @* … @using for Grand.Web.Models.*, Grand.Domain.*, Grand.Web.Common.* … *@
    @inject LocService Loc
    ```
    The `@removeTagHelper` line is required — without it the default `InputTagHelper` renders duplicate checkboxes.
16. Add `Views/{ThemeName}/_ViewStart.cshtml`.
17. Copy **only** the views the theme actually changes. Everything else resolves through the fallback locations. Each copied view is a file that must be re-reconciled on every upgrade.
18. A copied view keeps the same `@model`, the same route names, and the same storefront data attributes — `data-cart-action`, product IDs, quick-view URLs, wishlist, compare, add-to-cart, image `alt` / `title` / `loading`.
19. Keep widget zones (`@await Component.InvokeAsync("Widget", new { widgetZone = "..." })`) present in copied views. Dropping a zone silently disables every installed widget on that page.
20. A theme must not change view models, controller actions, or route definitions. If a theme needs different data, the change belongs in `Grand.Web`, not the theme.

#### Assets
21. Theme-owned CSS, JS, images, and vendored libraries live under the theme's `Content/` folder, referenced as `~/Plugins/Theme.{Name}/Content/...`.
22. `Content/theme.jpg` is the admin preview image; `logo.jpg` is the plugin-list logo. Both are required.
23. When the theme changes bundled frontend source, follow `.ai/skills/frontend-bundle-workflow.md` — rebuild and commit the bundle with the source.
24. Do not reference external CDNs.

### Recommendations
1. Use `src/Plugins/Theme.Modern/` as the reference implementation for every structural question.
2. Start from an override set — copy `Shared/_Root.cshtml` or a layout plus the handful of pages that differ — before considering a full theme.
3. Keep a note in the PR of which views were copied and at which upstream revision, so upgrades can diff them.
4. Prefer CSS overrides in `Content/css/` over copying a view just to change a class.
5. Group theme CSS by area (`common/`, `header/`, `catalog/`, `product/`, `cart/`, `customer/`) as `Theme.Modern` does, rather than one large file.

## Key Contracts

### IThemeView
```csharp
public interface IThemeView
{
    string AreaName { get; }
    string ThemeName { get; }
    ThemeInfo ThemeInfo { get; }
    IEnumerable<string> GetViewLocations();
}

public record ThemeInfo(string Title, string PreviewImageUrl, string PreviewText, bool SupportRtl);
```

### Reference implementation
```csharp
public class ModernThemeView : IThemeView
{
    public string AreaName => "";
    public string ThemeName => "Modern";

    public ThemeInfo ThemeInfo => new("Modern theme (beta)",
        "~/Plugins/Theme.Modern/Content/theme.jpg", "Minimal theme (beta)", false);

    public IEnumerable<string> GetViewLocations()
    {
        return new List<string> {
            "/Views/Modern/{1}/{0}.cshtml",
            "/Views/Modern/Shared/{0}.cshtml",
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        };
    }
}
```

## File Locations

| Concern | Path |
|---|---|
| `IThemeView` | `src/Web/Grand.Web.Common/Themes/IThemeView.cs` |
| Default theme view | `src/Web/Grand.Web.Common/Themes/DefaultThemeView.cs` |
| Theme context | `src/Web/Grand.Web.Common/Themes/ThemeContext.cs`, `ThemeContextFactory.cs` |
| Reference theme | `src/Plugins/Theme.Modern/` |
| Theme skeleton | `.ai/templates/theme/` |
| Default storefront views | `src/Web/Grand.Web/Views/` |

## Validation Checklist
- [ ] `ThemeName` equals the `Views/` subfolder name.
- [ ] `GetViewLocations()` ends with `/Views/{1}/{0}.cshtml` and `/Views/Shared/{0}.cshtml`.
- [ ] `IThemeView` registered with `AddScoped` in the theme's `IStartupApplication`.
- [ ] `Manifest.cs` uses `Group = "Themes"` and a `SystemName` matching the output folder.
- [ ] Output path set for both Debug **and** Release.
- [ ] `AddRazorSupportForMvc=true` and `StaticWebAssetsEnabled=false`.
- [ ] `_ViewImports.cshtml` present, including the `@removeTagHelper` for `InputTagHelper`.
- [ ] `_ViewStart.cshtml` present.
- [ ] `logo.jpg` and `Content/theme.jpg` exist and are copied to output.
- [ ] Copied views keep their `@model`, widget zones, and storefront data attributes.
- [ ] No view model, route, or controller changed from inside the theme.
- [ ] Build output present under `src/Web/Grand.Web/Plugins/Theme.{Name}/`.

## Common Failures

| Symptom | Cause |
|---|---|
| Theme selected, but pages render default markup | `ThemeName` does not match the `Views/` folder name, or `IThemeView` not registered |
| Some pages 500 with "view not found" | Fallback locations missing from `GetViewLocations()` |
| Duplicate checkboxes on themed forms | `@removeTagHelper … InputTagHelper` missing from the theme's `_ViewImports.cshtml` |
| Tag helpers unresolved in theme views | Theme has no `_ViewImports.cshtml`; it does not inherit `Grand.Web`'s |
| Theme missing in release deployments | Output path set only for the Debug configuration |
| Widgets disappear on a themed page | Widget zone dropped from the copied view |
| Theme preview blank in admin | `ThemeInfo.PreviewImageUrl` points at a missing or uncopied file |
| Theme CSS 404s | `Content/**` not marked `CopyToOutputDirectory` |
