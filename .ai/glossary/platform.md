# Glossary: Platform

Stores, localization, settings, permissions, content, media, and messaging.

Source: `src/Core/Grand.Domain/Stores/`, `Localization/`, `Configuration/`, `Permissions/`, `Seo/`, `Pages/`, `Media/`, `Messages/`

---

## Store

| Term | Meaning |
|---|---|
| **Store** | `Store` — a storefront with its own hosts, currency, language, and settings |
| **Domain host** | `DomainHost` — a hostname routed to a store; how a request resolves its store |
| **Bank account** | `BankAccount` — store payment details shown on invoices |
| **Store link entity** | `IStoreLinkEntity` — the `LimitedToStores` / `Stores` marker |

Multi-store is the default assumption, not a feature flag. The store is resolved **before** the customer, at the start of every request — see `.ai/knowledge/request-lifecycle.md`.

## Localization

| Term | Meaning |
|---|---|
| **Language** | `Language` — an installed language with culture and RTL flag |
| **Translation resource** | `TranslationResource` — one UI string, keyed (called *locale string resource* elsewhere) |
| **Translation resource area** | `TranslationResourceArea` — which surface a resource belongs to |
| **Translation entity** | `TranslationEntity` — a per-language value of an *entity property* |
| **Translation entity marker** | `ITranslationEntity` — the `Locales` collection |

Two distinct things:

- **Resources** are UI strings — labels, messages, validation text. Read through `ITranslationService` in services and `LocService` (`@Loc["..."]`) in views. Seeded from `App_Data/Resources/` and shipped through upgrade XML.
- **Translations on entities** are content — a product's name in German. Read through the translation extension with the working language id.

A hardcoded user-facing string is a defect in both cases.

## Settings

| Term | Meaning |
|---|---|
| **Settings class** | any `ISettings` implementation — `CatalogSettings`, `OrderSettings`, … |
| **Setting service** | `ISettingService` — load/save, with per-store overrides |
| **Store scope** | the store a setting value applies to; empty means the global value |

A setting has a global value and optional per-store overrides. Loading without a store id gives the global value, which is rarely what a storefront request wants. Some fields are deliberately system-wide and must be preserved when saving a store-scoped copy. See `.ai/skills/settings-and-localization.md`.

Always `ClearCache()` after saving settings.

## Permissions and navigation

| Term | Meaning |
|---|---|
| **Permission** | a named capability granted to customer groups |
| **Permission system name** | `PermissionSystemName` — the stable identifier used in `[PermissionAuthorize]` |
| **Permission action name** | `PermissionActionName` — finer-grained action within a permission |
| **Standard permission** | `StandardPermission` — the built-in set |
| **Permission provider** | `PermissionProvider` — registers permissions at install |
| **Admin site map** | `AdminSiteMap` — the admin navigation tree |
| **Group link entity** | `IGroupLinkEntity` — `LimitedToGroups` / `CustomerGroups` |

A new permission needs a provider entry **and** a migration, or existing installations never receive it. See `.ai/skills/permission-navigation.md`.

## SEO

| Term | Meaning |
|---|---|
| **Entity URL** | `EntityUrl` — the authoritative slug record, per entity and language |
| **SeName** | the slug property on `ISlugEntity` |
| **Entity types** | `EntityTypes` — which entity a slug belongs to |
| **Robots.txt** | `RobotsTxt` — operator-editable crawler rules |

Changing a slug means writing an `EntityUrl` record, not just assigning `SeName`.

## Content

| Term | Meaning |
|---|---|
| **Page** | `Grand.Domain.Pages.Page` — an operator-authored content page (called *topic* elsewhere) |
| **Page layout** | `PageLayout` — which view renders it |
| **Blog** | `Grand.Domain.Blogs` — posts, categories, comments |
| **News** | `Grand.Domain.News` — news items and comments |
| **Knowledgebase** | `Grand.Domain.Knowledgebase` — articles and categories |
| **Course** | `Grand.Domain.Courses` — courses, lessons, subjects, tied to a product |
| **Document** | `Grand.Domain.Documents` — operator documents attached to customers or orders |

Content authored in the admin is rendered with `Html.Raw` into a Vue-controlled page. Database content containing `{{ }}` is compiled as a Vue template unless wrapped in `v-pre` — see `.ai/standards/razor-frontend.md`.

## Media

| Term | Meaning |
|---|---|
| **Picture** | `Picture` — an image, stored in the database or on a configured provider |
| **Download** | `Download` / `DownloadType` — a downloadable file, e.g. for digital products |
| **Media settings** | `MediaSettings` — thumbnail sizes and image behavior |
| **Storage settings** | `StorageSettings` — where binaries live (DB, filesystem, S3, Azure Blob) |

## Messaging

| Term | Meaning |
|---|---|
| **Message template** | `MessageTemplate` — a DotLiquid email/notification body, keyed by name |
| **Message template names** | the constants message-sending code matches on |
| **Queued email** | the outbound message row; sending is asynchronous, via a scheduled task |
| **Token / drop** | the DotLiquid values a template may reference |
| **Message tokens added event** | the extension point for plugins to add tokens |

A template may only use tokens the relevant drop exposes. See `.ai/skills/message-notification.md`.

## Tasks and infrastructure

| Term | Meaning |
|---|---|
| **Schedule task** | `Grand.Domain.Tasks.ScheduleTask` — the persisted definition; the DI key must equal `ScheduleTaskName` |
| **Migration** | `IMigration` — a versioned upgrade step, identified by a GUID |
| **DB version** | `MigrationDb` / `DbVersion` — the installed schema version |
| **GrandNode version** | `GrandNodeVersion` — the product version record |
| **History** | `Grand.Domain.History` — change tracking for auditable entities |

Scheduled tasks and migrations run without a request, and therefore without `IWorkContext`. They take store and customer explicitly.
