# Renamed Terms

GrandNode descends from nopCommerce and renamed much of the vocabulary. Using the old term produces types that read as foreign to the codebase, and searches that find nothing.

Read this before naming a type, a model property, a resource key, or a variable.

| Elsewhere | In GrandNode | Type |
|---|---|---|
| Manufacturer | **Brand** | `Grand.Domain.Catalog.Brand` |
| — (new concept) | **Collection** | `Grand.Domain.Catalog.Collection` |
| Topic | **Page** | `Grand.Domain.Pages.Page` |
| Customer role | **Customer group** | `Grand.Domain.Customers.CustomerGroup` |
| Return request | **Merchandise return** | `Grand.Domain.Orders.MerchandiseReturn` |
| Reward points | **Loyalty points** | `Grand.Domain.Orders.LoyaltyPointsHistory` |
| Gift card | **Gift voucher** | `Grand.Domain.Orders.GiftVoucher` |
| Generic attribute | **User field** | `Grand.Domain.Common.UserField` |
| Locale string resource | **Translation resource** | `Grand.Domain.Localization.TranslationResource` |
| Localized property | **Translation entity** | `Grand.Domain.Localization.TranslationEntity` |
| URL record / slug record | **Entity URL** | `Grand.Domain.Seo.EntityUrl` |
| Product template | **Product layout** | `Grand.Domain.Catalog.ProductLayout` |
| Discount requirement | **Discount rule** | `Grand.Domain.Discounts.DiscountRule` |
| Specification attribute option | **Specification attribute option** | unchanged |
| Address attribute, checkout attribute | unchanged | `AddressAttribute`, `CheckoutAttribute` |

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
