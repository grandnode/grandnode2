# Glossary

GrandNode's domain words and the types behind them. Use them for type names, model properties, resource keys, and variables. A search for the generic word finds nothing here, and a type named with it reads as if it came from another system.

## Where to look

| Topic | File |
|---|---|
| Which word to use, which to avoid — **start here** | `vocabulary.md` |
| What every entity shares: base types, marker interfaces, user fields, translations, slugs | `entity-model.md` |
| Products, product types, category/brand/collection, attributes, pricing, inventory | `catalog.md` |
| Cart, orders, payment, shipping, merchandise returns, discounts, loyalty points | `sales.md` |
| Customers, customer groups, vendors, sales employees, affiliates | `customers.md` |
| Stores, localization, settings, permissions, SEO, CMS, media, messaging, tasks | `platform.md` |

## How the glossary is kept

- The term is the type name: `Grand.Domain.Orders.MerchandiseReturn`, with no `Entity`, `Model` or `Dto` suffix in the domain layer.
- The shipped code wins. If an entry disagrees with the entity, correct the entry.
- A change that introduces a new domain concept adds its entry in the same change.
