# Scoping: Store, Vendor, Customer, Language, Currency

GrandNode is multi-store and multi-vendor by default. Almost every leak of data across a boundary in this codebase is a missing scope filter. Read this before writing a query, a controller action, or an admin screen.

---

## The five axes

| Axis | Source | Read from |
|---|---|---|
| Store | resolved first, from host/route, by `IStoreContextSetter` | `IContextAccessor.StoreContext.CurrentStore` |
| Customer | resolved after store, by `IWorkContextSetter` | `IWorkContext.CurrentCustomer` |
| Vendor | the logged-in vendor manager, if any | `IWorkContext.CurrentVendor` |
| Language | customer preference, store default, or route | `IWorkContext.WorkingLanguage` |
| Currency | customer preference or store default | `IWorkContext.WorkingCurrency` |
| Tax display | derived from customer group and settings | `IWorkContext.TaxDisplayType` |

Store manager context (`IWorkContext.StoreManager`) identifies the store a store-owner is administering, which is not necessarily the storefront store.

Order matters: **store is resolved before customer**, because customer resolution depends on the store. Anything that assumes the reverse is wrong.

## Store scoping

Entities that can be limited to stores carry `LimitedToStores` + `Stores`. The pattern:

```csharp
if (entity.LimitedToStores && !entity.Stores.Contains(currentStore.Id))
    return null;   // or filter it out of the list
```

For queries, filter in the database, not after materialization:

```csharp
query = query.Where(x => !x.LimitedToStores || x.Stores.Contains(storeId));
```

Settings are store-scoped through `ISettingService` — a setting can have a global value and a per-store override. Loading a setting without a store id gives the global value, which is usually not what a storefront request wants. See `.ai/skills/settings-and-localization.md`.

Cache keys for store-scoped data must include the store id:

```csharp
var key = string.Format(CacheKey.TAXCATEGORIES_ALL_KEY, storeId);
```

Omitting the store id from a cache key makes store A serve store B's data. This is the single most common scoping bug.

## Vendor scoping

A vendor manager may only see and modify records their vendor owns.

- Filter by `VendorId` in the query, never in the view.
- On write, verify the loaded entity's `VendorId` matches `IWorkContext.CurrentVendor?.Id` before saving. An id in a form post is attacker-controlled.
- Vendor area views must not expose admin-only actions. See `.ai/knowledge/template-types.md`.

## Customer group scoping

Entities limited to customer groups carry `LimitedToGroups` + `CustomerGroups`. Same shape as store limiting:

```csharp
if (entity.LimitedToGroups &&
    !entity.CustomerGroups.Intersect(customer.Groups).Any())
    return null;
```

Providers (`IProvider`) carry both `LimitedToStores` and `LimitedToGroups` — a payment or shipping method can be hidden per store and per customer group.

## Language scoping

Localized entity properties live in a `LocalizedProperty` collection on the entity, resolved through the `GetTranslation` extension with `IWorkContext.WorkingLanguage.Id`. Do not read the raw property for display, and do not cache a localized projection without the language id in the key.

Localization *resources* (UI strings) are separate — `ITranslationService` / `LocService` in views. See `.ai/skills/settings-and-localization.md`.

## Currency scoping

Prices are stored in the store's primary currency and converted for display. Do not persist a converted value. Do not compare a converted price against a stored one.

## Where the ambient context is not available

`IWorkContext` is populated by `ContextMiddleware`, which is skipped for `/scalar/*`, `/openapi/*.json`, and `install`, and does not exist at all for:

- scheduled tasks
- message queue / email sending
- migrations
- plugin `Install()` / `Uninstall()`

Code in those paths must receive store, customer, or language **explicitly as parameters**. Reaching for `IWorkContext` there yields null or a stale context.

## Review checklist

- [ ] Every list query filters by store, and by vendor when the caller is a vendor.
- [ ] Every write re-checks ownership against the server-side context, not the posted id.
- [ ] Every cache key for scoped data includes store id (and language id when localized).
- [ ] Settings are loaded with the correct store id.
- [ ] Localized values are read through the translation extension with the working language.
- [ ] Background code takes scope as a parameter instead of reading `IWorkContext`.
- [ ] Admin, store-area, and vendor-area versions of the same screen each apply their own scope — reusing a shared model does not reuse the filter.
