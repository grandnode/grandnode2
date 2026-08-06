# Plugin — Shipping Rate Provider

## Purpose
Create, modify, and review GrandNode shipping rate calculation plugins that implement `IShippingRateCalculationProvider`.

## When To Use
Use this skill when building a new shipping rate plugin, changing rate calculation logic, adding a custom shipping form, or reviewing an existing shipping provider for correctness.

## When Not To Use
Do not use this skill for shipping method domain data (managed via `IShippingMethodService` in the Admin), for free-shipping logic, or for general plugin infrastructure; combine with `plugin-module` when foundational plugin setup is needed.

## Inputs Required
- Repository root.
- Closest existing shipping plugin to use as template.
- Rate calculation approach: fixed rate, weight-based, real-time API.
- Whether a custom checkout form step is required.
- Required admin configuration settings.

## Instructions

### Mandatory Rules

#### Provider Interface
1. Implement `IShippingRateCalculationProvider` from `Grand.Business.Core.Interfaces.Checkout.Shipping`. This extends `IProvider` which requires `ConfigurationUrl`, `SystemName`, `FriendlyName`, `Priority`, `LimitedToStores`, and `LimitedToGroups`.
2. Implement all `IShippingRateCalculationProvider` members:

| Member | Requirement |
|---|---|
| `GetShippingOptions(request)` | **Required.** Build and return a `GetShippingOptionResponse` with one `ShippingOption` per available method. Add errors to `response.AddError(msg)` instead of throwing. |
| `GetFixedRate(request)` | Return a known rate when a single rate can be computed before checkout, or `null` when rates vary. Used for cart estimate display. |
| `HideShipmentMethods(cart)` | Return `true` to suppress this provider during checkout for this cart (e.g., no shippable items). Return `false` otherwise. |
| `ValidateShippingForm(option, data)` | Return a list of validation error strings for any custom form submitted during the shipping step. Return empty list when no custom form is used. |
| `GetControllerRouteName()` | Return the public route name of a storefront controller that renders a custom shipping form, or empty string when no form is needed. |
| `ShippingRateCalculationType` | `ShippingRateCalculationType.Off` for pre-configured rates; `ShippingRateCalculationType.Real` for live API rates. |
| `ShipmentTracker` | Return `null` if shipment tracking is not supported; return an `IShipmentTracker` implementation otherwise. |

3. Resolve `FriendlyName` through `ITranslationService.GetResource(Defaults.FriendlyName)` so it is localizable.
4. Resolve `Priority` from the plugin settings `DisplayOrder` property.
5. Use `IShippingMethodService.GetAllShippingMethods(restrictByCountryId, customer, storeId)` to iterate over admin-configured methods and build a `ShippingOption` for each.
6. Set `ShippingOption.Name` and `ShippingOption.Description` using `shippingMethod.GetTranslation(x => x.Name, languageId)`.
7. Convert rates to the working currency using `ICurrencyService.ConvertFromPrimaryStoreCurrency(rate, workingCurrency)`.

#### Project Structure
8. Use `Microsoft.NET.Sdk.Razor` as the project SDK even when no Razor views are present, for consistency with existing shipping plugins.
9. Set both `<OutputPath>` and `<OutDir>` to `..\..\Web\Grand.Web\Plugins\{SystemName}\`.
10. Mark all GrandNode project references as `Private="false"` so they are not copied to the plugin output folder.
11. Include `logo.jpg` with `<CopyToOutputDirectory>Always</CopyToOutputDirectory>`.

#### Manifest, Defaults, Settings
12. Define the assembly-level `[assembly: PluginInfo(...)]` in `Manifest.cs` with `Group = "Shipping rate"`.
13. Define a `{Plugin}Defaults` class with `ProviderSystemName`, `FriendlyName` (resource key), and `ConfigurationUrl` constants.
14. Define `{Plugin}Settings : ISettings` with at minimum a `DisplayOrder` property. Add any plugin-specific rate settings here.

#### Startup and Install
15. Register the provider in `StartupApplication.ConfigureServices`:
    ```csharp
    services.AddScoped<IShippingRateCalculationProvider, YourShippingProvider>();
    ```
    Register any additional plugin services (repositories, calculation services) in the same method.
16. Implement `Install()` in the plugin class: save default settings with `ISettingService.SaveSetting` and register localization keys with `IPluginTranslateResource.AddOrUpdatePluginTranslateResource`. Call `base.Install()` last.
17. Implement `Uninstall()`: delete settings with `ISettingService.DeleteSetting<T>()` and remove resource keys. Call `base.Uninstall()` last.

#### Admin Configuration
18. Create an admin controller with `[AuthorizeAdmin]`, `[Area("Admin")]`, and `[PermissionAuthorize(PermissionSystemName.ShippingSettings)]`.
19. Use `IAdminStoreService.GetActiveStore()` and `ISettingService.LoadSetting<T>(storeScope)` / `SaveSetting<T>(settings, storeScope)` / `ClearCache()` for store-scoped settings.

### Recommendations
1. Prefer `Shipping.FixedRateShipping` as a template for simple pre-configured rates and `Shipping.ByWeight` for weight-based calculation.
2. Return a meaningful error string via `response.AddError(...)` rather than returning an empty response when the provider cannot compute rates.
3. Prefer returning `null` from `GetFixedRate` when different methods have different rates — this prevents displaying a misleading single price in cart estimates.

## Key Contracts

### GetShippingOptionResponse / ShippingOption
```csharp
// ShippingOption fields relevant to populate:
public string Name { get; set; }
public string Description { get; set; }
public double Rate { get; set; }
public string ShippingRateProviderSystemName { get; set; }  // set automatically by framework

// GetShippingOptionResponse helpers:
response.AddError("message");               // records a non-fatal error
response.ShippingOptions.Add(option);
```

### GetShippingOptionRequest (key fields)
```csharp
Customer Customer { get; }
IList<PackageItem> Items { get; }           // wraps ShoppingCartItem
Address ShippingAddress { get; }
```

### ShippingRateCalculationType enum
```csharp
Off  = 0   // rate configured via admin; no live API call
Real = 10  // live calculation via external service
```

## File Locations

| Concern | Path |
|---|---|
| IShippingRateCalculationProvider | `src/Business/Grand.Business.Core/Interfaces/Checkout/Shipping/IShippingRateCalculationProvider.cs` |
| IShippingMethodService | `src/Business/Grand.Business.Core/Interfaces/Checkout/Shipping/IShippingMethodService.cs` |
| IShipmentTracker | `src/Business/Grand.Business.Core/Interfaces/Checkout/Shipping/IShipmentTracker.cs` |
| GetShippingOptionRequest/Response | `src/Business/Grand.Business.Core/Utilities/Checkout/` |
| ShippingOption | `src/Core/Grand.Domain/Shipping/ShippingOption.cs` |
| ShippingRateCalculationType | `src/Business/Grand.Business.Core/Enums/Checkout/ShippingRateCalculationType.cs` |
| Example — fixed rate | `src/Plugins/Shipping.FixedRateShipping/` |
| Example — by weight | `src/Plugins/Shipping.ByWeight/` |
| Example — shipping point | `src/Plugins/Shipping.ShippingPoint/` |

## Validation Checklist
- [ ] All `IShippingRateCalculationProvider` and `IProvider` members implemented.
- [ ] `GetShippingOptions` returns at least one option or an error, never throws.
- [ ] Rates converted to working currency via `ICurrencyService`.
- [ ] `IShippingMethodService.GetAllShippingMethods` used with country and store scope.
- [ ] Output path set to `Grand.Web/Plugins/{SystemName}/`.
- [ ] Provider registered with `AddScoped<IShippingRateCalculationProvider, ...>`.
- [ ] `Install` saves settings and resource keys; `Uninstall` cleans them up.
- [ ] Admin controller uses `[PermissionAuthorize(PermissionSystemName.ShippingSettings)]`.

## Examples

### Example 1: Fixed Rate per Method
Pattern from `Shipping.FixedRateShipping`: load all `ShippingMethod` records, look up a per-method setting key (`"ShippingRateComputationMethod.FixedRate.Rate.ShippingMethodId{id}"`), create one `ShippingOption` per method with the stored rate, convert currency.

### Example 2: Flat Rate Plugin
Create a single `ShippingOption` with a hard-coded or settings-driven flat rate, returning it for all carts regardless of method or weight.

### Example 3: API-Based Real-Time Rate
Set `ShippingRateCalculationType = Real`. In `GetShippingOptions`, call an external carrier API, map each returned service to a `ShippingOption`, and add errors if the API is unavailable.
