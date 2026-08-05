# Glossary: Sales

Cart through order, payment, shipping, returns, and discounts.

Source: `src/Core/Grand.Domain/Orders/`, `Payments/`, `Shipping/`, `Discounts/`

---

## Cart

| Term | Meaning |
|---|---|
| **Shopping cart item** | `ShoppingCartItem` — a line held against the customer, not a separate cart document |
| **Shopping cart type** | `ShoppingCartType` — cart, wishlist, and the other list kinds share one entity |
| **Checkout attribute** | `CheckoutAttribute` — order-level options collected at checkout (gift wrap, delivery note), distinct from product attributes |

There is no `Cart` entity. A cart is the set of `ShoppingCartItem` rows on a `Customer` filtered by `ShoppingCartType` and store. Code that "loads the cart" is filtering that collection — apply the store filter.

## Order

| Term | Meaning |
|---|---|
| **Order** | `Order` — the placed order; immutable in its commercial essentials once placed |
| **Order item** | `OrderItem` — one purchased line, holding the price *as sold* |
| **Order note** | `OrderNote` — operator or system annotation, optionally customer-visible |
| **Order tag** | `OrderTag` — operator-defined label for filtering |
| **Order tax** | `OrderTax` — the tax breakdown as computed at placement |

`OrderItem` stores its own prices. Never recompute an order's totals from current product prices — the sold price is the record.

### Three statuses, three questions

| Enum | Question | Values |
|---|---|---|
| `OrderStatusSystem` | Where is the order in its lifecycle? | `Pending` (10), `Processing` (20), `Complete` (30), `Cancelled` (40) |
| `PaymentStatus` | Has it been paid? | pending / authorized / paid / refunded / voided |
| `ShippingStatus` | Has it shipped? | not required / not yet shipped / partially / shipped / delivered |

They move independently. A `Complete` order can be partially refunded; a `Processing` order can be fully shipped. Never derive one from another.

`OrderStatus` (alongside `OrderStatusSystem`) allows operator-defined statuses — read the system enum for logic, the operator status for display.

`OrderItemStatus` tracks per-line state, which is what makes partial shipment and partial return possible.

## Payment

| Term | Meaning |
|---|---|
| **Payment transaction** | `PaymentTransaction` — one attempt against a provider |
| **Transaction status** | `TransactionStatus` — where that attempt stands |
| **Payment status** | `PaymentStatus` — where the *order* stands commercially |
| **Payment provider** | `IPaymentProvider` — the plugin capability |
| **Payment restriction** | `PaymentRestrictedSettings` — which methods are hidden for which countries/groups |

An order may have several payment transactions (retry, capture, refund). `PaymentTransaction` is the audit trail; `Order.PaymentStatusId` is the summary. See `renamed-terms.md` for why they must not be conflated.

Flows: **Standard** (charged in-process) vs **Redirection** (customer leaves to the provider and returns). Which one a plugin implements changes everything about its lifecycle — see `.ai/skills/plugin-payment.md`.

## Shipping

| Term | Meaning |
|---|---|
| **Shipping method** | `ShippingMethod` — the operator-facing choice ("Courier", "Economy") |
| **Shipping option** | `ShippingOption` — a computed quote returned by a provider, with a rate |
| **Shipment** | `Shipment` — an actual dispatch; an order can have many |
| **Shipment item** | `ShipmentItem` — which order items, in which quantity, from which warehouse |
| **Warehouse** | `Warehouse` — stock location |
| **Pickup point** | `PickupPoints` — collect-in-person location, an alternative to delivery |
| **Delivery date** | `DeliveryDate` — the promised-window label shown on a product |
| **Shipment tracker** | `IShipmentTracker` — maps a tracking number to carrier events |

A *shipping method* is configuration; a *shipping option* is a runtime quote. Providers return options, never methods.

## Returns

| Term | Meaning |
|---|---|
| **Merchandise return** | `MerchandiseReturn` — the customer's request (called *return request* elsewhere) |
| **Merchandise return item** | `MerchandiseReturnItem` — which order items are coming back |
| **Merchandise return reason** | `MerchandiseReturnReason` — operator-defined reason list |
| **Merchandise return action** | `MerchandiseReturnAction` — what the customer wants: repair, replace, refund |
| **Merchandise return status** | `MerchandiseReturnStatus` — where the request stands |
| **Merchandise return note** | `MerchandiseReturnNote` — annotation on the request |

A merchandise return is a request, not a refund. It does not change `PaymentStatus` by itself.

## Discounts

| Term | Meaning |
|---|---|
| **Discount** | `Discount` — the definition: type, amount or percentage, validity window |
| **Discount type** | `DiscountType` — what it applies to: order total, order subtotal, shipping, per-product, category, brand, collection |
| **Discount coupon** | `DiscountCoupon` — a code that activates a discount |
| **Discount rule** | `DiscountRule` — a condition the cart must satisfy (called *discount requirement* elsewhere); implemented by `IDiscountRule` in a plugin |
| **Discount limitation** | `DiscountLimitationType` — usage caps: unlimited, N times, N times per customer |
| **Discount usage history** | `DiscountUsageHistory` — the redemption log enforcing those caps |

Multiple discounts can apply to one order. Never assume a single winner unless the discount type guarantees it.

## Loyalty and vouchers

| Term | Meaning |
|---|---|
| **Loyalty points** | `LoyaltyPointsHistory` — earned/spent ledger (called *reward points* elsewhere) |
| **Loyalty points settings** | `LoyaltyPointsSettings` — earn and redeem rates; some fields are system-wide, not per store |
| **Gift voucher** | `GiftVoucher` — prepaid value, `GiftVoucherType` physical or virtual |
| **Gift voucher usage history** | `GiftVoucherUsageHistory` — where the balance went |

Both are ledgers. Never store a computed balance as a field — derive it from the history.
