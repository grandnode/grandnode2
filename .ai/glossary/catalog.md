# Glossary: Catalog

Source: `src/Core/Grand.Domain/Catalog/`

---

## Product

`Product` is the sellable unit. Its kind is `ProductTypeId` (`ProductType`):

| Value | Meaning |
|---|---|
| `SimpleProduct` (0) | One sellable item — the default |
| `GroupedProduct` (10) | A parent listing whose children are the sellable products |
| `Reservation` (20) | Booked by date/time slot — rooms, appointments (`ProductReservation`) |
| `BundledProduct` (30) | Sold as a set of other products (`BundleProduct`) |
| `Auction` (40) | Sold by bidding (`Bid`) |

Product type is not cosmetic — cart, pricing, and inventory branch on it. A change that touches purchasing must state which product types it was verified against.

## Grouping

Three independent ways to group products. They are not synonyms and they are not hierarchical variants of each other.

| Term | Meaning | Link entity |
|---|---|---|
| **Category** | The navigational tree. Hierarchical, has a parent. | `ProductCategory` |
| **Brand** | The maker. Flat. (Called *Manufacturer* in other platforms.) | on the product |
| **Collection** | A curated cross-cutting set — "Summer 2026", "Staff picks". Flat. | `ProductCollection` |

A product belongs to many categories and collections, and to at most one brand.

## Attributes

Two different mechanisms, constantly confused:

| Term | Purpose | Types |
|---|---|---|
| **Product attribute** | Customer-selectable options that change what is bought — size, colour. Can change price, weight, SKU, and stock. | `ProductAttribute`, `ProductAttributeMapping`, `ProductAttributeValue`, `ProductAttributeCombination` |
| **Specification attribute** | Descriptive facts used for filtering and comparison — screen size, material. Never changes price or stock. | `SpecificationAttribute`, its options |

- `ProductAttributeMapping` attaches an attribute to one product, with its control type (`AttributeControlType`).
- `ProductAttributeCombination` is a concrete combination (Red + Large) with its own SKU, stock, and price.
- `PredefinedProductAttributeValue` seeds values reused across products.

If a change affects what the customer can buy, it is a product attribute. If it affects how they find or compare it, it is a specification attribute.

## Pricing

| Term | Meaning |
|---|---|
| **Tier price** | Quantity-break price on a product (`TierPrice`); per-combination variant `ProductCombinationTierPrices` |
| **Customer product price** | A price negotiated for one customer (`Grand.Domain.Customers.CustomerProductPrice`) |
| **Product price** | `ProductPrice` — per-currency price entries |
| **Catalog price rules** | Discounts of type catalog, applied through `Discount` — see `sales.md` |

Prices are stored in the store's primary currency and converted for display. Never persist a converted value.

## Inventory

| Term | Meaning |
|---|---|
| **Manage inventory method** | `ManageInventoryMethod` — don't track / track by product / track by attribute combination |
| **Warehouse** | `Grand.Domain.Shipping.Warehouse` — stock location; per-combination stock in `ProductCombinationWarehouseInventory` |
| **Backorder mode** | `BackorderMode` — what happens at zero stock |
| **Low stock activity** | `LowStockActivity` — automatic reaction at the minimum threshold |
| **Inventory journal** | `InventoryJournal` — the movement log |
| **Out of stock subscription** | `OutOfStockSubscription` — customer notification request |

Stock lives at the product, the combination, or the warehouse level depending on the inventory method. Code that adjusts stock must handle all three.

## Layout

`ProductLayout`, `CategoryLayout`, `BrandLayout`, `CollectionLayout` select which view renders the entity. **Layout**, never "template" — in this codebase a template is a message template or a Razor file. See `renamed-terms.md`.

## Reviews and relations

| Term | Meaning |
|---|---|
| **Product review** | `ProductReview` — customer rating and text, with approval |
| **Cross-sell product** | `CrossSellProduct` — "customers also bought", shown in the cart |
| **Related product** | Curated "you may also like", on the product page |
| **Also purchased** | `ProductAlsoPurchased` — computed from order history |
| **Product deleted** | `ProductDeleted` — tombstone kept so historical orders still resolve |

`ProductDeleted` exists because orders reference products that operators remove. Never assume a product id in an order still resolves to a live `Product`.

## Other

| Term | Meaning |
|---|---|
| **Gift voucher** | `Grand.Domain.Orders.GiftVoucher` — prepaid value (called *gift card* elsewhere) |
| **Reservation** | `ProductReservation` — a bookable slot for a reservation product |
| **Bid** | `Bid` — an auction offer |
| **Customer group product** | `CustomerGroupProduct` — product visibility or ordering per customer group |
| **Customer tag product** | `CustomerTagProduct` — the same, driven by customer tags |
