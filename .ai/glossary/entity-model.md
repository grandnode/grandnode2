# Glossary: Entity Model

The mechanics every GrandNode entity shares. Source: `src/Core/Grand.Domain/`.

---

## Base types

| Type | Meaning |
|---|---|
| `ParentEntity` | The root of every persisted type. Owns `Id`, mapped to Mongo's `_id`. |
| `BaseEntity` | `ParentEntity` + `UserFields` + audit fields. **The default base for a top-level entity.** |
| `SubBaseEntity` | `ParentEntity` with nothing added — for documents embedded in another entity. |

```csharp
public abstract class ParentEntity
{
    [DBFieldName("_id")]
    public string Id { get; set; }   // a new UniqueIdentifier when unset
}

public abstract class BaseEntity : ParentEntity, IAuditableEntity
{
    public IList<UserField> UserFields { get; set; } = new List<UserField>();
    public DateTime CreatedOnUtc { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
    public string UpdatedBy { get; set; }
}
```

**Id** is a `string`, not an `ObjectId` or an `int`. It is assigned at construction, so an entity has a valid id before it is saved. Never generate ids yourself, and never treat an empty id as "new" without checking.

**Audit fields** (`IAuditableEntity`) are filled by the data layer's audit provider, not by service code. Do not set `CreatedOnUtc` or `UpdatedBy` by hand.

## Marker interfaces

These are the contract for cross-cutting behavior. Implementing one opts the entity into the corresponding machinery; forgetting one is why a feature silently does not work.

| Interface | Members | Effect |
|---|---|---|
| `IStoreLinkEntity` | `LimitedToStores`, `Stores` | Entity can be restricted to a subset of stores |
| `IGroupLinkEntity` | `LimitedToGroups`, `CustomerGroups` | Entity can be restricted to customer groups |
| `ISlugEntity` | `SeName` | Entity has a URL slug, tracked in `EntityUrl` |
| `ITranslationEntity` | `Locales` (`IList<TranslationEntity>`) | Entity has per-language property translations |
| `IAuditableEntity` | created/updated by/on | Audit stamping (already on `BaseEntity`) |

Scoping semantics for the first two are in `.ai/knowledge/scoping.md`. Never filter on them in the view — filter in the query.

## User fields

`UserField` is the open extension point: a name/value pair with an optional store id, attached to any `BaseEntity`. It is how plugins attach data to core entities without changing the core schema.

Use it for genuinely optional, sparse, per-installation data. Do **not** use it for a field the domain always has — that belongs on the entity — and do not query heavily on it.

## Localized properties

An entity implementing `ITranslationEntity` carries a `Locales` collection. The raw property holds the default-language value; per-language values live in `Locales`.

Read through the translation extension with `IWorkContext.WorkingLanguage.Id` — never render the raw property directly, and never cache a localized projection without the language id in the key.

Do not confuse this with `TranslationResource`, which holds UI strings (labels, messages) rather than entity content.

## Slugs

An `ISlugEntity` exposes `SeName`. The authoritative slug records live in `EntityUrl` (`Grand.Domain.Seo`), keyed by entity type and language. Changing a slug means writing an `EntityUrl` record — not just assigning `SeName`.

## Persistence

Entities are persisted through `IRepository<T>` (`Grand.Data`). The business layer never sees a Mongo collection, filter, or driver type. `[DBFieldName]` (from `Grand.SharedKernel.Attributes`) maps a property to a different stored field name.

## Rules

1. A top-level entity derives from `BaseEntity`; an embedded document derives from `SubBaseEntity`.
2. `Id` is a `string` and is already populated — do not overwrite it on insert.
3. Implement the marker interfaces the feature needs; they are not optional decoration.
4. No UI concerns, no persistence details, and no dependencies on business or infrastructure in `Grand.Domain`.
5. Entities carry data and invariants, not repository or service calls.
6. Adding a field to an entity that already ships needs a default that preserves existing behavior, and usually a migration — see `.ai/templates/migration.md`.
