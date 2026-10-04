# Standard: Naming

Binding naming rules derived from the existing GrandNode tree. When a rule below conflicts with the closest existing file, follow the existing file and say so.

---

## Projects

| Kind | Pattern | Example |
|---|---|---|
| Core | `Grand.{Concern}` | `Grand.Domain`, `Grand.Data`, `Grand.Infrastructure` |
| Business | `Grand.Business.{Area}` | `Grand.Business.Catalog`, `Grand.Business.Checkout` |
| Web | `Grand.Web`, `Grand.Web.{Area}` | `Grand.Web.Admin`, `Grand.Web.Store`, `Grand.Web.Vendor` |
| Module | `Grand.Module.{Name}` | `Grand.Module.Api`, `Grand.Module.Migration` |
| Plugin | `{Group}.{Name}` | `Payments.StripeCheckout`, `Widgets.Slider`, `Theme.Modern` |
| Tests | `{SourceProject}.Tests` | `Grand.Business.Catalog.Tests` |

Plugin group prefixes are fixed: `Payments`, `Shipping`, `Tax`, `Widgets`, `Authentication`, `DiscountRules`, `ExchangeRate`, `Theme`.

## Types

| Kind | Pattern | Notes |
|---|---|---|
| Domain entity | `{Noun}` | `Product`, `Order`, `Customer` — no suffix |
| Settings | `{Area}Settings` | implements `ISettings` |
| Service interface | `I{Noun}Service` | `IOrderService` |
| Service | `{Noun}Service` | one implementation per interface unless a provider set |
| Provider | `{Feature}Provider` | implements a provider interface (`IPaymentProvider`, `IWidgetProvider`, …) |
| Plugin entry point | `{Feature}Plugin` | inherits `BasePlugin` |
| Plugin constants | `{Feature}Defaults` | static class of system names, resource keys, URLs |
| Theme view | `{Name}ThemeView` | implements `IThemeView` |
| Mediator query | `Get{Thing}Query` / `Get{Thing}` | matches the handler name |
| Mediator command | `{Verb}{Thing}Command` | `InsertBlogCommentCommand` |
| Mediator handler | `{RequestName}Handler` | `GetBlogPostHandler`, `ContactUsCommandHandler` |
| Validator | `{Model}Validator` | `AbstractValidator<T>` |
| Validator input record | `{Name}ValidatorRecord` | positional record |
| View model | `{Thing}Model` | lives in the web project, never in `Grand.Domain` |
| Migration | `Migration{WhatItDoes}` | in `Migrations/{major}.{minor}/` |
| Scheduled task | `{Name}ScheduleTask` | implements `IScheduleTask` |
| Startup | `StartupApplication` | implements `IStartupApplication` |

## Files and folders

- One public type per file; file name equals type name.
- Folder name equals namespace segment.
- Razor views: `{Action}.cshtml`; view component views: `Views/Shared/Components/{ComponentName}/Default.cshtml`.
- Admin tab partials: `CreateOrUpdate.Tab{Name}.cshtml` alongside `CreateOrUpdate.cshtml`.
- Migration folders are version numbers: `2.4/`, not `v2.4/` or `2.4.0/`.

## Keys and identifiers

| Kind | Pattern | Example |
|---|---|---|
| Plugin system name | `{Group}.{Name}` | `Payments.CashOnDelivery` |
| Setting key | `{settingsclass}.{property}` (lowercased by the setting service) | `taxsettings.pricesincludetax` |
| Localization resource — core | `{Area}.{Screen}.{Field}` | `Admin.Catalog.Products.Fields.Name` |
| Localization resource — plugin | `Plugins.{Group}.{Name}.{Field}` | `Plugins.Payments.CashOnDelivery.Fields.DescriptionText` |
| Friendly-name resource | `Plugins.{Group}.{Name}.FriendlyName` | referenced from `{Feature}Defaults.FriendlyName` |
| Permission system name | `PermissionSystemName.{Area}` | resolved through `StandardPermission` |
| Cache key | `{ENTITY}_BY_{CRITERIA}_KEY` constant | `PRODUCTS_BY_CATEGORY_KEY` |
| Cache prefix | `{ENTITY}_PATTERN_KEY` constant | used with `RemoveByPrefix` |
| Widget zone | lowercase snake, matching the view | `product_page_bottom` |
| Schedule task name | stable string, equal to the DI key | must match `ScheduleTask.ScheduleTaskName` |

## Members

- `PascalCase` for types, methods, properties, constants, and public fields.
- `camelCase` for parameters and locals.
- `_camelCase` for private instance fields.
- Async methods that return `Task` are named for what they do, not with a forced `Async` suffix — follow the surrounding file.
- Boolean members read as assertions: `IsEnabled`, `HasDiscount`, `SupportRtl`.

## Anti-patterns

- Do not abbreviate: `CustomerService`, not `CustSvc`.
- Do not put `Manager`, `Helper`, or `Util` in a new type name unless extending an existing one.
- Do not encode the layer in the type name (`ProductServiceImpl`).
- Do not reuse a plugin system name that has shipped; it is the persisted identity of the plugin.
