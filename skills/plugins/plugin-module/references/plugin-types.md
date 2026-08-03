# GrandNode Plugin Types

## Existing Plugin Inventory
- `Authentication.Facebook`
- `Authentication.Google`
- `DiscountRules.Standard`
- `ExchangeRate.McExchange`
- `Payments.BrainTree`
- `Payments.CashOnDelivery`
- `Payments.StripeCheckout`
- `Shipping.ByWeight`
- `Shipping.FixedRateShipping`
- `Shipping.ShippingPoint`
- `Tax.CountryStateZip`
- `Tax.FixedRate`
- `Theme.Modern`
- `Widgets.FacebookPixel`
- `Widgets.GoogleAnalytics`
- `Widgets.Slider`

## Common Plugin Structure
Use this structure unless the closest existing plugin uses a narrower shape:

- `{SystemName}.csproj`
- `Manifest.cs`
- `{Feature}Defaults.cs`
- `{Feature}Plugin.cs`
- `{Feature}Provider.cs`
- `{Feature}Settings.cs`, when settings are persisted
- `StartupApplication.cs` or `Infrastructure/StartupApplication.cs`
- `Areas/Admin/Controllers`, when admin configuration is required
- `Areas/Admin/Views`, when admin configuration uses Razor
- `Controllers`, `Views`, `Components`, or `EndpointProvider.cs`, when public UI or routes are required
- `logo.jpg`, when the plugin appears in plugin lists

## Manifest Rules
Create `Manifest.cs` with an assembly-level `PluginInfo` attribute:

```csharp
[assembly: PluginInfo(
    FriendlyName = "Friendly name",
    Group = "Plugin group",
    SystemName = Defaults.ProviderSystemName,
    Author = "grandnode team",
    Version = "1.0.0"
)]
```

Use existing group names when possible:
- `Payment methods`
- `Shipping rate`
- `Tax providers`
- `Widgets`
- `External authentication`
- `Discount rules`
- `Exchange rate`
- `Themes`

## Project Rules
Use `Microsoft.NET.Sdk.Razor` when the plugin contains Razor views.

Set Debug and Release output paths to:

```xml
<OutputPath>..\..\Web\Grand.Web\Plugins\{SystemName}\</OutputPath>
<OutDir>$(OutputPath)</OutDir>
```

Use central package versions. Add `<PackageReference Include="PackageName" />` without a version when the package is listed in `Directory.Packages.props`.

Set shared GrandNode project references to `Private=false` or the local equivalent used by the nearest plugin.

Copy plugin-owned static assets with `CopyToOutputDirectory` when needed.

## Startup Rules
Register providers and plugin services in `StartupApplication : IStartupApplication`.

Use scoped registration for providers unless the closest existing plugin uses a different lifetime:

```csharp
services.AddScoped<IPaymentProvider, ExamplePaymentProvider>();
```

Keep:
- `Priority => 10` unless startup order matters.
- `BeforeConfigure => false` unless middleware must run before normal configuration.
- Empty `Configure` unless middleware or endpoints are required.

## Install And Uninstall Rules
Derive the plugin class from `BasePlugin` when installation or configuration behavior is needed.

In `Install`:
- Save default settings with `ISettingService`.
- Add plugin-owned resources with `IPluginTranslateResource`.
- Call `base.Install()` last.

In `Uninstall`:
- Delete plugin-owned settings.
- Delete plugin-owned resources.
- Call `base.Uninstall()` last.

## Provider Contract
Every provider that implements `IProvider` must define:
- `ConfigurationUrl`
- `SystemName`
- `FriendlyName`
- `Priority`
- `LimitedToStores`
- `LimitedToGroups`

Return the provider system name from a defaults constant. Keep manifest, provider, settings keys, route names, and plugin output folder aligned.

## Payment Plugins
Use for checkout payment methods.

Template examples:
- `Payments.StripeCheckout` for redirect payment.
- `Payments.CashOnDelivery` for simple offline payment.
- `Payments.BrainTree` for gateway integration.

Implement:
- `IPaymentProvider`
- `{Feature}PaymentPlugin : BasePlugin, IPlugin`
- `{Feature}PaymentProvider`
- settings class
- configuration controller and view when configurable
- payment info route or redirect route when needed

Check:
- `PaymentMethodType`
- `ProcessPayment`
- `PostProcessPayment`
- `PostRedirectPayment`
- `ValidatePaymentForm`
- `SavePaymentInfo`
- `Capture`, `Refund`, `Void`, and support flags
- `CanRePostRedirectPayment`
- `LogoURL`
- webhook or callback idempotency when external gateways are used
- transaction status transitions and order lookup safety

Use the payment review skill for gateway correctness.

## Shipping Plugins
Use for shipping rate calculation and pickup point logic.

Template examples:
- `Shipping.FixedRateShipping`
- `Shipping.ByWeight`
- `Shipping.ShippingPoint`

Implement:
- `IShippingRateCalculationProvider`
- plugin class deriving from `BasePlugin`
- settings or rate models when rates are configurable
- admin configuration controller and view when needed

Check:
- country, state, warehouse, pickup, store, currency, and customer group behavior
- free shipping and shipping restriction interactions
- deterministic rate ordering
- handling of empty carts and non-shippable products

## Tax Plugins
Use for tax rate calculation.

Template examples:
- `Tax.FixedRate`
- `Tax.CountryStateZip`

Implement:
- `ITaxProvider`
- plugin class deriving from `BasePlugin`
- settings or rate service when rates are configurable
- admin or store configuration views when needed

Check:
- `TaxRequest`
- store-specific rates
- country, state, zip, tax category, and customer tax context
- default rate fallback
- precision and rounding expectations

## Widget Plugins
Use for public or admin widgets rendered in widget zones.

Template examples:
- `Widgets.GoogleAnalytics`
- `Widgets.FacebookPixel`
- `Widgets.Slider`

Implement:
- `IWidgetProvider`
- plugin class deriving from `BasePlugin`
- view component and view
- settings and consent cookie when tracking or analytics are used
- widget zones returned by `GetWidgetZones`

Check:
- exact widget zone names
- script injection safety
- consent behavior
- store and customer group limitations
- asset copy behavior

## Authentication Plugins
Use for external login providers.

Template examples:
- `Authentication.Google`
- `Authentication.Facebook`

Implement:
- `IExternalAuthenticationProvider`
- plugin class deriving from `BasePlugin`
- authentication builder or endpoint provider when callback routes are required
- public view component for login button
- admin configuration controller and view

Check:
- callback route names
- client ID and secret storage
- failed login handling
- account linking behavior
- event consumers that send registration or welcome messages

Use the security review skill for OAuth, callbacks, secrets, and account linking.

## Discount Rule Plugins
Use for discount requirement rules.

Template example:
- `DiscountRules.Standard`

Implement:
- `IDiscountProvider`
- one or more `IDiscountRule` implementations
- configuration controllers deriving from the existing discount rule base controller
- configuration views for each rule
- endpoint provider when routes are needed

Check:
- `CheckRequirement`
- `GetConfigurationUrl`
- discount rule system names
- metadata parsing
- store, customer group, product, cart, and order scope
- permission checks for discount management

## Exchange Rate Plugins
Use for currency exchange rate providers.

Template example:
- `ExchangeRate.McExchange`

Implement:
- `IExchangeRateProvider`
- provider-specific rate fetchers when multiple upstream sources exist
- plugin class deriving from `BasePlugin` when install state is enough

Check:
- primary exchange currency support
- HTTP client usage
- upstream failure behavior
- currency code normalization
- duplicate or stale rates

## Theme Plugins
Use for storefront theme replacement or extension.

Template example:
- `Theme.Modern`

Implement:
- theme plugin class deriving from `BasePlugin`
- theme view registration class when required by the existing theme pattern
- views under `Views/{ThemeName}`
- content under `Content`

Check:
- view path and `_ViewStart.cshtml`
- shared layout and component overrides
- static asset copy behavior
- responsive behavior
- compatibility with storefront models and components

