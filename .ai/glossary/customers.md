# Glossary: Customers and Vendors

Source: `src/Core/Grand.Domain/Customers/`, `Vendors/`, `Affiliates/`

---

## Customer

`Customer` is the account — and also the guest. A guest with items in a cart is a `Customer` record; registration converts it rather than creating a new one. Code that assumes "customer means registered" is wrong.

| Term | Meaning |
|---|---|
| **Customer group** | `CustomerGroup` — a set of customers driving pricing, visibility, and permissions (called *customer role* elsewhere) |
| **System customer group names** | `SystemCustomerGroupNames` — the built-in groups (administrators, registered, guests, vendors). Match on these constants, never on the display name |
| **Customer tag** | `CustomerTag` — operator-assigned label for segmentation and targeting |
| **Customer attribute** | `CustomerAttribute` / `CustomerAttributeValue` — operator-defined registration fields |
| **Customer note** | `CustomerNote` — operator annotation |
| **Customer product** | `CustomerProduct` — the customer's relationship to a product (recently viewed, personal listing) |
| **Customer product price** | `CustomerProductPrice` — a negotiated price for one customer |
| **User field** | `UserField` on `BaseEntity` — sparse extension data (called *generic attribute* elsewhere) |
| **System customer field names** | `SystemCustomerFieldNames` — the well-known `UserField` keys. Never hardcode the string |

Membership in a group is the authorization primitive. `CustomerGroup` drives `LimitedToGroups` on entities and providers — see `.ai/knowledge/scoping.md`.

## Identity and authentication

| Term | Meaning |
|---|---|
| **Password format** | `PasswordFormat` / `HashedPasswordFormat` — how the stored hash was produced; self-describing so old hashes can be upgraded on login |
| **Customer history password** | `CustomerHistoryPassword` — previous hashes, enforcing no-reuse policy |
| **External authentication** | `ExternalAuthentication` — a linked provider identity (Google, Facebook) |
| **Two factor authentication type** | `TwoFactorAuthenticationType` — app, email, or provider |
| **User API** | `UserApi` — API credentials for a customer, used by the API module |
| **Customer login results** | `CustomerLoginResults` — the enum a login attempt returns; not an exception |
| **User registration type** | `UserRegistrationType` — disabled, standard, email validation, admin approval |

Login failure is an expected outcome and returns a result value. Do not convert it into an exception — see `.ai/principles.md`.

## Vendor

`Vendor` is a seller operating inside a store — a marketplace participant, not an operator.

| Term | Meaning |
|---|---|
| **Vendor** | `Vendor` — the selling party; products carry its id |
| **Vendor note** | `VendorNote` — operator annotation |
| **Vendor review** | `VendorReview`, `VendorReviewHelpfulness` — customer feedback on the vendor |
| **Vendor settings** | `VendorSettings` — marketplace-wide vendor behavior |

A vendor manager sees only their own records. Every vendor-facing query filters on `VendorId`, and every vendor-facing write re-checks ownership server-side. A posted id is attacker-controlled. See `.ai/knowledge/scoping.md`.

`IWorkContext.CurrentVendor` is the logged-in vendor manager, and is null for ordinary customers and for background code.

## Sales employee

`SalesEmployee` is an internal staff member a customer can be assigned to — for commission and reporting. Distinct from a vendor (external seller) and from an administrator (an operator in the administrators customer group).

## Affiliate

`Grand.Domain.Affiliates.Affiliate` is a referrer credited for bringing in an order, tracked by URL parameter and stored on the order. Distinct from a vendor: an affiliate refers, a vendor sells.

## Four parties, four boundaries

| Party | Is | Sees |
|---|---|---|
| Customer | a buyer, possibly a guest | own orders, own data, one store |
| Vendor | an external seller | own products and orders, in stores they sell in |
| Sales employee | internal staff | assigned customers |
| Administrator | operator (a customer group) | everything their permissions allow |

Each has its own admin area — see `.ai/knowledge/admin-areas.md`. Reusing a shared model across areas does not reuse the scope filter; each area applies its own.
