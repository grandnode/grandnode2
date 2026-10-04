# Plugin — Payment Provider

## Purpose
Create, modify, and review GrandNode payment plugins that implement `IPaymentProvider`.

## When To Use
Use this skill when building a new payment method plugin, changing payment flow, adding capture/refund/void support, implementing a redirect gateway, or reviewing payment provider correctness and security.

## When Not To Use
Do not use this skill for order processing business logic outside the payment flow, or for general plugin infrastructure; combine with `plugin-module` when foundational setup is needed.

## Inputs Required
- Repository root.
- Payment method type: Standard (inline form) or Redirection (external gateway).
- Gateway or PSP API being integrated.
- Which operations must be supported: capture, refund, partial refund, void.
- Whether additional handling fees apply.
- Whether a custom checkout form step is required.

## Instructions

### Mandatory Rules

#### Provider Interface
1. Implement `IPaymentProvider` from `Grand.Business.Core.Interfaces.Checkout.Payments`. This extends `IProvider` which requires `ConfigurationUrl`, `SystemName`, `FriendlyName`, `Priority`, `LimitedToStores`, and `LimitedToGroups`.
2. Implement every member. For methods that are not supported, return a result with `result.AddError("Method not supported")` rather than throwing. Required behavior per member:

| Member | Standard payment | Redirect payment |
|---|---|---|
| `InitPaymentTransaction()` | Return `null` (framework creates transaction) | Return `null` |
| `ProcessPayment(tx)` | Set `result.NewPaymentTransactionStatus` (typically `Pending`) | Return empty result; processing happens after redirect |
| `PostProcessPayment(tx)` | Usually empty (`Task.CompletedTask`) | Usually empty |
| `PostRedirectPayment(tx)` | Return `string.Empty` | Return the gateway redirect URL |
| `CanRePostRedirectPayment(tx)` | Return `false` | Return `true` when the user can re-initiate |
| `ValidatePaymentForm(model)` | Validate custom form fields; return error list | Return empty list |
| `SavePaymentInfo(model)` | Save form data to `PaymentTransaction` fields; return updated tx or `null` | Return `null` |
| `GetControllerRouteName()` | Route name of the storefront payment-info controller, or `""` when `SkipPaymentInfo` | Route name or `""` |
| `PaymentMethodType` | `PaymentMethodType.Standard` | `PaymentMethodType.Redirection` |
| `SkipPaymentInfo()` | Return `true` to skip the payment info step | Usually `false` |
| `Description()` | Translatable description shown on checkout | Same |
| `LogoURL` | `"/Plugins/{SystemName}/logo.jpg"` | Same |
| `Capture/Refund/Void/Cancel` | Implement if supported; return error result if not | Same |
| `SupportCapture/Refund/PartialRefund/Void` | Return `true`/`false` matching what is implemented | Same |
| `GetAdditionalHandlingFee(cart)` | Return the calculated fee in the working currency | Same |
| `HidePaymentMethod(cart)` | Return `true` to suppress the method for this cart | Same |

3. Set `TransactionStatus` via `result.NewPaymentTransactionStatus` in `ProcessPayment`. Common values:
   - `TransactionStatus.Pending` — order placed but not yet confirmed.
   - `TransactionStatus.Authorized` — gateway authorized; capture is separate.
   - `TransactionStatus.Paid` — payment fully captured immediately.
4. Resolve `FriendlyName` through `ITranslationService.GetResource(Defaults.FriendlyName)`.
5. Set `LogoURL` to `"/Plugins/{SystemName}/logo.jpg"`.

#### Redirect Payment Flow
6. When `PaymentMethodType == Redirection`: `PostRedirectPayment` returns the URL; the checkout redirects the customer there. The gateway posts back to a return controller action in the plugin.
7. Implement a public storefront controller (`[Area("")]` without `[AuthorizeAdmin]`) as the redirect return handler. The controller receives the gateway callback, updates the `PaymentTransaction`, and redirects to order confirmation.
8. Register the return controller route in `EndpointProvider` or via a convention route in `StartupApplication.Configure`.

#### Operations: Capture, Refund, Void
9. Implement `Capture`, `Refund`, and `Void` only when the gateway supports them. Return `result.AddError("Capture method not supported")` for unsupported operations.
10. Return `SupportCapture() => true` only when `Capture` is actually implemented; same for Refund, PartiallyRefund, and Void. Returning `true` causes the admin UI to show the corresponding action button.
11. In `CancelPayment`, update `paymentTransaction.TransactionStatus = TransactionStatus.Canceled` and persist via `IPaymentTransactionService.UpdatePaymentTransaction`.

#### Project Structure
12. Use `Microsoft.NET.Sdk.Razor` as SDK.
13. Set output path to `..\..\Web\Grand.Web\Plugins\{SystemName}\`.
14. Mark all GrandNode project references `Private="false"`.
15. Include `logo.jpg` with `CopyToOutputDirectory = Always`.

#### Manifest, Defaults, Settings
16. Define `[assembly: PluginInfo(...)]` with `Group = "Payment methods"`.
17. Define `{Plugin}Defaults` with `ProviderSystemName`, `FriendlyName` (resource key), `ConfigurationUrl`, and — for redirect plugins — `ReturnHandlerRouteName`.
18. Define `{Plugin}Settings : ISettings` with at minimum `DisplayOrder`, `AdditionalFee`, `AdditionalFeePercentage`, and `SkipPaymentInfo`. Add gateway-specific fields (API keys, endpoint URLs).

#### Startup and Install
19. Register the provider:
    ```csharp
    services.AddScoped<IPaymentProvider, YourPaymentProvider>();
    ```
20. In `Install()`: save default settings, register localization keys. Call `base.Install()` last.
21. In `Uninstall()`: delete settings, remove resource keys. Call `base.Uninstall()` last.

#### Admin Configuration
22. Create an admin controller with `[AuthorizeAdmin]`, `[Area("Admin")]`, and `[PermissionAuthorize(PermissionSystemName.PaymentMethods)]`.
23. Use `IAdminStoreService.GetActiveStore()`, `LoadSetting`, `SaveSetting`, `ClearCache` for store-scoped config.

### Security Requirements
24. Never log or store raw card numbers, CVVs, or full PANs. Store only tokens, last-4, and transaction IDs from the gateway.
25. Never construct redirect return URLs from unvalidated query parameters; validate the gateway callback signature or token before updating the transaction.
26. Store gateway API keys in settings via `ISettingService`, not in source code or configuration files.

### Recommendations
1. Prefer `Payments.CashOnDelivery` as a template for Standard payment and `Payments.StripeCheckout` for Redirect payment.
2. Prefer returning `TransactionStatus.Pending` from `ProcessPayment` for Standard methods unless the gateway confirms synchronously.
3. Prefer `GetAdditionalHandlingFee` for fixed or percentage fees; use `IOrderCalculationService.GetShoppingCartSubTotal` for percentage-based calculation.

## Key Contracts

### ProcessPaymentResult
```csharp
public bool          Success                      { get; }           // no errors
public List<string>  Errors                       { get; }
public TransactionStatus NewPaymentTransactionStatus { get; set; }  // default Pending
public double        PaidAmount                   { get; set; }
result.AddError("message");
```

### TransactionStatus enum (key values)
```csharp
Pending = 0, Authorized = 10, PartialPaid = 15, Paid = 20,
PartiallyRefunded = 25, Refunded = 30, Voided = 40, Canceled = 50
```

### PaymentMethodType enum
```csharp
Standard    = 1   // inline checkout form
Redirection = 2   // customer sent to external gateway
Other       = 3
```

### CapturePaymentResult / RefundPaymentResult / VoidPaymentResult
```csharp
result.AddError("message");
result.CaptureTransactionId = "...";      // CapturePaymentResult only
result.NewPaymentStatus = TransactionStatus.Paid;
```

### RefundPaymentRequest
```csharp
PaymentTransaction PaymentTransaction { get; }
double AmountToRefund { get; }
bool IsPartialRefund { get; }
```

## File Locations

| Concern | Path |
|---|---|
| IPaymentProvider | `src/Business/Grand.Business.Core/Interfaces/Checkout/Payments/IPaymentProvider.cs` |
| ProcessPaymentResult | `src/Business/Grand.Business.Core/Utilities/Checkout/ProcessPaymentResult.cs` |
| CapturePaymentResult | `src/Business/Grand.Business.Core/Utilities/Checkout/CapturePaymentResult.cs` |
| RefundPaymentResult / RefundPaymentRequest | `src/Business/Grand.Business.Core/Utilities/Checkout/` |
| VoidPaymentResult | `src/Business/Grand.Business.Core/Utilities/Checkout/VoidPaymentResult.cs` |
| PaymentTransaction entity | `src/Core/Grand.Domain/Payments/PaymentTransaction.cs` |
| TransactionStatus enum | `src/Core/Grand.Domain/Payments/TransactionStatus.cs` |
| PaymentMethodType enum | `src/Business/Grand.Business.Core/Enums/Checkout/PaymentMethodType.cs` |
| IPaymentTransactionService | `src/Business/Grand.Business.Core/Interfaces/Checkout/Payments/IPaymentTransactionService.cs` |
| Example — Standard | `src/Plugins/Payments.CashOnDelivery/` |
| Example — Redirect | `src/Plugins/Payments.StripeCheckout/` |
| Example — Advanced | `src/Plugins/Payments.BrainTree/` |

## Validation Checklist
- [ ] All `IPaymentProvider` and `IProvider` members implemented.
- [ ] Methods that are not supported return `result.AddError(...)` not throw.
- [ ] `SupportCapture/Refund/PartialRefund/Void` match actual implementation.
- [ ] `PaymentMethodType` set correctly (Standard vs Redirection).
- [ ] Redirect plugins have a return controller action that validates the gateway callback.
- [ ] No raw card data stored or logged.
- [ ] Gateway secrets stored via `ISettingService`, not hardcoded.
- [ ] Output path set to `Grand.Web/Plugins/{SystemName}/`.
- [ ] `Install` saves settings and resource keys; `Uninstall` cleans them up.
- [ ] Admin controller uses `[PermissionAuthorize(PermissionSystemName.PaymentMethods)]`.

## Examples

### Example 1: Standard Payment (Invoice/COD)
Set `PaymentMethodType.Standard`. `ProcessPayment` returns `TransactionStatus.Pending`. `PostRedirectPayment` returns `""`. All capture/refund/void methods return an error result. `SkipPaymentInfo` driven from settings.

### Example 2: Redirect Gateway (e.g., Stripe Checkout)
Set `PaymentMethodType.Redirection`. `ProcessPayment` returns an empty result. `PostRedirectPayment` calls the gateway SDK to create a session and returns the session URL. Implement a public return controller that verifies the webhook/callback and calls `IPaymentTransactionService.UpdatePaymentTransaction`.

### Example 3: Authorize-and-Capture Gateway
Set `PaymentMethodType.Standard`. `ProcessPayment` calls the gateway authorize API and sets `TransactionStatus.Authorized`. Implement `Capture` to charge the authorization. Return `SupportCapture() => true`. Implement `Void` to cancel the authorization.
