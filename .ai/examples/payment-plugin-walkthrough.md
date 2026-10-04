# Example: Payment Plugin, File by File

Source: `src/Plugins/Payments.CashOnDelivery/`

The smallest complete GrandNode plugin that still has every part: manifest, defaults, settings, provider, plugin lifecycle, DI registration, storefront controller, and an admin configuration screen. Use it as the reference when scaffolding any plugin, not just payment ones.

Read `.ai/skills/plugin-payment.md` for the payment-specific contract, and `.ai/templates/plugin/` for the copy-ready skeleton.

---

## The tree

```
Payments.CashOnDelivery/
  Payments.CashOnDelivery.csproj
  Manifest.cs
  CashOnDeliveryPaymentDefaults.cs
  CashOnDeliveryPaymentSettings.cs
  CashOnDeliveryPaymentProvider.cs
  CashOnDeliveryPaymentPlugin.cs
  StartupApplication.cs
  EndpointProvider.cs
  logo.jpg
  Controllers/PaymentCashOnDeliveryController.cs      ← storefront
  Models/ConfigurationModel.cs
  Models/PaymentInfoModel.cs
  Views/
  Areas/Admin/Controllers/PaymentCashOnDeliveryController.cs
  Areas/Admin/Views/_ViewImports.cshtml
  Areas/Admin/Views/_ViewStart.cshtml
  Areas/Admin/Views/PaymentCashOnDelivery/Configure.cshtml
```

Note the two controllers with the same name in different namespaces — one storefront, one admin. That is the convention, not an accident.

## 1. Identity lives in one place

```csharp
public static class CashOnDeliveryPaymentDefaults
{
    public const string ProviderSystemName = "Payments.CashOnDelivery";
    public const string FriendlyName = "Payments.CashOnDelivery.FriendlyName";
    public const string ConfigurationUrl = "/Admin/PaymentCashOnDelivery/Configure";
}
```

Three constants, referenced everywhere else:

- `ProviderSystemName` → the manifest's `SystemName`, the provider's `SystemName`, and the output folder. All three must agree; the system name is the plugin's persisted identity and cannot change after release.
- `FriendlyName` is a **resource key**, not a display string. The provider resolves it through `ITranslationService`.
- `ConfigurationUrl` must match the admin controller's actual route, or the admin's *Configure* link 404s.

The manifest points back at the constant:

```csharp
[assembly: PluginInfo(
    FriendlyName = "Cash On Delivery (COD)",
    Group = "Payment methods",
    SystemName = CashOnDeliveryPaymentDefaults.ProviderSystemName,
    Author = "grandnode team",
    Version = "1.0.0")]
```

## 2. The provider: `IProvider` members first

```csharp
public class CashOnDeliveryPaymentProvider : IPaymentProvider
{
    public CashOnDeliveryPaymentProvider(
        ITranslationService translationService,
        IHttpContextAccessor httpContextAccessor,
        CashOnDeliveryPaymentSettings cashOnDeliveryPaymentSettings)
    { … }

    public string ConfigurationUrl => CashOnDeliveryPaymentDefaults.ConfigurationUrl;
    public string SystemName => CashOnDeliveryPaymentDefaults.ProviderSystemName;
    public string FriendlyName => _translationService.GetResource(CashOnDeliveryPaymentDefaults.FriendlyName);
    public int Priority => _cashOnDeliveryPaymentSettings.DisplayOrder;
    public IList<string> LimitedToStores => new List<string>();
    public IList<string> LimitedToGroups => new List<string>();
    …
}
```

Points worth copying:

- **The settings class is injected directly.** GrandNode registers `ISettings` implementations in DI — do not resolve them through `ISettingService` inside a provider.
- **`FriendlyName` goes through `ITranslationService`.** A literal here cannot be translated and cannot be changed by an operator.
- **`Priority` comes from `DisplayOrder`**, so operators control ordering from the admin screen.
- **`LimitedToStores` / `LimitedToGroups`** are the scoping hooks from `.ai/knowledge/scoping.md`. Returning empty lists means "available everywhere".

The payment-specific members follow (`ProcessPayment`, `PostProcessPayment`, `PostRedirectPayment`, …). COD is the degenerate case — it returns `TransactionStatus.Pending` and does nothing else, which makes the surrounding structure easy to see.

## 3. The plugin: install is a contract with uninstall

```csharp
public class CashOnDeliveryPaymentPlugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    public override string ConfigurationUrl() => CashOnDeliveryPaymentDefaults.ConfigurationUrl;

    public override async Task Install()
    {
        var settings = new CashOnDeliveryPaymentSettings { DescriptionText = "…" };
        await settingService.SaveSetting(settings);

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Payments.CashOnDelivery.FriendlyName", "Cash on delivery");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Payment.CashOnDelivery.DescriptionText", "Description");
        // … one Add per resource key, plus a .Hint for each admin field
        await base.Install();
    }

    public override async Task Uninstall()
    {
        await settingService.DeleteSetting<CashOnDeliveryPaymentSettings>();
        await pluginTranslateResource.DeletePluginTranslationResource(
            "Plugins.Payment.CashOnDelivery.DescriptionText");
        // … one Delete per key Install added
        await base.Uninstall();
    }
}
```

- Primary constructor syntax — the codebase uses it for plugin classes.
- `base.Install()` / `base.Uninstall()` mark the plugin installed/uninstalled and are called **last**.
- **Every key added in `Install` should be removed in `Uninstall`.** In the shipped file `Uninstall` misses `DisplayOrder` and `FriendlyName` — an example of the drift this rule exists to prevent. Do not copy that gap.
- Admin fields get two keys each: the label and a `.Hint`.

## 4. Registration

```csharp
public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPaymentProvider, CashOnDeliveryPaymentProvider>();
    }

    public int Priority => 10;
    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment) { }
    public bool BeforeConfigure => false;
}
```

Discovered by assembly scanning — nothing in `Program.cs` or `Grand.Web` mentions this plugin. `Priority => 10` is the plugin convention. `Configure` stays empty because the plugin owns no middleware; routes come from `EndpointProvider.cs` instead.

## 5. Admin configuration: store scope is the whole point

```csharp
[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentCashOnDeliveryController : BasePaymentController
{
    public async Task<IActionResult> Configure()
    {
        var storeScope = await _adminStoreService.GetActiveStore();
        var settings = await _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(storeScope);
        var model = new ConfigurationModel { … , ActiveStore = storeScope };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var storeScope = await _adminStoreService.GetActiveStore();
        var settings = await _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(storeScope);
        settings.DescriptionText = model.DescriptionText;
        // …
        await _settingService.SaveSetting(settings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));
        return await Configure();
    }
}
```

Five things that are each a bug when omitted:

1. All three attributes. Without `[PermissionAuthorize]` any admin can reconfigure payments.
2. `GetActiveStore()` on **both** GET and POST. Saving without the scope overwrites the global value for every store.
3. `LoadSetting` before mutating, so untouched properties are not reset to type defaults.
4. `ClearCache()` after saving, or the storefront keeps the old settings.
5. Invalid model state re-renders the form rather than saving partial data.

Views under `Areas/Admin/Views/` need their own `_ViewImports.cshtml` (tag helpers + `@inject LocService Loc`) and a `_ViewStart.cshtml` with `Layout = ""` — the admin supplies the shell.

## 6. Project file

`Microsoft.NET.Sdk.Razor` with `AddRazorSupportForMvc=true` and `StaticWebAssetsEnabled=false`; output path set for **both** Debug and Release to `..\..\Web\Grand.Web\Plugins\Payments.CashOnDelivery\`; every GrandNode reference `<Private>false</Private>`, with `<ExcludeAssets>all</ExcludeAssets>` on `Grand.Web.Common`; `logo.jpg` copied to output.

A Release output path missing means the plugin silently disappears from release builds.

## Trace: what happens at runtime

1. Host starts → assembly scan finds `StartupApplication` → `IPaymentProvider` registered.
2. Plugin marked installed → `Install()` seeded settings and resource keys.
3. Checkout asks for available payment providers → this one appears, ordered by `Priority`, filtered by `LimitedToStores` / `LimitedToGroups`.
4. Its name renders through `FriendlyName` → `ITranslationService` → the resource key installed in step 2.
5. Operator clicks *Configure* → `ConfigurationUrl` → admin controller → settings loaded for the active store.
6. Customer selects it → `ProcessPayment` → `TransactionStatus.Pending`.
