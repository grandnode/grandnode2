# Custom Theme: Runtime Branding Customizer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a per-store branding customizer (5 colors + 3 images) to the GrandNode2 admin panel that injects CSS custom properties into the storefront at runtime.

**Architecture:** A new `BrandingSettings : ISettings` domain class is persisted per-store via the existing `ISettingService`. A Razor partial `_BrandingStyles.cshtml` reads those settings and emits a `<style>` block of CSS custom properties into `Theme.Modern`'s `<head>`. A standalone `BrandingController` exposes a Design > Branding admin page with color pickers and image uploaders.

**Tech Stack:** ASP.NET Core 10, MongoDB via `ISettingService`, Razor tag helpers (`admin-input`, `admin-label` with `[UIHint("Picture")]`), native HTML5 `<input type="color">`, MSTest + Moq.

---

## File Map

| Action | Path | Responsibility |
|---|---|---|
| Create | `src/Core/Grand.Domain/Stores/BrandingSettings.cs` | Domain settings entity (5 colors + 3 picture IDs) |
| Create | `src/Web/Grand.Web.AdminShared/Models/Settings/BrandingSettingsModel.cs` | Admin view model |
| Create | `src/Web/Grand.Web.Admin/Controllers/BrandingController.cs` | GET/POST admin CRUD |
| Create | `src/Web/Grand.Web.Admin/Areas/Admin/Views/Branding/Index.cshtml` | Admin branding UI |
| Create | `src/Web/Grand.Web.Common/Views/Shared/Partials/_BrandingStyles.cshtml` | CSS variable emitter |
| Modify | `src/Plugins/Theme.Modern/Views/Modern/Shared/_Layout.cshtml` | Include `_BrandingStyles` + conditional logo |
| Modify | `src/Plugins/Theme.Modern/Content/css/header/header.css` | Replace hardcoded colors with CSS vars |
| Modify | `src/Modules/Grand.Module.Installer/Utilities/StandardAdminSiteMap.cs` | Add Design > Branding (new installs) |
| Create | `src/Modules/Grand.Module.Migration/Migrations/2.5/MigrationAddDesignSiteMap.cs` | Insert menu for existing installs |
| Create | `src/Tests/Grand.Business.Common.Tests/Services/Configuration/BrandingSettingsTests.cs` | Unit tests |

---

## Task 1: BrandingSettings domain class

**Files:**
- Create: `src/Core/Grand.Domain/Stores/BrandingSettings.cs`
- Test: `src/Tests/Grand.Business.Common.Tests/Services/Configuration/BrandingSettingsTests.cs`

- [ ] **Step 1: Write the failing test**

Open `src/Tests/Grand.Business.Common.Tests/Services/Configuration/BrandingSettingsTests.cs` and create it:

```csharp
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Data;
using Grand.Domain.Configuration;
using Grand.Domain.Stores;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Configuration;

[TestClass]
public class BrandingSettingsTests
{
    private Mock<ICacheBase> _cacheMock;
    private Mock<IRepository<Setting>> _repositoryMock;

    [TestInitialize]
    public void Init()
    {
        _cacheMock = new Mock<ICacheBase>();
        _repositoryMock = new Mock<IRepository<Setting>>();
        _cacheMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<IList<Setting>>>>()))
            .ReturnsAsync(new List<Setting>());
    }

    [TestMethod]
    public void BrandingSettings_DefaultInstance_HasNullColorProperties()
    {
        var settings = new BrandingSettings();
        Assert.IsNull(settings.PrimaryColor);
        Assert.IsNull(settings.SecondaryColor);
        Assert.IsNull(settings.AccentColor);
        Assert.IsNull(settings.BackgroundColor);
        Assert.IsNull(settings.TextColor);
    }

    [TestMethod]
    public void BrandingSettings_DefaultInstance_HasNullPictureIds()
    {
        var settings = new BrandingSettings();
        Assert.IsNull(settings.LogoPictureId);
        Assert.IsNull(settings.FaviconPictureId);
        Assert.IsNull(settings.BannerPictureId);
    }

    [TestMethod]
    public void BrandingSettings_ImplementsISettings()
    {
        var settings = new BrandingSettings();
        Assert.IsInstanceOfType(settings, typeof(ISettings));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test src/Tests/Grand.Business.Common.Tests/Grand.Business.Common.Tests.csproj --filter "BrandingSettingsTests"
```

Expected: compile error — `BrandingSettings` does not exist yet.

- [ ] **Step 3: Create BrandingSettings.cs**

Create `src/Core/Grand.Domain/Stores/BrandingSettings.cs`:

```csharp
using Grand.Domain.Configuration;

namespace Grand.Domain.Stores;

public class BrandingSettings : ISettings
{
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    public string AccentColor { get; set; }
    public string BackgroundColor { get; set; }
    public string TextColor { get; set; }
    public string LogoPictureId { get; set; }
    public string FaviconPictureId { get; set; }
    public string BannerPictureId { get; set; }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test src/Tests/Grand.Business.Common.Tests/Grand.Business.Common.Tests.csproj --filter "BrandingSettingsTests"
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Core/Grand.Domain/Stores/BrandingSettings.cs src/Tests/Grand.Business.Common.Tests/Services/Configuration/BrandingSettingsTests.cs
git commit -m "feat: add BrandingSettings domain class with tests"
```

---

## Task 2: BrandingSettingsModel view model

**Files:**
- Create: `src/Web/Grand.Web.AdminShared/Models/Settings/BrandingSettingsModel.cs`

- [ ] **Step 1: Create BrandingSettingsModel.cs**

Create `src/Web/Grand.Web.AdminShared/Models/Settings/BrandingSettingsModel.cs`:

```csharp
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Settings;

public class BrandingSettingsModel : BaseModel
{
    public string ActiveStore { get; set; }

    [GrandResourceDisplayName("Admin.Design.Branding.PrimaryColor")]
    public string PrimaryColor { get; set; }

    [GrandResourceDisplayName("Admin.Design.Branding.SecondaryColor")]
    public string SecondaryColor { get; set; }

    [GrandResourceDisplayName("Admin.Design.Branding.AccentColor")]
    public string AccentColor { get; set; }

    [GrandResourceDisplayName("Admin.Design.Branding.BackgroundColor")]
    public string BackgroundColor { get; set; }

    [GrandResourceDisplayName("Admin.Design.Branding.TextColor")]
    public string TextColor { get; set; }

    [UIHint("Picture")]
    [GrandResourceDisplayName("Admin.Design.Branding.Logo")]
    public string LogoPictureId { get; set; }

    [UIHint("Picture")]
    [GrandResourceDisplayName("Admin.Design.Branding.Favicon")]
    public string FaviconPictureId { get; set; }

    [UIHint("Picture")]
    [GrandResourceDisplayName("Admin.Design.Branding.Banner")]
    public string BannerPictureId { get; set; }
}
```

> **Note:** `GrandResourceDisplayName` is a custom attribute already used in neighboring models in this namespace. Check one of those files (e.g., `GeneralCommonSettingsModel.cs`) for the exact `using` directive it requires and add it here.

- [ ] **Step 2: Build to verify compilation**

```bash
dotnet build src/Web/Grand.Web.AdminShared/Grand.Web.AdminShared.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Web/Grand.Web.AdminShared/Models/Settings/BrandingSettingsModel.cs
git commit -m "feat: add BrandingSettingsModel admin view model"
```

---

## Task 3: BrandingController

**Files:**
- Create: `src/Web/Grand.Web.Admin/Controllers/BrandingController.cs`

- [ ] **Step 1: Create BrandingController.cs**

Create `src/Web/Grand.Web.Admin/Controllers/BrandingController.cs`:

```csharp
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Stores;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Models.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

public class BrandingController : BaseAdminController
{
    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;

    public BrandingController(ISettingService settingService, ITranslationService translationService)
    {
        _settingService = settingService;
        _translationService = translationService;
    }

    public async Task<IActionResult> Index()
    {
        var storeScope = await GetActiveStore();
        var settings = await _settingService.LoadSetting<BrandingSettings>(storeScope);

        var model = new BrandingSettingsModel {
            ActiveStore = storeScope,
            PrimaryColor = settings.PrimaryColor,
            SecondaryColor = settings.SecondaryColor,
            AccentColor = settings.AccentColor,
            BackgroundColor = settings.BackgroundColor,
            TextColor = settings.TextColor,
            LogoPictureId = settings.LogoPictureId,
            FaviconPictureId = settings.FaviconPictureId,
            BannerPictureId = settings.BannerPictureId
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(BrandingSettingsModel model)
    {
        var storeScope = await GetActiveStore();
        var settings = await _settingService.LoadSetting<BrandingSettings>(storeScope);

        settings.PrimaryColor = model.PrimaryColor;
        settings.SecondaryColor = model.SecondaryColor;
        settings.AccentColor = model.AccentColor;
        settings.BackgroundColor = model.BackgroundColor;
        settings.TextColor = model.TextColor;
        settings.LogoPictureId = model.LogoPictureId;
        settings.FaviconPictureId = model.FaviconPictureId;
        settings.BannerPictureId = model.BannerPictureId;

        await _settingService.SaveSetting(settings, storeScope);

        Success(_translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Index");
    }
}
```

- [ ] **Step 2: Build to verify compilation**

```bash
dotnet build src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Web/Grand.Web.Admin/Controllers/BrandingController.cs
git commit -m "feat: add BrandingController with GET/POST for per-store branding settings"
```

---

## Task 4: Admin Branding view

**Files:**
- Create: `src/Web/Grand.Web.Admin/Areas/Admin/Views/Branding/Index.cshtml`

- [ ] **Step 1: Create the Branding views directory and Index.cshtml**

Create directory `src/Web/Grand.Web.Admin/Areas/Admin/Views/Branding/` and create `Index.cshtml`:

```cshtml
@model BrandingSettingsModel
@{
    Layout = "_AdminLayout";
    ViewBag.Title = Loc["Admin.Design.Branding"];
}

<div class="content-header clearfix">
    <h1 class="float-left">@Loc["Admin.Design.Branding"]</h1>
</div>

<form asp-action="Index" method="post">
    <div class="container-fluid">
        <div class="form-horizontal">
            <input asp-for="ActiveStore" type="hidden"/>

            <grand-panel>
                <grand-panel-heading>@Loc["Admin.Design.Branding.Colors"]</grand-panel-heading>
                <grand-panel-body>

                    <div class="form-group row">
                        <div class="col-4 col-sm-3 text-right">
                            <admin-label asp-for="PrimaryColor" class="control-label"/>
                        </div>
                        <div class="col-8 col-sm-9">
                            <input type="color" asp-for="PrimaryColor" class="form-control" style="width:80px; padding:2px;"/>
                        </div>
                    </div>

                    <div class="form-group row">
                        <div class="col-4 col-sm-3 text-right">
                            <admin-label asp-for="SecondaryColor" class="control-label"/>
                        </div>
                        <div class="col-8 col-sm-9">
                            <input type="color" asp-for="SecondaryColor" class="form-control" style="width:80px; padding:2px;"/>
                        </div>
                    </div>

                    <div class="form-group row">
                        <div class="col-4 col-sm-3 text-right">
                            <admin-label asp-for="AccentColor" class="control-label"/>
                        </div>
                        <div class="col-8 col-sm-9">
                            <input type="color" asp-for="AccentColor" class="form-control" style="width:80px; padding:2px;"/>
                        </div>
                    </div>

                    <div class="form-group row">
                        <div class="col-4 col-sm-3 text-right">
                            <admin-label asp-for="BackgroundColor" class="control-label"/>
                        </div>
                        <div class="col-8 col-sm-9">
                            <input type="color" asp-for="BackgroundColor" class="form-control" style="width:80px; padding:2px;"/>
                        </div>
                    </div>

                    <div class="form-group row">
                        <div class="col-4 col-sm-3 text-right">
                            <admin-label asp-for="TextColor" class="control-label"/>
                        </div>
                        <div class="col-8 col-sm-9">
                            <input type="color" asp-for="TextColor" class="form-control" style="width:80px; padding:2px;"/>
                        </div>
                    </div>

                </grand-panel-body>
            </grand-panel>

            <grand-panel>
                <grand-panel-heading>@Loc["Admin.Design.Branding.Images"]</grand-panel-heading>
                <grand-panel-body>

                    <div class="form-group row">
                        <div class="col-4 col-sm-3 text-right">
                            <admin-label asp-for="LogoPictureId" class="control-label"/>
                        </div>
                        <div class="col-8 col-sm-9">
                            <admin-input asp-for="LogoPictureId"/>
                        </div>
                    </div>

                    <div class="form-group row">
                        <div class="col-4 col-sm-3 text-right">
                            <admin-label asp-for="FaviconPictureId" class="control-label"/>
                        </div>
                        <div class="col-8 col-sm-9">
                            <admin-input asp-for="FaviconPictureId"/>
                        </div>
                    </div>

                    <div class="form-group row">
                        <div class="col-4 col-sm-3 text-right">
                            <admin-label asp-for="BannerPictureId" class="control-label"/>
                        </div>
                        <div class="col-8 col-sm-9">
                            <admin-input asp-for="BannerPictureId"/>
                        </div>
                    </div>

                </grand-panel-body>
            </grand-panel>

            <grand-panel>
                <grand-panel-body>
                    <button type="submit" class="btn btn-primary">@Loc["Admin.Common.Save"]</button>
                </grand-panel-body>
            </grand-panel>

        </div>
    </div>
</form>
```

> **Note:** `grand-panel`, `grand-panel-heading`, `grand-panel-body` are tag helpers used throughout the admin area. Check any neighboring admin view (e.g., `src/Web/Grand.Web.Admin/Areas/Admin/Views/Setting/Partials/GeneralCommon.TabStoreInformationSettings.cshtml`) to confirm the exact tag helper names if the build fails. The `[UIHint("Picture")]` on the model property causes `<admin-input>` to automatically render as a picture uploader.

- [ ] **Step 2: Build to check view compilation**

```bash
dotnet build src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Web/Grand.Web.Admin/Areas/Admin/Views/Branding/Index.cshtml
git commit -m "feat: add admin Branding index view with color pickers and image uploaders"
```

---

## Task 5: _BrandingStyles partial view

**Files:**
- Create: `src/Web/Grand.Web.Common/Views/Shared/Partials/_BrandingStyles.cshtml`

- [ ] **Step 1: Create _BrandingStyles.cshtml**

Create `src/Web/Grand.Web.Common/Views/Shared/Partials/_BrandingStyles.cshtml`:

```cshtml
@using Grand.Business.Core.Interfaces.Common.Configuration
@using Grand.Business.Core.Interfaces.Storage
@using Grand.Domain.Stores
@using Grand.Infrastructure
@inject ISettingService settingService
@inject IContextAccessor contextAccessor
@inject IPictureService pictureService
@{
    var storeId = contextAccessor.StoreContext.CurrentStore.Id;
    var branding = await settingService.LoadSetting<BrandingSettings>(storeId);

    var hasColors = !string.IsNullOrEmpty(branding.PrimaryColor)
        || !string.IsNullOrEmpty(branding.SecondaryColor)
        || !string.IsNullOrEmpty(branding.AccentColor)
        || !string.IsNullOrEmpty(branding.BackgroundColor)
        || !string.IsNullOrEmpty(branding.TextColor)
        || !string.IsNullOrEmpty(branding.BannerPictureId);

    var faviconUrl = !string.IsNullOrEmpty(branding.FaviconPictureId)
        ? await pictureService.GetPictureUrl(branding.FaviconPictureId)
        : null;

    var bannerUrl = !string.IsNullOrEmpty(branding.BannerPictureId)
        ? await pictureService.GetPictureUrl(branding.BannerPictureId)
        : null;
}
@if (hasColors)
{
    <style>
        :root {
            @if (!string.IsNullOrEmpty(branding.PrimaryColor)) {
                <text>--brand-primary: @branding.PrimaryColor;</text>
            }
            @if (!string.IsNullOrEmpty(branding.SecondaryColor)) {
                <text>--brand-secondary: @branding.SecondaryColor;</text>
            }
            @if (!string.IsNullOrEmpty(branding.AccentColor)) {
                <text>--brand-accent: @branding.AccentColor;</text>
            }
            @if (!string.IsNullOrEmpty(branding.BackgroundColor)) {
                <text>--brand-background: @branding.BackgroundColor;</text>
            }
            @if (!string.IsNullOrEmpty(branding.TextColor)) {
                <text>--brand-text: @branding.TextColor;</text>
            }
            @if (!string.IsNullOrEmpty(bannerUrl)) {
                <text>--brand-banner: url('@bannerUrl');</text>
            }
        }
    </style>
}
@if (!string.IsNullOrEmpty(faviconUrl))
{
    <link rel="shortcut icon" href="@faviconUrl" type="image/x-icon"/>
}
```

- [ ] **Step 2: Build Grand.Web.Common to verify compilation**

```bash
dotnet build src/Web/Grand.Web.Common/Grand.Web.Common.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Web/Grand.Web.Common/Views/Shared/Partials/_BrandingStyles.cshtml
git commit -m "feat: add _BrandingStyles partial that injects CSS custom properties per store"
```

---

## Task 6: Integrate partial into Theme.Modern layout

**Files:**
- Modify: `src/Plugins/Theme.Modern/Views/Modern/Shared/_Layout.cshtml`

- [ ] **Step 1: Read the current layout file**

Open `src/Plugins/Theme.Modern/Views/Modern/Shared/_Layout.cshtml` and locate the `<head>` section. It currently ends with something like:

```html
<partial name="Partials/Favicons"/>
<resources asp-type="HeadLink"/>
<resources asp-type="HeadScript"/>
@await RenderSectionAsync("head", false)
```

- [ ] **Step 2: Insert _BrandingStyles into the head**

Add `<partial name="Partials/_BrandingStyles"/>` immediately after `<partial name="Partials/Favicons"/>`. The resulting block should be:

```html
<partial name="Partials/Favicons"/>
<partial name="Partials/_BrandingStyles"/>
<resources asp-type="HeadLink"/>
<resources asp-type="HeadScript"/>
@await RenderSectionAsync("head", false)
```

- [ ] **Step 3: Update the logo rendering in the layout**

Locate where the logo is currently rendered in the layout (search for `Logo` or `store-logo`). There will be a `<partial name="Logo"/>` or direct img rendering. Replace it with a conditional that uses the branding logo when set:

```cshtml
@inject Grand.Business.Core.Interfaces.Common.Configuration.ISettingService _brandingSettingsSvc
@inject Grand.Business.Core.Interfaces.Storage.IPictureService _brandingPictureSvc
@inject Grand.Infrastructure.IContextAccessor _brandingCtx
@{
    var _brandingSettings = await _brandingSettingsSvc.LoadSetting<Grand.Domain.Stores.BrandingSettings>(
        _brandingCtx.StoreContext.CurrentStore.Id);
    var _brandingLogoUrl = !string.IsNullOrEmpty(_brandingSettings.LogoPictureId)
        ? await _brandingPictureSvc.GetPictureUrl(_brandingSettings.LogoPictureId)
        : null;
}
@if (!string.IsNullOrEmpty(_brandingLogoUrl))
{
    <a class="navbar-brand store-logo mx-lg-0 mx-auto" href="@Url.RouteUrl("HomePage")">
        <img src="@_brandingLogoUrl" alt="@_brandingCtx.StoreContext.CurrentStore.Name"/>
    </a>
}
else
{
    <partial name="Logo"/>
}
```

> **Note:** Use the `_branding*` prefix for all locals in this block to avoid name conflicts with other `@inject` or `@{...}` blocks already in the layout. Place this block where the current logo element is — locate it by searching for `Logo` or `navbar-brand` in the file.

- [ ] **Step 4: Build the plugin to verify compilation**

```bash
dotnet build src/Plugins/Theme.Modern/Theme.Modern.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/Plugins/Theme.Modern/Views/Modern/Shared/_Layout.cshtml
git commit -m "feat: inject _BrandingStyles partial and conditional logo into Theme.Modern layout"
```

---

## Task 7: Update Theme.Modern CSS to use CSS variables

**Files:**
- Modify: `src/Plugins/Theme.Modern/Content/css/header/header.css`

The goal is to replace hardcoded recurring color values with `var(--brand-*, fallback)`. Focus on the five colors that map to the branding settings:

| CSS color | Brand variable | Used for |
|---|---|---|
| `#25b232` | `--brand-primary` | Links, prices, CTAs, active states |
| `#232323` / `#111` | `--brand-secondary` | Header/footer dark background |
| `#fff` | `--brand-background` | White backgrounds, cards |
| `#000` | `--brand-text` | Body text, labels |
| `#eaf0fa` | `--brand-accent` | Light hover states, highlights |

- [ ] **Step 1: Replace primary color in header.css**

Open `src/Plugins/Theme.Modern/Content/css/header/header.css`. Replace all occurrences of `#25b232` and `color: #25b232` with the CSS variable form:

Before:
```css
color: #25b232;
```

After:
```css
color: var(--brand-primary, #25b232);
```

Before:
```css
background-color: #25b232;
```

After:
```css
background-color: var(--brand-primary, #25b232);
```

Apply this pattern to every `#25b232` occurrence in `header.css`.

- [ ] **Step 2: Replace secondary (dark) color in header.css**

Replace `#232323` with `var(--brand-secondary, #232323)` and `#111` with `var(--brand-secondary, #111)` throughout `header.css`. Example:

Before:
```css
background-color: #232323;
```

After:
```css
background-color: var(--brand-secondary, #232323);
```

- [ ] **Step 3: Apply same pattern to remaining colors in header.css**

- Replace `background-color: #fff` with `background-color: var(--brand-background, #fff)` where it represents page/card backgrounds (not when `#fff` is used for text color on dark backgrounds — those stay hardcoded or use `--brand-text` as appropriate).
- Replace `color: #000` / `color: #000000` with `color: var(--brand-text, #000)`.
- Replace `background-color: #eaf0fa` with `background-color: var(--brand-accent, #eaf0fa)`.

- [ ] **Step 4: Build and verify CSS loads**

```bash
dotnet build src/Plugins/Theme.Modern/Theme.Modern.csproj
```

Expected: Build succeeded.

> **Manual check:** Start the app (`dotnet run --project src/Web/Grand.Web`) and verify the storefront header still renders correctly with the default fallback colors before any branding settings are saved. See CLAUDE.md for the full run command.

- [ ] **Step 5: Commit**

```bash
git add src/Plugins/Theme.Modern/Content/css/header/header.css
git commit -m "feat: replace hardcoded colors in Theme.Modern header CSS with CSS custom properties"
```

---

## Task 8: Register admin menu item

**Files:**
- Modify: `src/Modules/Grand.Module.Installer/Utilities/StandardAdminSiteMap.cs`
- Create: `src/Modules/Grand.Module.Migration/Migrations/2.5/MigrationAddDesignSiteMap.cs`

- [ ] **Step 1: Add Design > Branding to StandardAdminSiteMap (new installs)**

Open `src/Modules/Grand.Module.Installer/Utilities/StandardAdminSiteMap.cs`. Locate the `SiteMap` list and add a new entry. Insert it between the last content-related group and the Configuration group (adjust `DisplayOrder` to fit between neighbors):

```csharp
new AdminSiteMap {
    SystemName = "Design",
    ResourceName = "Admin.Design",
    IconClass = "fa fa-paint-brush",
    DisplayOrder = 25,
    ChildNodes = new List<AdminSiteMap> {
        new() {
            SystemName = "Branding",
            ResourceName = "Admin.Design.Branding",
            ControllerName = "Branding",
            ActionName = "Index",
            DisplayOrder = 0,
            IconClass = "fa fa-dot-circle-o"
        }
    }
},
```

- [ ] **Step 2: Create migration folder and file for existing installs**

Create directory `src/Modules/Grand.Module.Migration/Migrations/2.5/` and create `MigrationAddDesignSiteMap.cs`:

```csharp
using Grand.Data;
using Grand.Domain.Admin;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationAddDesignSiteMap : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
    public string Name => "Add Design > Branding to admin site map";

    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationAddDesignSiteMap>>();

        try
        {
            if (repository.Table.Any(x => x.SystemName == "Design"))
                return true;

            var designMenu = new AdminSiteMap {
                SystemName = "Design",
                ResourceName = "Admin.Design",
                IconClass = "fa fa-paint-brush",
                DisplayOrder = 25,
                ChildNodes = new List<AdminSiteMap> {
                    new() {
                        SystemName = "Branding",
                        ResourceName = "Admin.Design.Branding",
                        ControllerName = "Branding",
                        ActionName = "Index",
                        DisplayOrder = 0,
                        IconClass = "fa fa-dot-circle-o"
                    }
                }
            };

            repository.Insert(designMenu);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - AddDesignSiteMap");
        }

        return true;
    }
}
```

> **Note:** The `Guid Identity` must be unique across all migrations. The value `A1B2C3D4-E5F6-7890-ABCD-EF1234567890` is a placeholder — generate a real GUID (e.g., via `[System.Guid]::NewGuid()` in PowerShell or any online GUID generator) and replace it before committing.

- [ ] **Step 3: Build the migration module**

```bash
dotnet build src/Modules/Grand.Module.Migration/Grand.Module.Migration.csproj
dotnet build src/Modules/Grand.Module.Installer/Grand.Module.Installer.csproj
```

Expected: Both build with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Modules/Grand.Module.Installer/Utilities/StandardAdminSiteMap.cs
git add src/Modules/Grand.Module.Migration/Migrations/2.5/MigrationAddDesignSiteMap.cs
git commit -m "feat: register Design > Branding admin menu item for new and existing installs"
```

---

## Task 9: Full build and end-to-end verification

- [ ] **Step 1: Run full solution build**

```bash
dotnet build GrandNode.sln --configuration Release
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 2: Run all tests**

```bash
dotnet test GrandNode.sln
```

Expected: All tests pass.

- [ ] **Step 3: Start the app and verify the admin page exists**

```bash
# Terminal 1 — start MongoDB
docker run -d -p 127.0.0.1:27017:27017 --name mongodb mongo

# Terminal 2 — start the app
dotnet run --project src/Web/Grand.Web
```

Navigate to `http://localhost:5000/Admin/Branding`. Confirm:
- [ ] Page loads without errors
- [ ] Five color pickers are visible
- [ ] Three image upload fields are visible
- [ ] Saving colors works (no server error)

- [ ] **Step 4: Verify CSS variables are injected**

Open browser DevTools → Elements → `<head>`. After saving at least one color, confirm a `<style>` block containing `:root { --brand-primary: #... }` appears.

- [ ] **Step 5: Verify Theme.Modern reflects the saved color**

Set `PrimaryColor` to `#e63946` (a bright red). Save. Reload the storefront. Confirm that elements previously styled `#25b232` (links, prices, CTA buttons) are now red.

- [ ] **Step 6: Final commit**

```bash
git add -A
git commit -m "feat: complete runtime branding customizer — colors, images, CSS vars, admin UI"
```
