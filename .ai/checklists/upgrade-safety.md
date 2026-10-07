# Checklist: Upgrade Safety

Use this whenever a change leaves something behind in the database: an entity or field, a setting, a translation resource, a permission, a sitemap entry, a migration, or any name another record points to.

GrandNode installations upgrade in place and keep their data. Each answer below has to hold for **an installation that already runs the previous version and already has data**, not just for a fresh install.

Answer every item as "yes, because …" or "N/A, because …". An item you could not verify goes into the PR as a stated risk.

---

## 1. Shape of the stored data

- [ ] Top-level documents derive from `BaseEntity`; embedded ones from `SubBaseEntity`.
- [ ] Scoping and lookup behavior comes from the marker interfaces (`IStoreLinkEntity`, `IGroupLinkEntity`, `ISlugEntity`, `ITranslationEntity`), not hand-rolled fields.
- [ ] A document written before this change still deserializes, and with the new field's default it behaves exactly as before.
- [ ] Dropping or renaming a stored field comes with a migration, or the PR says plainly that the old data is abandoned.
- [ ] `Grand.Domain` gained no UI type, driver type, or infrastructure dependency.
- [ ] Type and property names use the words in `.ai/glossary/vocabulary.md`.
- [ ] When checking real documents by hand, remember the stored field names are PascalCase (`Name`, `StoreId`). A lowercase `mongosh` filter matches nothing and looks like missing data.

## 2. Names other records depend on

These strings are keys. Change one and every installation is left with records pointing at nothing:

| Name | Where it lives |
|---|---|
| Plugin and provider `SystemName` | manifest, provider class, settings rows |
| `PermissionSystemName` | permission records, customer group mappings |
| Message template name | `MessageTemplate` documents |
| `ScheduleTaskName` | `ScheduleTask` documents, and must equal its DI key |
| Setting key | `Setting` documents |

- [ ] None of the above changed. If one really must, a migration moves the existing records and the PR lists it under *Breaking changes*.

## 3. Settings

- [ ] The class implements `ISettings`, and its defaults reproduce today's behavior.
- [ ] Existing installations get the setting from a migration, not only from the installer.
- [ ] Load and save use the same store scope.
- [ ] Making one field store-overridable did not freeze its siblings. The store-level row replaces the **whole** object (`LoadSetting` only falls back to the global row when no store row exists). A system-wide sibling field means the overridable field moves into its own `ISettings` class first.
- [ ] When a store-scoped copy is saved, the fields that must stay system-wide are restored from the global values.
- [ ] `ClearCache()` runs after the save.

## 4. Translation resources

- [ ] No hardcoded user-facing text. Every string has a key named per `.ai/standards/naming.md`, and admin fields have a `.Hint` key too.
- [ ] Each new core key is in **both** files: `App_Data/Resources/DefaultLanguage.xml` (fresh installs) and `App_Data/Resources/Upgrade/en_{version}.xml` (upgrades). Either one alone leaves raw keys on screen for part of the user base.
- [ ] `DefaultLanguage.xml` is UTF-16 LE: `grep` and `rg` find nothing in it, whatever you search for. Check it with PowerShell `[xml]` or Python, comparing keys case-insensitively.
- [ ] Plugin resources go in through `Install()`, and `Uninstall()` removes **all** of them.

## 5. Permissions and admin navigation

- [ ] The permission is declared in the `PermissionProvider` and enforced on the controller with `[PermissionAuthorize]`.
- [ ] A migration grants it on existing installations.
- [ ] A new admin sitemap entry ships with a migration as well.

## 6. The migration itself

- [ ] `Identity` is a newly generated GUID, not copied from a neighbor.
- [ ] `Version` matches its folder, and `Priority` orders it against the other migrations of that version.
- [ ] It actually runs for the installations it targets. `MigrationManager` only picks migrations whose version is **newer** than the installed one, so a migration in the current version's folder never runs on a database already stamped with that version. Compare `src/Build/Grand.Common.props` `<Version>` with the target folder. A new version line also needs its `MigrationUpgradeDbVersion_{version}` class.
- [ ] Running it a second time changes nothing.
- [ ] `UpgradeProcess` never throws; it catches and returns `false`.
- [ ] It leaves alone anything an operator may have edited.
- [ ] It does not depend on `IWorkContext` or any other ambient request context. It runs outside a request.
- [ ] Tested locally by setting `GrandNodeVersion` back one minor version in the database and restarting. Migrations already recorded are skipped by `Identity`. Restore the version stamp afterwards.

## 7. Reads, writes, and cache

- [ ] Cached reads use a key that includes the store id, plus the language id for translated data.
- [ ] Every write clears the matching prefix and publishes the entity event.
- [ ] Cached families that embed this entity are cleared too.
- [ ] New query patterns have an index, or the PR says why not.
- [ ] No unbounded query over a collection that grows with orders or customers.

## 8. Before the PR

- [ ] Exercised against a database that already holds data, not only a fresh install.
- [ ] The PR explains the upgrade path: what an existing installation goes through on first start.
- [ ] The PR states the rollback: what an operator does if this change turns out wrong.
