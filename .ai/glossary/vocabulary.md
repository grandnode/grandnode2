# Vocabulary

The words GrandNode uses for its domain concepts, and the generic e-commerce words that must not replace them. Using the generic term produces types that read as foreign to the codebase, and searches that find nothing.

Read this before naming a type, a model property, a resource key, or a variable.

| Use | Not | Type |
|---|---|---|
| **Brand** | Manufacturer | `Grand.Domain.Catalog.Brand` |
| **Collection** | — | `Grand.Domain.Catalog.Collection` |
| **Page** | Topic | `Grand.Domain.Pages.Page` |
| **Customer group** | Customer role | `Grand.Domain.Customers.CustomerGroup` |
| **Merchandise return** | Return request | `Grand.Domain.Orders.MerchandiseReturn` |
| **Loyalty points** | Reward points | `Grand.Domain.Orders.LoyaltyPointsHistory` |
| **Gift voucher** | Gift card | `Grand.Domain.Orders.GiftVoucher` |
| **User field** | Generic attribute | `Grand.Domain.Common.UserField` |
| **Translation resource** | Locale string resource | `Grand.Domain.Localization.TranslationResource` |
| **Translation entity** | Localized property | `Grand.Domain.Localization.TranslationEntity` |
| **Entity URL** | URL record, slug record | `Grand.Domain.Seo.EntityUrl` |
| **Product layout** | Product template | `Grand.Domain.Catalog.ProductLayout` |
| **Discount rule** | Discount requirement | `Grand.Domain.Discounts.DiscountRule` |
| **Specification attribute option** | — | `Grand.Domain.Catalog.SpecificationAttributeOption` |
| **Address attribute**, **checkout attribute** | — | `AddressAttribute`, `CheckoutAttribute` |

## Layouts, not templates

Every entity that has a selectable rendering has a `*Layout` type — `ProductLayout`, `CategoryLayout`, `BrandLayout`, `CollectionLayout`, `PageLayout`. "Template" in this codebase means a **message template** (`Grand.Domain.Messages.MessageTemplate`, DotLiquid) or a Razor view file, never a catalog rendering choice.

## Two payment vocabularies

`PaymentStatus` and `TransactionStatus` are different enums for different objects:

- `Order.PaymentStatusId` → `Grand.Domain.Payments.PaymentStatus` — where the order stands commercially.
- `PaymentTransaction.TransactionStatus` → `Grand.Domain.Payments.TransactionStatus` — where one payment attempt stands with the provider.

An order may have several payment transactions. Do not treat the two as interchangeable.

## Groups, twice

"Group" means two unrelated things depending on the namespace:

- `CustomerGroup` — a set of customers, used for pricing, visibility, and permissions.
- `PluginInfo.Group` — the plugin category string in a manifest (`"Payment methods"`, `"Widgets"`, `"Themes"`).

## Provider vs plugin

- A **plugin** is the installable unit: an assembly, a manifest, an `IPlugin` implementation, an output folder.
- A **provider** is a capability the plugin registers: `IPaymentProvider`, `IShippingRateCalculationProvider`, `IWidgetProvider`, `IDiscountProvider`, `IThemeView`.

One plugin may register several providers. `SystemName` on the provider and `SystemName` in the manifest must match — see `.ai/standards/naming.md`.

## Store vs shop vs site

The codebase says **store** (`Grand.Domain.Stores.Store`) — a storefront with its own domain hosts, currency, language, and settings. "Shop", "site", and "tenant" appear nowhere; do not introduce them.

## Words to avoid entirely

| Do not write | Because |
|---|---|
| `Manufacturer` | it is `Brand` |
| `Topic` | it is `Page` |
| `CustomerRole` | it is `CustomerGroup` |
| `ReturnRequest` | it is `MerchandiseReturn` |
| `RewardPoints` | it is `LoyaltyPoints` |
| `GiftCard` | it is `GiftVoucher` |
| `GenericAttribute` | it is `UserField` |
| `Tenant` | it is `Store` |
| `Repository` as a type-name suffix on a business service | the repository is `IRepository<T>`; services are `*Service` |
