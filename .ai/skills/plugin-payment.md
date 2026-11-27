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
4. Resolve `FriendlyName` through `IRequestTranslationService.GetResource(Defaults.FriendlyName)`.
5. Set `LogoURL` to `"/Plugins/{SystemName}/logo.jpg"`.

#### Redirect Payment Flow
6. When `PaymentMethodType == Redirection`: `PostRedirectPayment` returns the URL; the checkout redirects the customer there. The gateway posts back to a return controller action in the plugin.
7. Implement a public storefront controller (`[Area("")]` without `[AuthorizeAdmin]`) as the redirect return handler. The controller receives the gateway callback, records the money with `MarkAsPaidCommand` (or `MarkAsAuthorizedCommand` for an authorization), and redirects to order confirmation. Pass `CreatedBy` = the plugin's system name, `Reference` = the gateway's payment id, and `Amount` = what the gateway reports it took. Never set `PaidAmount` or `TransactionStatus` and save through `IPaymentTransactionService.UpdatePaymentTransaction` for a money operation: the commands also update the order and write the transaction's payment history (`IPaymentTransactionHistoryService`), a direct save leaves the history empty.
8. Register the return controller route in `EndpointProvider` or via a convention route in `StartupApplication.Configure`.

#### Operations: Capture, Refund, Void
9. Implement `Capture`, `Refund`, and `Void` only when the gateway supports them. Return `result.AddError("Capture method not supported")` for unsupported operations.
10. Return `SupportCapture() => true` only when `Capture` is actually implemented; same for Refund, PartiallyRefund, and Void. Returning `true` causes the admin UI to show the corresponding action button.
11. In `CancelPayment`, update `paymentTransaction.TransactionStatus = TransactionStatus.Canceled` and persist via `IPaymentTransactionService.UpdatePaymentTransaction`.

#### Recurring Payments
- The provider, not the shop, runs the schedule: the gateway charges every cycle, retries failures and reports each cycle to the shop, the first one (the initial order's payment) included: `CyclesPaid` stays 0 until it does. A cart with a recurring product is offered only methods whose `SupportRecurringPayment()` returns `true`, and the order is refused for any other method.
- Return `SupportRecurringPayment() => true` only when the payment flow starts the gateway's schedule for a recurring order and `CancelRecurringPayment` stops it. Both members have defaults (`false`, and an error result), so a provider without recurring support implements neither.
- Never report "cancelled" from `CancelRecurringPayment` unless the gateway confirmed it: on an error result the shop keeps the recurring payment running.
- `CancelRecurringPayment` stops the gateway's schedule for `RecurringPayment.ProviderReference` and returns. The shop cancels through `CancelRecurringPaymentCommand`, which calls it outside the recurring payment's lock and then, under the lock on a fresh read, marks it `Cancelled` only on success; a throw counts as a refusal. Webhooks for the payment are not held up by the call, and a cycle recorded meanwhile is kept. A `Pending` payment with no `ProviderReference` is cancelled without the call: it has no reference to stop yet, the gateway may have started the subscription at checkout already, and the first cycle or failure it reports on the cancelled payment makes the shop stop the gateway, so send `ProviderReference` with the failure command as well. A cycle charged after the payment was cancelled or completed is noted on the initial order for staff to refund.
- A provider reports to the shop with three mediator commands (`Grand.Business.Core.Commands.Checkout.Orders`): `RecordRecurringPaymentCycleCommand` (a cycle was paid; the shop places the cycle order from the second cycle on and, whenever the recurring payment is completed or cancelled after the cycle is recorded, calls `CancelRecurringPayment` to stop the gateway), `RecordRecurringPaymentFailureCommand` and `RecordRecurringPaymentEndedCommand` (the gateway ended it).
- Pass `RecurringPayment.Id` to the gateway as metadata when the schedule starts, and send it back as `RecurringPaymentId` in every command. The webhook must verify the gateway signature before sending any of them.
- `CycleReference` is the gateway's invoice or charge id, so a resent webhook is recorded once and the command returns `false`. `ProviderReference` is the subscription id.
- `Amount` uses the units of `MarkAsPaidCommand.Amount`: the order total in the order's currency, as Stripe Checkout passes it (`paymentIntent.Amount / 100.0`).
- The first paid cycle is the initial order's own payment: the provider still marks that order paid on its existing path and also sends the cycle command, which records cycle 1 and places no order. If placing a later cycle's order throws, the command notes it on the initial order for staff (the order may exist but be unpaid), stops the gateway when the payment is completed, and rethrows so the provider can log or report it. `CycleReference` is required, and `Amount` must not be negative. The cycle is already recorded, so a resend of the same `CycleReference` returns `false` and does not retry the order. A cycle the gateway charged less for is recorded as a partial payment, so its order stays partially paid, and a cycle of 0 records no payment; an order note says so.

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
3. Prefer `GetAdditionalHandlingFee` for fixed or percentage fees; use `IShoppingCartSubTotalService.GetShoppingCartSubTotal` for percentage-based calculation (a provider must not depend on `IOrderCalculationService`: it depends on `IPaymentService`, which loads every provider).

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
Set `PaymentMethodType.Redirection`. `ProcessPayment` returns an empty result. `PostRedirectPayment` calls the gateway SDK to create a session and returns the session URL. Implement a public return controller that verifies the webhook/callback and sends `MarkAsPaidCommand` with the gateway's payment id and amount (see `StripeCheckoutService.CreatePaymentTransaction`).

### Example 3: Authorize-and-Capture Gateway
Set `PaymentMethodType.Standard`. `ProcessPayment` calls the gateway authorize API and sets `TransactionStatus.Authorized`. Implement `Capture` to charge the authorization. Return `SupportCapture() => true`. Implement `Void` to cancel the authorization.
