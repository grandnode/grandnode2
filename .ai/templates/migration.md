# Template: Migration

An upgrade migration for existing installations. Read `.ai/prompts/add-migration.md` for the workflow.

Location: `src/Modules/Grand.Module.Migration/Migrations/{major}.{minor}/`

---

## Shape

```csharp
using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._2._4;

public class Migration{WhatItDoes} : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("{NEW-GUID}");
    public string Name => "{Short description} 2.4";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        // resolve services from serviceProvider — there is no ambient context here
        return true;
    }
}
```

Namespace convention: version folder `2.4/` maps to namespace segment `_2._4`.

## The four members

| Member | Rule |
|---|---|
| `Priority` | Order within the version. Resources, permissions, and sitemap migrations conventionally run at `0`; anything depending on them runs higher. |
| `Version` | The release the change ships in. Do not reuse a shipped `DbVersion` for new behavior. |
| `Identity` | A **freshly generated** GUID. The runner records it to know the migration already ran. Copying one from another migration means one of them silently never runs. |
| `Name` | Human-readable, including the version — it is what an operator sees in the upgrade log. |

## `UpgradeProcess` rules

1. **Never throw.** Catch and return `false`. A thrown exception aborts the whole upgrade.
2. **Be idempotent.** Check before inserting. A migration may be re-run after a partial upgrade.
3. **Do not destroy operator data.** Only overwrite values the operator cannot have customized.
4. New settings get defaults that preserve the pre-upgrade behavior — an upgraded store must behave as it did before.
5. `IWorkContext` does not exist here. Resolve what you need from `serviceProvider` and pass store ids explicitly. See `.ai/knowledge/scoping.md`.

## Localization resources

Do not write resource strings inline. Add them to the upgrade XML and import:

```csharp
public bool UpgradeProcess(IServiceProvider serviceProvider)
{
    return serviceProvider.ImportLanguageResourcesFromXml("App_Data/Resources/Upgrade/en_240.xml");
}
```

This mirrors `MigrationUpdateResourceString` in every version folder.

## Other migration kinds

Follow the existing file of the same name in the nearest version folder, rather than inventing a shape:

| Concern | Follow |
|---|---|
| Permissions | `MigrationSystemPermission.cs` |
| Admin navigation | `MigrationUpdateAdminSiteMap.cs` |
| Scheduled tasks | `MigrationScheduleTasks.cs` |
| Settings | `MigrationUpdateMediaSettings.cs`, `MigrationUpdateStore.cs` |
| DB version bump | `MigrationUpgradeDbVersion_{version}.cs` |

A new version folder needs a `MigrationUpgradeDbVersion_{version}` class to record the version bump.

---

## Checklist

- [ ] `Identity` GUID newly generated and unique across the repository.
- [ ] `Version` matches the folder and the release.
- [ ] `UpgradeProcess` cannot throw.
- [ ] Running twice is a no-op.
- [ ] No operator-owned data destroyed.
- [ ] Resource strings live in `App_Data/Resources/Upgrade/en_{version}.xml`.
- [ ] New settings default to pre-upgrade behavior.
- [ ] `MigrationUpgradeDbVersion_{version}` exists when the version folder is new.
