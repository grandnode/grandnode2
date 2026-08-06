# Template: Plugin Admin Configuration

The admin screen a configurable plugin adds. Distilled from `src/Plugins/Payments.CashOnDelivery/Areas/Admin/`.

Prerequisite: the plugin skeleton from `base-plugin.md`, including `{Feature}Settings` and `{Feature}Defaults.ConfigurationUrl`.

---

## 1. `Models/ConfigurationModel.cs`

```csharp
using Grand.Web.Common.Models;

namespace {SystemName}.Models;

public class ConfigurationModel : BaseModel
{
    public string ActiveStore { get; set; }

    [GrandResourceDisplayName("Plugins.{Group}.{Name}.DisplayOrder")]
    public int DisplayOrder { get; set; }

    // one property per configurable setting, each with its own resource key
}
```

Every displayed field needs a `GrandResourceDisplayName` resource key that `Install()` registers and `Uninstall()` removes.

## 2. `Areas/Admin/Controllers/{ControllerName}Controller.cs`

```csharp
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using {SystemName}.Models;

namespace {SystemName}.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.{Permission})]
public class {ControllerName}Controller : Base{Kind}Controller
{
    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;
    private readonly IAdminStoreService _adminStoreService;

    public {ControllerName}Controller(
        ISettingService settingService,
        ITranslationService translationService,
        IAdminStoreService adminStoreService)
    {
        _settingService = settingService;
        _translationService = translationService;
        _adminStoreService = adminStoreService;
    }

    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope
        var storeScope = await _adminStoreService.GetActiveStore();
        var settings = await _settingService.LoadSetting<{Feature}Settings>(storeScope);

        var model = new ConfigurationModel {
            DisplayOrder = settings.DisplayOrder,
            ActiveStore = storeScope
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var storeScope = await _adminStoreService.GetActiveStore();
        var settings = await _settingService.LoadSetting<{Feature}Settings>(storeScope);

        settings.DisplayOrder = model.DisplayOrder;

        await _settingService.SaveSetting(settings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}
```

Non-negotiable points:

- All three attributes: `[AuthorizeAdmin]`, `[Area("Admin")]`, `[PermissionAuthorize(...)]`. Missing the permission attribute leaves the screen open to any admin.
- The route implied by the controller and action must equal `{Feature}Defaults.ConfigurationUrl`, or the admin's *Configure* link 404s.
- Load **and** save with the same `storeScope` from `IAdminStoreService.GetActiveStore()`. Saving without the scope overwrites the global value for every store.
- `ClearCache()` after saving, or the storefront keeps serving the previous settings.
- Derive from the base controller for the plugin kind (`BasePaymentController`, `BaseShippingController`, …) where one exists.

## 3. `Areas/Admin/Views/_ViewImports.cshtml`

```cshtml
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Grand.Web.Common
@addTagHelper *, Grand.Web

@using Microsoft.AspNetCore.Mvc.ViewFeatures
@using Grand.Infrastructure.Extensions
@using Grand.Web.Common.Localization

@inject LocService Loc
```

## 4. `Areas/Admin/Views/_ViewStart.cshtml`

```cshtml
@{
    Layout = "";
}
```

The empty layout is deliberate — the admin renders plugin configuration inside its own shell.

## 5. `Areas/Admin/Views/{ControllerName}/Configure.cshtml`

```cshtml
@model ConfigurationModel

<div class="form-horizontal">
    <div class="form-group">
        <admin-label asp-for="DisplayOrder" />
        <div class="col-md-9 col-sm-9">
            <admin-input asp-for="DisplayOrder" />
            <span asp-validation-for="DisplayOrder"></span>
        </div>
    </div>
</div>
```

Use the admin tag helpers rather than raw markup, and follow the closest existing `Configure.cshtml` for the store-scope override controls when the plugin is store-scoped.

---

## Checklist

- [ ] `ConfigurationUrl` in `{Feature}Defaults` matches the controller route.
- [ ] All three authorization attributes present, with the correct `PermissionSystemName`.
- [ ] Store scope loaded from `IAdminStoreService.GetActiveStore()` and passed to both `LoadSetting` and `SaveSetting`.
- [ ] `ClearCache()` called after save.
- [ ] Every model field has a resource key registered in `Install()` and removed in `Uninstall()`.
- [ ] `_ViewImports.cshtml` and `_ViewStart.cshtml` present under `Areas/Admin/Views/`.
- [ ] Invalid model state re-renders the form instead of silently saving.
