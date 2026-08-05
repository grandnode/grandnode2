# Template: Theme Plugin

A storefront theme. Read `.ai/skills/theme-creation.md` first — it holds the contract; this file holds the shape.

Distilled from `src/Plugins/Theme.Modern/`.

Placeholder: `{ThemeName}` — used for `Theme.{ThemeName}` system name, `IThemeView.ThemeName`, and the `Views/{ThemeName}/` folder. All three must match.

---

## Layout

```
src/Plugins/Theme.{ThemeName}/
  Theme.{ThemeName}.csproj
  Manifest.cs
  {ThemeName}ThemePlugin.cs
  {ThemeName}ThemeView.cs
  StartupApplication.cs
  logo.jpg
  Content/
    theme.jpg              ← admin preview image
    css/
    script/
    images/
  Views/
    {ThemeName}/
      _ViewImports.cshtml
      _ViewStart.cshtml
      Shared/
      {Controller}/        ← only the views this theme changes
```

## 1. `Theme.{ThemeName}.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
	<Import Project="..\..\Build\Grand.Common.props" />
	<PropertyGroup>
		<AddRazorSupportForMvc>true</AddRazorSupportForMvc>
		<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>
	</PropertyGroup>

	<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
		<OutputPath>..\..\Web\Grand.Web\Plugins\Theme.{ThemeName}\</OutputPath>
		<OutDir>$(OutputPath)</OutDir>
	</PropertyGroup>

	<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
		<OutputPath>..\..\Web\Grand.Web\Plugins\Theme.{ThemeName}\</OutputPath>
		<OutDir>$(OutputPath)</OutDir>
	</PropertyGroup>

	<ItemGroup>
		<ProjectReference Include="..\..\Core\Grand.Data\Grand.Data.csproj">
			<Private>false</Private>
		</ProjectReference>
		<ProjectReference Include="..\..\Business\Grand.Business.Core\Grand.Business.Core.csproj">
			<Private>false</Private>
		</ProjectReference>
		<ProjectReference Include="..\..\Core\Grand.Domain\Grand.Domain.csproj">
			<Private>false</Private>
		</ProjectReference>
		<ProjectReference Include="..\..\Core\Grand.Mapping\Grand.Mapping.csproj">
			<Private>false</Private>
		</ProjectReference>
		<ProjectReference Include="..\..\Core\Grand.Infrastructure\Grand.Infrastructure.csproj">
			<Private>false</Private>
		</ProjectReference>
		<ProjectReference Include="..\..\Core\Grand.SharedKernel\Grand.SharedKernel.csproj">
			<Private>false</Private>
		</ProjectReference>
		<ProjectReference Include="..\..\Web\Grand.Web.Common\Grand.Web.Common.csproj">
			<Private>false</Private>
			<ExcludeAssets>all</ExcludeAssets>
		</ProjectReference>
		<ProjectReference Include="..\..\Web\Grand.Web\Grand.Web.csproj">
			<Private>false</Private>
			<ExcludeAssets>all</ExcludeAssets>
		</ProjectReference>
	</ItemGroup>

	<ItemGroup>
		<Folder Include="Views\" />
	</ItemGroup>
	<ItemGroup>
		<None Update="Content\**\*.*">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</None>
		<None Update="logo.jpg">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</None>
	</ItemGroup>
</Project>
```

A theme is the one plugin kind that references `Grand.Web` — it compiles against the storefront view models. Both `Grand.Web` and `Grand.Web.Common` use `<ExcludeAssets>all</ExcludeAssets>`; the host already loads them.

## 2. `Manifest.cs`

```csharp
using Grand.Infrastructure.Plugins;

[assembly: PluginInfo(
    FriendlyName = "{ThemeName} theme",
    Group = "Themes",
    SystemName = "Theme.{ThemeName}",
    Author = "grandnode team",
    Version = "1.0.0"
)]
```

## 3. `{ThemeName}ThemePlugin.cs`

```csharp
using Grand.Infrastructure.Plugins;

namespace Theme.{ThemeName};

public class {ThemeName}ThemePlugin : BasePlugin, IPlugin;
```

A theme with no settings and no resource keys needs nothing more. Add `Install()` / `Uninstall()` overrides only if the theme persists something, and call `base` last.

## 4. `{ThemeName}ThemeView.cs`

```csharp
using Grand.Web.Common.Themes;

namespace Theme.{ThemeName};

public class {ThemeName}ThemeView : IThemeView
{
    public string AreaName => "";
    public string ThemeName => "{ThemeName}";

    public ThemeInfo ThemeInfo => new(
        "{ThemeName} theme",
        "~/Plugins/Theme.{ThemeName}/Content/theme.jpg",
        "Short description shown in the admin theme picker",
        false);

    public IEnumerable<string> GetViewLocations()
    {
        return new List<string> {
            "/Views/{ThemeName}/{1}/{0}.cshtml",
            "/Views/{ThemeName}/Shared/{0}.cshtml",
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        };
    }
}
```

`{0}` is the view name and `{1}` the controller name — those braces are format placeholders, not template placeholders; leave them literal. The last two entries are the fallback to `Grand.Web`'s views and must stay.

The fourth `ThemeInfo` argument is `SupportRtl`. Set it truthfully.

## 5. `StartupApplication.cs`

```csharp
using Grand.Infrastructure;
using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Theme.{ThemeName};

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IThemeView, {ThemeName}ThemeView>();
    }

    public int Priority => 10;

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public bool BeforeConfigure => false;
}
```

## 6. `Views/{ThemeName}/_ViewImports.cshtml`

```cshtml
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@*we remove the default InputTagHelper to prevent the checkbox duplicating*@
@removeTagHelper Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Grand.Web.Common
@addTagHelper *, Grand.Web

@using Microsoft.AspNetCore.Http.Extensions
@using Microsoft.AspNetCore.Mvc.Localization
@using Microsoft.AspNetCore.Mvc.ViewFeatures
@using System.Net
@using Grand.SharedKernel.Extensions
@using Grand.Web.Common
@using Grand.Web.Common.Page
@using Grand.Web.Common.Extensions
@using Grand.Web.Common.Security.Captcha
@using Grand.Web.Common.Themes
@using Grand.Web.Common.Localization
@using Grand.Web.Extensions
@using Grand.Web.Models.Catalog
@using Grand.Web.Models.Checkout
@using Grand.Web.Models.Common
@using Grand.Web.Models.Customer
@using Grand.Web.Models.Media
@using Grand.Web.Models.Orders
@using Grand.Web.Models.ShoppingCart
@using Grand.Domain
@using Grand.Domain.Catalog
@using Grand.Domain.Common
@using Grand.Domain.Customers
@using Grand.Domain.Orders
@using Grand.Domain.Stores
@using Grand.Infrastructure

@inject LocService Loc
```

Copy the full `@using` list from `src/Plugins/Theme.Modern/Views/Modern/_ViewImports.cshtml` — the theme does not inherit `Grand.Web`'s imports, and a missing namespace only surfaces when a copied view fails to compile.

The `@removeTagHelper` line is required. Without it every checkbox in the theme renders twice.

## 7. `Views/{ThemeName}/_ViewStart.cshtml`

Copy from `Theme.Modern` — it sets the theme's root layout.

## 8. Content

- `Content/theme.jpg` — admin preview image, referenced by `ThemeInfo.PreviewImageUrl`.
- `logo.jpg` — plugin-list logo.
- `Content/css/` — group by area (`common/`, `header/`, `catalog/`, `product/`, `cart/`, `customer/`) rather than one large file.
- Reference as `~/Plugins/Theme.{ThemeName}/Content/...`. No external CDNs.

## 9. Views

Copy **only** the views the theme changes, preserving the `{Controller}/{Action}.cshtml` structure of `Grand.Web/Views`. Everything not copied resolves through the fallback locations.

For each copied view, keep:

- the same `@model`
- widget zones: `@await Component.InvokeAsync("Widget", new { widgetZone = "..." })`
- storefront data attributes: `data-cart-action`, product IDs, quick-view URLs, wishlist, compare, add-to-cart
- image `alt`, `title`, `loading`, and priority attributes

Record which views were copied, and at which upstream revision, in the PR description — that list is what makes the next upgrade tractable.

---

## Checklist

- [ ] `ThemeName`, the `Views/` subfolder, and the `Theme.{ThemeName}` system name all match.
- [ ] `GetViewLocations()` ends with the two default fallbacks.
- [ ] `IThemeView` registered with `AddScoped`.
- [ ] `Group = "Themes"` in the manifest.
- [ ] Output path set for Debug **and** Release.
- [ ] `AddRazorSupportForMvc=true`, `StaticWebAssetsEnabled=false`.
- [ ] `_ViewImports.cshtml` includes the `@removeTagHelper` for `InputTagHelper`.
- [ ] `_ViewStart.cshtml` present.
- [ ] `logo.jpg` and `Content/theme.jpg` exist and are copied to output.
- [ ] No view model, route, or controller changed by the theme.
- [ ] Build output present under `src/Web/Grand.Web/Plugins/Theme.{ThemeName}/`.
