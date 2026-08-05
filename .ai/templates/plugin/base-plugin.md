# Template: Base Plugin

The files every installable GrandNode plugin needs. Add the provider interface for the specific kind on top — see `.ai/skills/plugin-payment.md`, `plugin-shipping.md`, `plugin-widget.md`, `plugin-discount-rules.md`, or `plugin-module.md`.

Distilled from `src/Plugins/Payments.CashOnDelivery/`.

Placeholders: `{SystemName}` = `{Group}.{Name}`, `{Feature}` = type-name prefix.

---

## 1. `src/Plugins/{SystemName}/{SystemName}.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
	<Import Project="..\..\Build\Grand.Common.props" />
	<PropertyGroup>
		<AddRazorSupportForMvc>true</AddRazorSupportForMvc>
		<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>
	</PropertyGroup>

	<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
		<OutputPath>..\..\Web\Grand.Web\Plugins\{SystemName}\</OutputPath>
		<OutDir>$(OutputPath)</OutDir>
	</PropertyGroup>

	<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
		<OutputPath>..\..\Web\Grand.Web\Plugins\{SystemName}\</OutputPath>
		<OutDir>$(OutputPath)</OutDir>
	</PropertyGroup>

	<ItemGroup>
		<ProjectReference Include="..\..\Core\Grand.Data\Grand.Data.csproj">
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
		<ProjectReference Include="..\..\Business\Grand.Business.Core\Grand.Business.Core.csproj">
			<Private>false</Private>
		</ProjectReference>
		<ProjectReference Include="..\..\Web\Grand.Web.Common\Grand.Web.Common.csproj">
			<Private>false</Private>
			<ExcludeAssets>all</ExcludeAssets>
		</ProjectReference>
	</ItemGroup>

	<ItemGroup>
		<None Update="logo.jpg">
			<CopyToOutputDirectory>Always</CopyToOutputDirectory>
		</None>
	</ItemGroup>
</Project>
```

Use `Microsoft.NET.Sdk` and drop the Razor properties when the plugin ships no `.cshtml`. Trim project references down to what the plugin actually uses.

## 2. `Manifest.cs`

```csharp
using Grand.Infrastructure.Plugins;

[assembly: PluginInfo(
    FriendlyName = "{Human readable name}",
    Group = "{Payment methods|Shipping rate|Tax providers|Widgets|External authentication|Discount rules|Exchange rate|Themes}",
    SystemName = {Feature}Defaults.ProviderSystemName,
    Author = "grandnode team",
    Version = "1.0.0"
)]
```

## 3. `{Feature}Defaults.cs`

```csharp
namespace {SystemName};

public static class {Feature}Defaults
{
    public const string ProviderSystemName = "{SystemName}";
    public const string FriendlyName = "{SystemName}.FriendlyName";
    public const string ConfigurationUrl = "/Admin/{ControllerName}/Configure";
}
```

`ProviderSystemName` is the plugin's persisted identity. It must equal the `Manifest.cs` `SystemName` and the output folder name, and must never change after release.

## 4. `{Feature}Settings.cs`

```csharp
using Grand.Domain.Configuration;

namespace {SystemName};

public class {Feature}Settings : ISettings
{
    public int DisplayOrder { get; set; }
    // add plugin-specific settings
}
```

Every setting needs a default that preserves existing behavior for stores upgrading into it.

## 5. `{Feature}Plugin.cs`

```csharp
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace {SystemName};

public class {Feature}Plugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    public override string ConfigurationUrl()
    {
        return {Feature}Defaults.ConfigurationUrl;
    }

    public override async Task Install()
    {
        var settings = new {Feature}Settings {
            DisplayOrder = 0
        };
        await settingService.SaveSetting(settings);

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            {Feature}Defaults.FriendlyName, "{Human readable name}");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.{Group}.{Name}.DisplayOrder", "Display order");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.{Group}.{Name}.DisplayOrder.Hint", "The display order of this provider.");

        await base.Install();
    }

    public override async Task Uninstall()
    {
        await settingService.DeleteSetting<{Feature}Settings>();

        await pluginTranslateResource.DeletePluginTranslationResource(
            "Plugins.{Group}.{Name}.DisplayOrder");
        await pluginTranslateResource.DeletePluginTranslationResource(
            "Plugins.{Group}.{Name}.DisplayOrder.Hint");

        await base.Uninstall();
    }
}
```

`base.Install()` marks the plugin installed and `base.Uninstall()` marks it uninstalled — call them **last**, after the work that must not be skipped.

Every resource key added in `Install()` must be removed in `Uninstall()`. Mismatches leave orphaned resources in every store that ever installed the plugin.

## 6. `{Feature}Provider.cs`

Implements the provider interface for the plugin kind. Every provider derives from `IProvider`:

```csharp
public string ConfigurationUrl => {Feature}Defaults.ConfigurationUrl;
public string SystemName => {Feature}Defaults.ProviderSystemName;
public string FriendlyName => translationService.GetResource({Feature}Defaults.FriendlyName);
public int Priority => settings.DisplayOrder;
public IList<string> LimitedToStores => new List<string>();
public IList<string> LimitedToGroups => new List<string>();
```

`FriendlyName` resolves through `ITranslationService` — never a literal. `LimitedToStores` / `LimitedToGroups` are the scoping hooks described in `.ai/knowledge/scoping.md`.

## 7. `StartupApplication.cs`

```csharp
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {SystemName};

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<I{Kind}Provider, {Feature}Provider>();
    }

    public int Priority => 10;

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public bool BeforeConfigure => false;
}
```

Priority `10` is the convention for plugins. Leave `Configure` empty unless the plugin owns middleware or endpoints.

## 8. `logo.jpg`

Required. Without it the plugin renders without an image in the admin plugin list.

---

## Checklist

- [ ] Project added to `GrandNode.sln`.
- [ ] `SystemName` identical in `Manifest.cs`, `{Feature}Defaults`, and the output folder.
- [ ] `Group` is one of the existing group names.
- [ ] Output path set for Debug **and** Release.
- [ ] All GrandNode references `Private=false`.
- [ ] Provider registered in `StartupApplication`.
- [ ] `Install()` saves settings + adds resources, `base.Install()` last.
- [ ] `Uninstall()` deletes settings + removes every resource `Install()` added, `base.Uninstall()` last.
- [ ] `logo.jpg` present and copied to output.
- [ ] Build output lands in `src/Web/Grand.Web/Plugins/{SystemName}/`.
