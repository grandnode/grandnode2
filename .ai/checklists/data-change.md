# Checklist: Data Change

Run when the change touches a domain entity, a migration, settings, localization resources, permissions, or any persisted identity.

The question behind every item: **what happens to a store that already has data and is running the previous version?**

---

## Entity changes

- [ ] New entity derives from `BaseEntity` (top-level) or `SubBaseEntity` (embedded).
- [ ] The marker interfaces the feature needs are implemented: `IStoreLinkEntity`, `IGroupLinkEntity`, `ISlugEntity`, `ITranslationEntity`.
- [ ] A new field has a default that makes existing documents behave as they did before.
- [ ] A removed or renamed field has a migration, or the old data is knowingly abandoned and the PR says so.
- [ ] No UI concern, persistence detail, or infrastructure dependency added to `Grand.Domain`.
- [ ] Naming matches `.ai/glossary/` — the domain word, not the nopCommerce word.

## Migration

- [ ] `Identity` is a freshly generated GUID, unique across the repository.
- [ ] `Version` matches the folder and the shipping release.
- [ ] `Priority` orders it correctly against the other migrations in that version.
- [ ] `UpgradeProcess` cannot throw — it catches and returns `false`.
- [ ] Running it twice is a no-op.
- [ ] It does not overwrite or delete anything an operator may have customized.
- [ ] A new version folder has its `MigrationUpgradeDbVersion_{version}` class.
- [ ] It does not read `IWorkContext` — there is no ambient context.

## Settings

- [ ] New settings class implements `ISettings`.
- [ ] Defaults preserve pre-upgrade behavior.
- [ ] A migration seeds the setting for existing installations.
- [ ] Store scope handled: loaded and saved with the same scope.
- [ ] System-wide fields are preserved when saving a store-scoped copy.
- [ ] `ClearCache()` called after saving.

## Localization

- [ ] Every new user-facing string has a resource key.
- [ ] Key naming follows `.ai/standards/naming.md`.
- [ ] Core resources ship through `App_Data/Resources/Upgrade/en_{version}.xml` plus an import migration.
- [ ] Plugin resources are added in `Install()` and **every one of them** removed in `Uninstall()`.
- [ ] Admin fields have both a label and a `.Hint` key.

## Permissions and navigation

- [ ] New permission registered in the `PermissionProvider`.
- [ ] Migration adds it for existing installations.
- [ ] Controllers enforce it via `[PermissionAuthorize]`.
- [ ] Admin sitemap entry added, with a migration, if the feature needs navigation.

## Persisted identities — never rename

- [ ] Plugin `SystemName` unchanged.
- [ ] Provider `SystemName` unchanged.
- [ ] `PermissionSystemName` unchanged.
- [ ] Message template name unchanged.
- [ ] `ScheduleTaskName` unchanged (and still equal to its DI key).
- [ ] Setting key unchanged.

Renaming any of these orphans data in every installation. If a rename is genuinely required, it needs a migration that moves the old records, and a "Breaking changes" entry.

## Caching and events

- [ ] Reads of the new data are cached with a key containing store id (and language id where localized).
- [ ] Every write invalidates the prefix.
- [ ] Every write publishes the entity event.
- [ ] Other cached families that embed this entity are invalidated.

## Indexes and query shape

- [ ] New query patterns are supported by an index, or the omission is deliberate and stated.
- [ ] No unbounded query over a collection that grows with orders or customers.

## Verification

- [ ] Tested against a database that already has data, not only a fresh install.
- [ ] The upgrade path was reasoned about explicitly, and the reasoning is in the PR.
- [ ] Rollback consequences stated: what an operator does if this change is wrong.
