# Vocabulary

Check this page before you name a type, a property, a resource key or a variable. Each row gives the word GrandNode uses and the generic e-commerce word that must not replace it. A type named with the generic word looks out of place next to its neighbors, and a search for it in this repository comes back empty.

## Catalog

| Write | Never | Implemented by |
|---|---|---|
| **Brand** | Manufacturer | `Grand.Domain.Catalog.Brand` |
| **Collection** | — (own concept, no generic equivalent) | `Grand.Domain.Catalog.Collection` |
| **Product layout** | Product template | `Grand.Domain.Catalog.ProductLayout` |
| **Specification attribute option** | — | `Grand.Domain.Catalog.SpecificationAttributeOption` |

Selectable renderings are always **layouts**: `ProductLayout`, `CategoryLayout`, `BrandLayout`, `CollectionLayout`, `PageLayout`. The word *template* is reserved for two other things: a message template (`Grand.Domain.Messages.MessageTemplate`, rendered with DotLiquid) and a Razor view file.

## Sales

| Write | Never | Implemented by |
|---|---|---|
| **Merchandise return** | Return request | `Grand.Domain.Orders.MerchandiseReturn` |
| **Loyalty points** | Reward points | `Grand.Domain.Orders.LoyaltyPointsHistory` |
| **Gift voucher** | Gift card | `Grand.Domain.Orders.GiftVoucher` |
| **Discount rule** | Discount requirement | `Grand.Domain.Discounts.DiscountRule` |
| **Checkout attribute** | — | `CheckoutAttribute` |

Payment state uses two separate enums, and mixing them up is a real bug:

- **`PaymentStatus`** (`Order.PaymentStatusId`): the commercial position of the whole order.
- **`TransactionStatus`** (`PaymentTransaction.TransactionStatus`): where a single payment attempt stands with its provider.

One order can have several payment transactions.

## Customers

| Write | Never | Implemented by |
|---|---|---|
| **Customer group** | Customer role | `Grand.Domain.Customers.CustomerGroup` |
| **User field** | Generic attribute | `Grand.Domain.Common.UserField` |
| **Address attribute** | — | `AddressAttribute` |

`CustomerGroup` and `PluginInfo.Group` share a word and nothing else. The first is a set of customers that drives pricing, visibility and permissions. The second is the category string in a plugin manifest, such as `"Payment methods"`, `"Widgets"` or `"Themes"`.

## Platform

| Write | Never | Implemented by |
|---|---|---|
| **Store** | Shop, site, tenant | `Grand.Domain.Stores.Store` |
| **Page** | Topic | `Grand.Domain.Pages.Page` |
| **Translation resource** | Locale string resource | `Grand.Domain.Localization.TranslationResource` |
| **Translation entity** | Localized property | `Grand.Domain.Localization.TranslationEntity` |
| **Entity URL** | URL record, slug record | `Grand.Domain.Seo.EntityUrl` |

A store is a storefront with its own domain hosts, currency, language and settings. Use no other word for it.

## Extensibility

The **plugin** is what gets installed: an assembly, its manifest, an `IPlugin` implementation, an output folder. A **provider** is a capability that a plugin registers, such as `IPaymentProvider`, `IShippingRateCalculationProvider`, `IWidgetProvider`, `IDiscountProvider` or `IThemeView`. One plugin can register several providers. The provider's `SystemName` must equal the manifest's (`.ai/standards/naming.md`).

## Identifiers that must not appear

A grep for these in new code should come back empty:

`Manufacturer` · `Topic` · `CustomerRole` · `ReturnRequest` · `RewardPoints` · `GiftCard` · `GenericAttribute` · `Tenant`

Do not give a business service the `Repository` suffix either. Data access goes through `IRepository<T>`, and services are named `*Service`.
