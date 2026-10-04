# Glossary

The vocabulary of the GrandNode domain, mapped to the types that implement it.

Read this before naming anything, before writing a model, and before assuming a term means what it means in another e-commerce platform. GrandNode descends from nopCommerce and renamed a large part of the vocabulary — using the old word in a new type name produces code that reads as if it belongs to a different system.

| File | Covers |
|---|---|
| `entity-model.md` | Base types, marker interfaces, and the mechanics every entity shares |
| `catalog.md` | Products, grouping, attributes, pricing, inventory |
| `sales.md` | Cart, orders, payment, shipping, returns, discounts |
| `customers.md` | Customers, groups, vendors, sales employees |
| `platform.md` | Stores, localization, settings, permissions, CMS, media, messaging |
| `renamed-terms.md` | Terms that differ from nopCommerce and other platforms — read this first |

## Rules

1. Use the domain term the codebase uses. A "return request" is a **merchandise return**; a "customer role" is a **customer group**.
2. The domain term is also the type name. `Grand.Domain.Orders.MerchandiseReturn` — no `Entity`, `Model`, or `Dto` suffix in the domain layer.
3. When a glossary entry and the shipped entity disagree, the entity is correct — fix the glossary.
4. New domain concepts get a glossary entry in the same change that introduces them.
