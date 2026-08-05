# Prompt: Add Migration

## Purpose
Add an upgrade migration that seeds or changes data for existing installations — settings, localization resources, permissions, admin sitemap entries, scheduled tasks, or document shape.

## Inputs Required
- Repository root.
- What the migration must change, and why an existing installation cannot work without it.
- Target `DbVersion` (the version the change ships in).
- Whether the change is additive or rewrites existing documents.

## Steps

1. Read `.ai/skills/database-review.md` and `.ai/skills/mongodb-review.md`.
2. Open `src/Modules/Grand.Module.Migration/Migrations/` and read the highest existing version folder. Follow its file naming.
3. Decide the version folder. Create it only if the release version is new.
4. Create a class implementing `IMigration`:
   - `Priority` — ordering inside the version. Resource and permission migrations usually run at `0`.
   - `Version` — `new DbVersion(major, minor)`.
   - `Identity` — a **newly generated** GUID, never copied from another migration.
   - `Name` — a short human-readable description including the version.
   - `UpgradeProcess(IServiceProvider)` — returns `false` on failure, and must not throw.
5. For localization resources, add the strings to `App_Data/Resources/Upgrade/en_{version}.xml` and call `serviceProvider.ImportLanguageResourcesFromXml(...)` rather than writing resources inline.
6. For permissions, admin sitemap, or scheduled tasks, follow the existing `MigrationSystemPermission`, `MigrationUpdateAdminSiteMap`, and `MigrationScheduleTasks` files in the nearest version folder.
7. Bump the DB version with a `MigrationUpgradeDbVersion_{version}` class when introducing a new version folder.
8. Verify the migration is idempotent: running it twice must not duplicate data or overwrite operator changes.

## Mandatory Rules

1. `Identity` must be unique across the whole repository — the migration runner uses it to record what already ran.
2. A migration must never throw; catch and return `false`.
3. A migration must be idempotent — check for existing records before inserting.
4. A migration must not delete operator-owned data unless that is explicitly the requested change.
5. Resource changes go through the upgrade XML file, not through hardcoded strings in the migration.
6. New settings must be added with defaults that preserve existing behavior for upgraded stores.
7. Do not reuse a `DbVersion` that has already shipped for a behavior change; add a new migration in the current version instead.

## Output Format

- **Migration**: class name, version, priority, identity GUID.
- **What it seeds**: settings, resources, permissions, sitemap, or tasks.
- **Idempotency**: exactly how a second run is made a no-op.
- **Rollback**: what an operator has to do if this migration is wrong.
- **Validation**: build and test results.
