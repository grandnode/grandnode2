# Prompt: Create Theme

## Purpose
Create a new storefront theme plugin, or override a subset of views in an existing theme, without forking the whole view tree.

## Inputs Required
- Repository root.
- Theme name (used as `Theme.{Name}` system name and as the view-location folder name).
- Whether the theme is a full theme or a small override on top of the default views.
- Which pages or components the theme changes.
- Whether the theme ships its own CSS/JS, and whether it needs the Vite build.

## Steps

1. Read `.ai/skills/theme-creation.md` — it is the authority for `IThemeView`, view-location fallback, and asset layout.
2. Read `.ai/knowledge/template-types.md` for the view conventions of the area you are overriding.
3. Read `.ai/templates/theme/` for the skeleton files.
4. Inspect `src/Plugins/Theme.Modern/` as the reference implementation.
5. Create the project:
   1. `Theme.{Name}.csproj` — `Microsoft.NET.Sdk.Razor`, `AddRazorSupportForMvc`, `StaticWebAssetsEnabled=false`, output to `..\..\Web\Grand.Web\Plugins\Theme.{Name}\`.
   2. `Manifest.cs` — `Group = "Themes"`.
   3. `{Name}ThemePlugin.cs` — `BasePlugin, IPlugin`.
   4. `{Name}ThemeView.cs` — `IThemeView` with the view-location list.
   5. `StartupApplication.cs` — `services.AddScoped<IThemeView, {Name}ThemeView>()`.
   6. `Views/{Name}/_ViewImports.cshtml` and `_ViewStart.cshtml`.
   7. `Content/` with CSS, scripts, images, and `theme.jpg` preview.
   8. `logo.jpg`.
6. Copy **only** the views the theme actually changes into `Views/{Name}/`. The fallback locations resolve everything else from `Grand.Web`.
7. Confirm each copied view still binds the same `@model` and keeps the storefront data attributes described in `.ai/knowledge/template-types.md`.
8. If the theme changes bundled frontend assets, follow `.ai/skills/frontend-bundle-workflow.md` and commit the generated bundle alongside the source.
9. Build and confirm `Views/` and `Content/` land under `src/Web/Grand.Web/Plugins/Theme.{Name}/`.

## Mandatory Rules

1. `IThemeView.ThemeName` must match the folder name under `Views/`.
2. `GetViewLocations()` must end with the default fallbacks `"/Views/{1}/{0}.cshtml"` and `"/Views/Shared/{0}.cshtml"`, so uncopied views still resolve.
3. `AreaName` is `""` for storefront themes.
4. `ThemeInfo.PreviewImageUrl` must point at a file that exists under `~/Plugins/Theme.{Name}/Content/`.
5. Do not copy the whole `Grand.Web/Views` tree — every copied view is a file that must be maintained against upstream changes.
6. Do not change view models, route names, or controller contracts from inside a theme.
7. Preserve `data-cart-action`, product IDs, quick-view URLs, wishlist, compare, and add-to-cart attributes in copied views.
8. Keep `Content/**` and `logo.jpg` on `CopyToOutputDirectory=PreserveNewest`.

## Output Format

- **Theme**: system name, theme name, full theme or override.
- **View locations**: the `GetViewLocations()` list, with a note on what falls back to default.
- **Views copied**: each path + what changed in it.
- **Assets**: CSS/JS files added and whether a bundle rebuild was required.
- **Validation**: build result and confirmed output path.
- **Upstream risk**: which copied views are most likely to drift from `Grand.Web` on upgrade.
