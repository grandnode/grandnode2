# Standard: Dependencies and Build

---

## Central package management

`Directory.Packages.props` at the repository root sets `ManagePackageVersionsCentrally=true`.

Consequences:

- Project files reference packages **without** a version:
  ```xml
  <PackageReference Include="FluentValidation" />
  ```
- Versions are declared once, in `Directory.Packages.props`:
  ```xml
  <PackageVersion Include="FluentValidation" Version="12.1.1" />
  ```
- Adding a version attribute in a `.csproj` is a build error, not a style issue.
- Bumping a version affects every project — call it out explicitly in the PR.

Do not add a new third-party package when the repository already has a capability for it. Check first:

| Need | Already present |
|---|---|
| Mediator / CQRS | `Grand.Mediator` (in-house; MediatR is **not** referenced) |
| Validation | `FluentValidation` |
| Object mapping | `Grand.Mapping` (AutoMapper-compatible API; AutoMapper itself is **not** referenced) |
| MongoDB | `MongoDB.Driver` |
| Caching / distributed cache | `Microsoft.Extensions.Caching.Memory`, `StackExchange.Redis` |
| Templating for messages | `DotLiquid` |
| PDF | `Scryber.Core` |
| Images | `SixLabors.ImageSharp`, `SkiaSharp` |
| Mail | `MailKit` |
| DI scanning | `Scrutor` |
| API docs | `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore` |

## Shared MSBuild props

Every project imports `..\..\Build\Grand.Common.props`, which sets:

- `TargetFramework` = `net10.0`
- `LangVersion` = `latest`
- `ImplicitUsings` = `true`
- global using of `System.Text`
- release build with no debug symbols
- product version, currently `2.4.0`, set in the `SetVersion` target

Do not override `TargetFramework` or `LangVersion` in an individual project.

## Project references

- Reference GrandNode projects with `<Private>false</Private>` in plugins and modules — the host already loads those assemblies.
- Use `<ExcludeAssets>all</ExcludeAssets>` (plugins referencing `Grand.Web` / `Grand.Web.Common`) or `<ExcludeAssets>runtime</ExcludeAssets>` (modules) following the nearest existing project of the same kind.
- Never reference a plugin from core, business, or web projects. Dependencies point inward only.

## Output paths

Plugins and modules must write into the host's discovery folders:

```xml
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
  <OutputPath>..\..\Web\Grand.Web\Plugins\{SystemName}\</OutputPath>
  <OutDir>$(OutputPath)</OutDir>
</PropertyGroup>
```

Modules use `..\..\Web\Grand.Web\Modules\{ModuleName}\`. Both Debug and Release configurations must be set — a missing Release path means the plugin silently disappears from release builds.

## SDK selection

| Contents | SDK | Extra properties |
|---|---|---|
| No Razor views | `Microsoft.NET.Sdk` | — |
| Razor views | `Microsoft.NET.Sdk.Razor` | `AddRazorSupportForMvc=true`, `StaticWebAssetsEnabled=false` |

## Static content

```xml
<None Update="Content\**\*.*">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
<None Update="logo.jpg">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

`logo.jpg` is required for a plugin to render in the admin plugin list. Themes additionally need `Content/theme.jpg` for the theme preview.

## Adding a project

1. Create under the correct `src/` folder — see `.ai/knowledge/repository-map.md`.
2. Import `Grand.Common.props`.
3. Add to `GrandNode.sln`.
4. Add the mirror test project under `src/Tests/` when the project contains logic.
