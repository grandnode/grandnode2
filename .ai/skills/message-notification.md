# Message and Notification

## Purpose
Create, modify, and review GrandNode email message templates, DotLiquid tokens, message sending flows, queued email lifecycle, and domain event notification handlers.

## When To Use
Use this skill when adding or changing a message template, adding new DotLiquid tokens or drop properties, triggering a new email notification from business code, seeding message templates in the installer, extending an existing `Liquid*` drop class, or wiring a mediator event handler that sends a notification.

## When Not To Use
Do not use this skill for general admin UI work, settings, or localization changes beyond message template resource keys; combine it with `admin-area-changes` or `settings-and-localization` when those are also involved.

Do not use this skill as the primary review for MongoDB queries or security; combine with the relevant skill when those concerns apply.

## Inputs Required
- Repository root.
- Trigger event or business action that should send the notification.
- Recipient type: customer, store owner, vendor, or arbitrary address.
- Domain entities whose data must appear in the template body.
- Whether the template is new or an existing one is being updated.
- Required subject line and HTML body structure.

## Instructions

### Mandatory Rules

#### Message Templates
1. Define the template name as a `public const string` in `MessageTemplateNames` at `src/Business/Grand.Business.Messages/Services/MessageTemplateNames.cs`. Follow the existing `Subject.RecipientNotification` naming convention.
2. Seed the default template in `src/Modules/Grand.Module.Installer/Services/InstallDataMessageTemplates.cs` inside `InstallMessageTemplates()`. Every seeded template must reference a default email account, set `IsActive = true`, and include a valid DotLiquid `Subject` and `Body`.
3. Keep template `Name` consistent with the constant defined in `MessageTemplateNames`. Code that sends the template looks it up by this name.
4. Use `ITranslationEntity.Locales` on `MessageTemplate` for per-language subject and body overrides. Never store language-specific copies as separate documents.
5. Only use tokens that are exposed by the relevant `Liquid*` drop classes or by `LiquidStore`. Do not invent tokens that have no backing drop property.
6. Use DotLiquid syntax: `{{Drop.Property}}` for values, `{% if condition %}...{% endif %}` for conditions, `{% for item in Collection %}...{% endfor %}` for loops.

#### Sending Messages
7. Add a typed send method to `IMessageProviderService` in `src/Business/Grand.Business.Core/Interfaces/Messages/IMessageProviderService.cs` when the trigger is a recurring, well-defined business event. Follow the signature pattern:
   ```csharp
   Task<int> SendXxxMessage(TEntity entity, Store store, string languageId);
   ```
8. Implement the method in `MessageProviderService` at `src/Business/Grand.Business.Messages/Services/MessageProviderService.cs`. The standard implementation pattern is:
   1. Get `MessageTemplate` by name using `IMessageTemplateService.GetMessageTemplateByName(name, store.Id)`.
   2. Return 0 if the template is not active.
   3. Resolve `EmailAccount` using the template's `EmailAccountId` and `IEmailAccountService`.
   4. Build tokens with `LiquidObjectBuilder` using the appropriate `AddXxxTokens()` methods.
   5. Call `await builder.Build()` to get the populated `LiquidObject`.
   6. Call `await SendNotification(messageTemplate, emailAccount, languageId, liquidObject, toEmail, toName)`.
9. Use the generic `SendNotification` overload directly when a one-off or plugin-triggered send does not justify a dedicated typed method.

#### DotLiquid Drops and Tokens
10. Add new token properties to an existing `Liquid*` drop class in `src/Business/Grand.Business.Core/Utilities/Messages/DotLiquidDrops/` when the data already belongs to that drop's entity. Do not add properties that require loading additional entities unless those are already available in the drop constructor.
11. Add a new drop class inheriting from `DotLiquid.Drop` only when the new template uses a domain entity that has no existing drop. Place it in the drops folder and wire it via a new `GetXxxTokensCommand` following the pattern of existing token commands under `src/Business/Grand.Business.Core/Commands/Messages/Tokens/`.
12. Handle `MessageTokensAddedEvent` in an `INotificationHandler` to inject extra tokens from a plugin without modifying core drop classes. The event carries the `MessageTemplate` name and the current `LiquidObject`.
13. Never access `ITranslationService`, repositories, or services directly from inside a `Liquid*` drop property getter. Populate all values in the command handler that constructs the drop.

#### Queued Emails
14. All outgoing emails are persisted as `QueuedEmail` documents before they are sent. `SendNotification` queues the email via `IQueuedEmailService.InsertQueuedEmail`. Never call `IEmailSender` directly from business code.
15. Respect `MessageTemplate.DelayBeforeSend` and `MessageTemplate.DelayPeriodId` — `SendNotification` converts these to `QueuedEmail.DontSendBeforeDateUtc`. Do not override or ignore the delay.
16. The scheduled task `QueuedMessagesSendScheduleTask` in `Grand.Module.ScheduledTasks` sends queued emails. Do not introduce an alternative sending path.

#### Domain Events and Handlers
17. Raise domain events via `IMediator.Publish(new XxxEvent(...))` after the triggering business action completes. Do not publish inside a repository or data layer.
18. Implement `INotificationHandler<TEvent>` in the relevant `Grand.Business.*` project when the notification should be sent in response to a domain event.
19. Register event handlers automatically — `Grand.Mediator` scans assemblies registered in startup. No explicit handler registration is required beyond ensuring the assembly is included.

### Recommendations
1. Prefer adding a property to an existing drop over creating a new drop when the entity is already available in the build pipeline.
2. Prefer the `MessageTokensAddedEvent` handler for plugin-specific tokens rather than modifying core drop classes.
3. Prefer reusing `{{Store.Name}}`, `{{Store.URL}}`, and `{{Store.AdminEmail}}` tokens that are always available via `LiquidStore`.
4. Prefer the `Reference` enum field on `QueuedEmail` (e.g., `Reference.Order`, `Reference.Customer`) to link queued emails to the source entity for audit purposes.
5. Prefer testing `MessageProviderService` with a mock `IMessageTemplateService` that returns a pre-built `MessageTemplate` to verify the queuing call.

## Constraints
- Never hardcode email addresses in `SendXxx` methods; always resolve them from `EmailAccount` or from the entity being notified.
- Never bypass `IQueuedEmailService`; do not call SMTP directly from business or web code.
- Never use tokens that are not backed by a drop property — they will render as empty strings.
- Never add rendering logic (formatting, currency, date) to drop property getters unless the same pattern is already used in that drop.
- Never store multiple language copies of a template as separate `MessageTemplate` documents; use the `Locales` collection.
- Never call `IMessageProviderService` from inside a Razor view or a controller action directly — trigger sends from handlers or services.

## Key Contracts

### IMessageProviderService (`src/Business/Grand.Business.Core/Interfaces/Messages/IMessageProviderService.cs`)
Typed send methods follow the pattern:
```csharp
Task<int> SendCustomerRegisteredMessage(Customer customer, Store store, string languageId);
Task<int> SendOrderPlacedCustomerMessage(Order order, Customer customer, Store store, string languageId);
// ... one method per template
```

Generic send:
```csharp
Task<int> SendNotification(
    MessageTemplate messageTemplate, EmailAccount emailAccount,
    string languageId, LiquidObject liquidObject,
    string toEmailAddress, string toName,
    string attachmentFilePath = null, string attachmentFileName = null,
    IEnumerable<string> attachedDownloads = null,
    string replyToEmailAddress = null, string replyToName = null,
    string fromEmail = null, string fromName = null, string subject = null,
    Reference reference = Reference.None, string objectId = "");
```

### LiquidObjectBuilder (`src/Business/Grand.Business.Core/Utilities/Messages/DotLiquidDrops/LiquidObjectBuilder.cs`)
```csharp
var liquidObject = await new LiquidObjectBuilder(_mediator)
    .AddStoreTokens(store, language, emailAccount)
    .AddOrderTokens(order, customer, store, host)
    .AddCustomerTokens(customer, store, host, language)
    .Build();
```

### MessageTokensAddedEvent (`src/Business/Grand.Business.Core/Events/Messages/MessageTokensAddedEvent.cs`)
```csharp
public class MessageTokensAddedEvent : INotification {
    public MessageTemplate Message { get; }
    public LiquidObject LiquidObject { get; }
}
```

## File Locations

| Concern | Path |
|---|---|
| MessageTemplate entity | `src/Core/Grand.Domain/Messages/MessageTemplate.cs` |
| QueuedEmail entity | `src/Core/Grand.Domain/Messages/QueuedEmail.cs` |
| EmailAccount entity | `src/Core/Grand.Domain/Messages/EmailAccount.cs` |
| MessageTemplateNames constants | `src/Business/Grand.Business.Messages/Services/MessageTemplateNames.cs` |
| IMessageProviderService interface | `src/Business/Grand.Business.Core/Interfaces/Messages/IMessageProviderService.cs` |
| MessageProviderService implementation | `src/Business/Grand.Business.Messages/Services/MessageProviderService.cs` |
| IMessageTemplateService interface | `src/Business/Grand.Business.Core/Interfaces/Messages/IMessageTemplateService.cs` |
| IQueuedEmailService interface | `src/Business/Grand.Business.Core/Interfaces/Messages/IQueuedEmailService.cs` |
| IEmailAccountService interface | `src/Business/Grand.Business.Core/Interfaces/Messages/IEmailAccountService.cs` |
| IEmailSender interface | `src/Business/Grand.Business.Core/Interfaces/Messages/IEmailSender.cs` |
| DotLiquid drop classes | `src/Business/Grand.Business.Core/Utilities/Messages/DotLiquidDrops/` |
| LiquidObjectBuilder | `src/Business/Grand.Business.Core/Utilities/Messages/DotLiquidDrops/LiquidObjectBuilder.cs` |
| Token commands | `src/Business/Grand.Business.Core/Commands/Messages/Tokens/` |
| MessageTokensAddedEvent | `src/Business/Grand.Business.Core/Events/Messages/MessageTokensAddedEvent.cs` |
| IMessageTokenProvider interface | `src/Business/Grand.Business.Core/Interfaces/Messages/IMessageTokenProvider.cs` |
| MessageTokenProvider implementation | `src/Business/Grand.Business.Messages/Services/MessageTokenProvider.cs` |
| Installer — template seed | `src/Modules/Grand.Module.Installer/Services/InstallDataMessageTemplates.cs` |
| Send scheduled task | `src/Modules/Grand.Module.ScheduledTasks/BackgroundServices/QueuedMessagesSendScheduleTask.cs` |
| Tests | `src/Tests/Grand.Business.Messages.Tests/Services/` |

## Available Liquid Drops

| Drop class | Token prefix | Entity |
|---|---|---|
| `LiquidStore` | `{{Store.*}}` | Store, EmailAccount |
| `LiquidCustomer` | `{{Customer.*}}` | Customer |
| `LiquidOrder` | `{{Order.*}}`, `{{Order.OrderItems}}` | Order, OrderItem |
| `LiquidShipment` | `{{Shipment.*}}` | Shipment |
| `LiquidProduct` | `{{Product.*}}` | Product |
| `LiquidMerchandiseReturn` | `{{MerchandiseReturn.*}}` | MerchandiseReturn |
| `LiquidGiftVoucher` | `{{GiftVoucher.*}}` | GiftVoucher |
| `LiquidNewsLetterSubscription` | `{{NewsLetterSubscription.*}}` | NewsLetterSubscription |
| `LiquidVendor` | `{{Vendor.*}}` | Vendor |
| `LiquidBlogComment` | `{{BlogComment.*}}` | BlogComment |
| `LiquidNewsComment` | `{{NewsComment.*}}` | NewsComment |
| `LiquidProductReview` | `{{ProductReview.*}}` | ProductReview |
| `LiquidContactUs` | `{{ContactUs.*}}` | ContactUs |
| `LiquidAskQuestion` | `{{AskQuestion.*}}` | AskQuestion |

## Expected Output
Produce one of these results:
- A new or modified message template with matching constant, installer seed, send method, drop properties, and test.
- A review report listing message and notification issues by severity.
- A plugin token extension using `MessageTokensAddedEvent` without modifying core drops.

Include changed files, token list, installer seed status, queuing behavior, and remaining risks.

## Validation Checklist
- [ ] Template name constant added or updated in `MessageTemplateNames`.
- [ ] Template seeded in `InstallDataMessageTemplates` with active flag and email account.
- [ ] Template name matches constant and lookup key in `SendXxx` method.
- [ ] Only tokens backed by existing drop properties are used in subject and body.
- [ ] New drop properties are populated in the command handler, not in the getter.
- [ ] `LiquidObjectBuilder` includes all required `AddXxxTokens()` calls.
- [ ] Send method calls `GetMessageTemplateByName`, resolves `EmailAccount`, builds tokens, and calls `SendNotification`.
- [ ] Early return when template `IsActive == false`.
- [ ] Emails are queued via `IQueuedEmailService`; `IEmailSender` is not called directly.
- [ ] Template delay is not overridden in the send method.
- [ ] Domain event handlers are in the correct `Grand.Business.*` project.
- [ ] Tests cover the send method queuing call and inactive-template early return.

## Examples

### Example 1: New Order Status Email
Input: Send an email to the customer when an order reaches a custom "Ready for pickup" status.

Output:
1. Add `const string SendOrderReadyForPickupCustomerMessage = "OrderReadyForPickup.CustomerNotification"` to `MessageTemplateNames`.
2. Seed the template in `InstallDataMessageTemplates` with subject `"Your order {{Order.OrderNumber}} is ready"` and a body using `{{Order.*}}` and `{{Store.*}}` tokens.
3. Add `Task<int> SendOrderReadyForPickupCustomerMessage(Order order, Customer customer, Store store, string languageId)` to `IMessageProviderService`.
4. Implement the method in `MessageProviderService`: load template by name, return 0 if inactive, build `LiquidObjectBuilder` with `AddStoreTokens` + `AddOrderTokens` + `AddCustomerTokens`, call `SendNotification` with the customer's email.
5. Publish `OrderReadyForPickupEvent` from the order status-change service and handle it in a `INotificationHandler<OrderReadyForPickupEvent>` that calls `_messageProviderService.SendOrderReadyForPickupCustomerMessage(...)`.

### Example 2: Plugin Token Extension
Input: A loyalty plugin needs to include a customer's loyalty points balance in existing order emails.

Output:
1. Create an `INotificationHandler<MessageTokensAddedEvent>` in the plugin.
2. In `Handle`, check `notification.Message.Name` to filter only the relevant templates.
3. Load the loyalty balance for the customer referenced in the existing `LiquidCustomer` drop on `notification.LiquidObject`.
4. Assign a new `LiquidLoyaltyPoints` drop (or a simple string property) to `notification.LiquidObject`.
5. Use `{{LoyaltyPoints.Balance}}` in the template body — it resolves from the drop set on `LiquidObject`.
6. No changes to core drop classes are needed.

### Example 3: Update Existing Template Body
Input: Add the vendor name to the existing shipment sent email.

Output:
1. Confirm `LiquidShipment` or `LiquidVendor` already exposes a vendor name property; if not, add the property to the drop and populate it in `GetShipmentTokensCommand`.
2. Update the `ShipmentSent.CustomerNotification` body in `InstallDataMessageTemplates` with the new token (for new installations).
3. Provide a migration that updates the template body for existing installations in `Grand.Module.Migration`.
