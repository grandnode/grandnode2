# Example: Theme Override

Source: `src/Plugins/Theme.Modern/`

How a GrandNode theme replaces part of the storefront without forking it. Read `.ai/skills/theme-creation.md` for the contract and `.ai/templates/theme/theme-plugin.md` for the skeleton.

---

## Five files and a folder

```
Theme.Modern/
  Theme.Modern.csproj
  Manifest.cs                ← Group = "Themes"
  ModernThemePlugin.cs       ← one line
  ModernThemeView.cs         ← the whole mechanism
  StartupApplication.cs      ← one registration
  logo.jpg
  Content/{css,script,images,swiper}, theme.jpg
  Views/Modern/…             ← the overrides
```

The plugin class is a declaration and nothing more:

```csharp
public class MinimalThemePlugin : BasePlugin, IPlugin;
```

A theme with no settings and no resource keys needs no `Install()` or `Uninstall()` — `BasePlugin` already marks it installed. (The class name here does not match the file name `ModernThemePlugin.cs`; the type name is not load-bearing, but new code should keep them aligned per `.ai/standards/naming.md`.)

Registration is equally small:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IThemeView, ModernThemeView>();
}
```

## The mechanism: `GetViewLocations()`

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

`{0}` is the view name, `{1}` the controller name. Resolution walks the list in order and takes the first hit:

| Request | Resolves to |
|---|---|
| `Product/ProductTemplate.Simple` (theme has it) | `/Views/Modern/Product/ProductTemplate.Simple.cshtml` |
| `Vendor/List` (theme does not) | `/Views/Vendor/List.cshtml` — the default in `Grand.Web` |

The last two entries are the fallback. **Remove them and every page the theme has not copied fails with "view not found."** That single detail is what makes a theme an override set rather than a fork.

`ThemeName` is `"Modern"` and the folder is `Views/Modern/` — those must match, because the format strings hardcode the folder name.

## What "a subset" means in practice

`Theme.Modern` ships 110 `.cshtml` files against 235 in `Grand.Web/Views`:

| Folder | Views |
|---|---|
| `Shared/` | 47 |
| `Account/` | 18 |
| `Product/` | 16 |
| `Catalog/` | 11 |
| `Blog/` | 5 |
| `Checkout/`, `News/`, `Order/` | 2 each |
| `Common/`, `Home/`, `MerchandiseReturn/`, `ShoppingCart/`, `Wishlist/` | 1 each |

The weight is in `Shared/` — layouts, partials, and components, the markup that defines the theme's look. Everything not listed (vendor pages, courses, knowledgebase, most of checkout) falls through to the default views and stays correct for free.

**That distribution is the lesson.** Start from layouts and shared partials. Copy a page-level view only when its structure genuinely differs; if only styling differs, a rule in `Content/css/` is cheaper and does not need re-reconciling on upgrade.

## The `_ViewImports.cshtml` trap

The theme's view folder does **not** inherit `Grand.Web`'s imports. `Views/Modern/_ViewImports.cshtml` re-declares everything — the tag helper registrations, ~30 `@using` lines for `Grand.Web.Models.*` / `Grand.Domain.*` / `Grand.Web.Common.*`, and `@inject LocService Loc`.

It also carries this, which is easy to lose when writing a new theme by hand:

```cshtml
@*we remove the default InputTagHelper to prevent the checkbox duplicating*@
@removeTagHelper Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper, Microsoft.AspNetCore.Mvc.TagHelpers
```

Without it, every checkbox in the theme renders twice. Copy the file wholesale rather than assembling the `@using` list from scratch — a missing namespace only surfaces when some copied view fails to compile.

## Assets

`Content/` is grouped by area — `common/`, `header/`, `home/`, `catalog/`, `product/`, `cart/`, `customer/`, `blog-news/`, `fonts/` — plus `script/` and a vendored `swiper/`. The whole tree is copied with `PreserveNewest` and served from `~/Plugins/Theme.Modern/Content/...`.

`Content/theme.jpg` is what `ThemeInfo.PreviewImageUrl` points at — the image the admin theme picker shows. `logo.jpg` is the separate plugin-list logo. Both are required.

## Project file, the theme-specific parts

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
	<PropertyGroup>
		<AddRazorSupportForMvc>true</AddRazorSupportForMvc>
		<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>
	</PropertyGroup>
	…
	<ProjectReference Include="..\..\Web\Grand.Web\Grand.Web.csproj">
		<Private>false</Private>
		<ExcludeAssets>all</ExcludeAssets>
	</ProjectReference>
```

A theme is the one plugin kind that references `Grand.Web` — it compiles against the storefront view models. `ExcludeAssets=all` keeps the assembly out of the plugin's output; the host already has it.

Output path is set for both Debug and Release to `..\..\Web\Grand.Web\Plugins\Theme.Modern\`.

## The upgrade cost

Every copied view is a file that must be reconciled against upstream on each GrandNode upgrade. A view that drifted silently — a widget zone dropped, a `data-cart-action` attribute lost, a model property renamed — is the standard theme failure mode.

Mitigations, in order of value:

1. Copy fewer views. Prefer CSS.
2. Record which views were copied, at which upstream revision, in the PR.
3. On upgrade, diff each copied view against its `Grand.Web` counterpart before assuming it still works.
4. Never change a view model or route from inside a theme — that turns an override into a fork.
