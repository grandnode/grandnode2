# Permission and Navigation

## Purpose
Create, modify, and review GrandNode permissions, controller authorization attributes, admin sitemap entries, and permission seeding for new features and panels.

## When To Use
Use this skill when adding a new admin feature that requires a permission check, adding a menu item to the Admin panel, changing which customer groups have access to a feature, adding a fine-grained action-level permission, reviewing controller authorization, or seeding permissions and navigation entries for existing or new installations.

## When Not To Use
Do not use this skill for Store Owner or Vendor panel UI work without a permission change; combine with `admin-area-changes` when controllers and views are also involved.

Do not use this skill as the primary review for MongoDB query safety or authentication flow; combine with `security-review` or `dotnet-review` when those concerns apply.

## Inputs Required
- Repository root.
- Feature name and affected panel: Admin, Store Owner, Vendor, or public store.
- Required permission system name and category.
- Actions the permission should gate: List, Create, Edit, Preview, Delete, Export, Import, etc.
- Which customer groups should receive the permission by default.
- Whether a sitemap (navigation) entry is required and where it should appear.

## Instructions

### Mandatory Rules

#### Defining a Permission
1. Add a `public const string` to `PermissionSystemName` in `src/Core/Grand.Domain/Permissions/PermissionSystemName.cs`. Use `PascalCase` for the value, e.g. `"ManageRewards"`.
2. Add a `public static readonly Permission` field to the appropriate `StandardPermission*` partial class in `src/Core/Grand.Domain/Permissions/`. Choose the file that matches the feature category (Catalog, Customer, Order, Marketing, Content, Configuration, System, Report, Vendor, Store, PublicStore). Create a new partial-class file only when none of the existing categories fits.
3. Set these fields on the permission entry:
   - `Name` — human-readable, e.g. `"Manage Rewards"`.
   - `SystemName` — the `PermissionSystemName` constant defined in step 1.
   - `Area` — use `"Admin area"`, `"Vendor area"`, `"Store area"`, or `"Public store"` matching the target panel.
   - `Category` — match the private category constant used by the sibling permissions in the same file.
   - `Actions` — list only the `PermissionActionName` constants that apply. Omit unused actions.
4. Register the permission in `PermissionProvider.GetPermissions()` at `src/Business/Grand.Business.Common/Services/Security/PermissionProvider.cs`.
5. Add a `DefaultPermission` entry in `PermissionProvider.GetDefaultPermissions()` to assign the permission to the correct customer group(s) by default. Use the group system names from `SystemCustomerGroupNames` (e.g., `Administrators`).

#### Seeding for New Installations
6. The installer reads permissions from `IPermissionProvider`. No separate installer file change is required when `PermissionProvider` is updated correctly — the installer calls `IPermissionProvider.GetPermissions()` and `GetDefaultPermissions()` during `InstallPermissions()` in `src/Modules/Grand.Module.Installer/Services/InstallDataPermissions.cs`.

#### Seeding for Existing Installations
7. Add a migration under `src/Modules/Grand.Module.Migration/Migrations/` when a new permission must appear on existing installations. In the migration, check whether the permission already exists by `SystemName` before inserting it to keep the migration idempotent.

#### Authorizing Controllers
8. Ensure Admin controllers inherit from `BaseAdminController` (it already applies `[AuthorizeAdmin]`) to enforce access to the admin panel.
9. Ensure Vendor controllers inherit from `BaseVendorController` (it already applies `[AuthorizeVendor]`) to enforce vendor panel access.
10. Ensure Store Owner controllers inherit from `BaseStoreController` (it already applies `[AuthorizeStore]`) to enforce store panel access.
11. Apply `[PermissionAuthorize(PermissionSystemName.X)]` at class level on Admin, Store, or Vendor controllers to gate the entire controller on a specific feature permission. This must come after the panel-level attribute.
12. Apply `[PermissionAuthorizeAction(PermissionActionName.X)]` at action method level to enforce granular action gating within a controller that already carries `[PermissionAuthorize(...)]`. This checks both the class-level permission and the named action.
13. Never substitute `IPermissionService.Authorize(...)` in a controller action for the attribute-based approach unless the check is conditional on runtime data. Attribute checks run before the action body executes.

#### Admin Sitemap (Navigation)
14. Add a new `AdminSiteMap` entry to `StandardAdminSiteMap.SiteMap` in `src/Modules/Grand.Module.Installer/Utilities/StandardAdminSiteMap.cs` for new installations. Set:
   - `SystemName` — unique identifier string.
   - `ResourceName` — localization key, e.g. `"Admin.Catalog.Rewards.Manage"`.
   - `ControllerName` and `ActionName` — must match the route. Use `Url` only when a controller/action pair does not apply.
   - `PermissionNames` — list of `PermissionSystemName` constants; the menu item is hidden when the user lacks all listed permissions (or any, when `AllPermissions = false`).
   - `AllPermissions` — set `true` only when the page genuinely requires every listed permission simultaneously.
   - `IconClass` — copy an icon class from a sibling entry in the same category group.
   - `DisplayOrder` — order within its parent `ChildNodes` list.
15. Place leaf items inside the `ChildNodes` of the appropriate root category node. Do not add top-level root nodes unless the feature cannot belong to any existing category.
16. Add a migration that inserts the `AdminSiteMap` entry for existing installations using `IAdminSiteMapService.InsertSiteMap(...)`. Check whether the entry already exists by `SystemName` before inserting.
17. Add the corresponding localization resource key to `DefaultLanguage.xml` (`src/Web/Grand.Web/App_Data/Resources/DefaultLanguage.xml`) so the menu label renders correctly.

### Recommendations
1. Prefer existing `PermissionActionName` constants over inventing new action name strings.
2. Prefer placing a new permission in an existing `StandardPermission*` partial class over adding a new file.
3. Prefer assigning new admin-area permissions to the `Administrators` group by default; leave Store and Vendor area permissions to their respective groups.
4. Prefer an empty `PermissionNames` list on parent category nodes and explicit `PermissionNames` on leaf nodes, matching the pattern of sibling entries.
5. Prefer verifying the permission check at the controller or action level rather than inside the view template.

## Constraints
- Never place a permission constant directly in a controller instead of referencing `PermissionSystemName`.
- Never hardcode a customer group ID in `DefaultPermission`; use `SystemCustomerGroupNames` constants.
- Never add a sitemap entry without a matching permission or without confirming the route exists.
- Never skip the migration for existing installations when a new permission or sitemap entry is required.
- Never apply `[PermissionAuthorizeAction(...)]` on an action whose class does not carry `[PermissionAuthorize(...)]`.
- Never use UI-side hiding alone to restrict access; server-side attribute checks are mandatory.

## Key Contracts

### Permission entity (`src/Core/Grand.Domain/Permissions/Permission.cs`)
```csharp
public class Permission : BaseEntity {
    public string Name       { get; set; }   // display name
    public string SystemName { get; set; }   // unique key, matches PermissionSystemName constant
    public string Area       { get; set; }   // "Admin area" | "Vendor area" | "Store area" | "Public store"
    public string Category   { get; set; }   // grouping label shown in ACL grid
    public ICollection<string> CustomerGroups { get; set; }  // assigned group IDs
    public ICollection<string> Actions       { get; set; }   // gatable sub-actions
}
```

### Authorization attributes

| Attribute | Target | Checks |
|---|---|---|
| `[AuthorizeAdmin]` | Controller class | `ManageAccessAdminPanel`; rejects vendors/store managers |
| `[AuthorizeVendor]` | Controller class | `ManageAccessVendorPanel` + Vendors group + active vendor |
| `[AuthorizeStore]` | Controller class | `ManageAccessStoreManagerPanel` + StoreManagers group + StaffStoreId |
| `[AuthorizeAdminOrStore]` | Controller class | Admin OR Store Owner; includes IP restriction check |
| `[PermissionAuthorize("SystemName")]` | Controller class | Named feature permission for current customer |
| `[PermissionAuthorizeAction("ActionName")]` | Action method | Named action within the class-level feature permission |

### PermissionActionName constants (`src/Core/Grand.Domain/Permissions/PermissionActionName.cs`)
`List`, `Preview`, `Create`, `Edit`, `Delete`, `Export`, `Import`, `Payments`, `Cancel`, plus sub-entity variants for Weights, Dimensions, Units.

### AdminSiteMap entity (`src/Core/Grand.Domain/Admin/AdminSiteMap.cs`)
```csharp
public class AdminSiteMap : BaseEntity {
    public string SystemName    { get; set; }
    public string ResourceName  { get; set; }   // localization key
    public string ControllerName { get; set; }
    public string ActionName    { get; set; }
    public string Url           { get; set; }   // use when no controller/action
    public IList<AdminSiteMap> ChildNodes   { get; set; }
    public string IconClass     { get; set; }
    public int    DisplayOrder  { get; set; }
    public IList<string> PermissionNames { get; set; }
    public bool   AllPermissions { get; set; }  // false = ANY permission; true = ALL
}
```

## File Locations

| Concern | Path |
|---|---|
| Permission entity | `src/Core/Grand.Domain/Permissions/Permission.cs` |
| PermissionSystemName constants | `src/Core/Grand.Domain/Permissions/PermissionSystemName.cs` |
| PermissionActionName constants | `src/Core/Grand.Domain/Permissions/PermissionActionName.cs` |
| StandardPermission (panel access) | `src/Core/Grand.Domain/Permissions/StandardPermission.cs` |
| StandardPermission by category | `src/Core/Grand.Domain/Permissions/StandardPermission*.cs` |
| DefaultPermission entity | `src/Core/Grand.Domain/Permissions/DefaultPermission.cs` |
| IPermissionProvider interface | `src/Business/Grand.Business.Core/Interfaces/Common/Security/IPermissionProvider.cs` |
| PermissionProvider implementation | `src/Business/Grand.Business.Common/Services/Security/PermissionProvider.cs` |
| IPermissionService interface | `src/Business/Grand.Business.Core/Interfaces/Common/Security/IPermissionService.cs` |
| AuthorizeAdmin attribute | `src/Web/Grand.Web.Common/Filters/AuthorizeAdminAttribute.cs` |
| AuthorizeVendor attribute | `src/Web/Grand.Web.Common/Filters/AuthorizeVendorAttribute.cs` |
| AuthorizeStore attribute | `src/Web/Grand.Web.Common/Filters/AuthorizeStoreAttribute.cs` |
| AuthorizeAdminOrStore attribute | `src/Web/Grand.Web.Common/Filters/AuthorizeAdminOrStoreAttribute.cs` |
| PermissionAuthorize attribute | `src/Web/Grand.Web.Common/Security/Authorization/PermissionAuthorizeAttribute.cs` |
| PermissionAuthorizeAction attribute | `src/Web/Grand.Web.Common/Security/Authorization/PermissionAuthorizeActionAttribute.cs` |
| AdminSiteMap entity | `src/Core/Grand.Domain/Admin/AdminSiteMap.cs` |
| StandardAdminSiteMap seed | `src/Modules/Grand.Module.Installer/Utilities/StandardAdminSiteMap.cs` |
| IAdminSiteMapService | `src/Business/Grand.Business.Core/Interfaces/System/Admin/IAdminSiteMapService.cs` |
| AdminSiteMapService | `src/Web/Grand.Web.Common/Menu/AdminSiteMapService.cs` |
| Installer — permissions seed | `src/Modules/Grand.Module.Installer/Services/InstallDataPermissions.cs` |
| Tests — PermissionService | `src/Tests/Grand.Business.Common.Tests/Services/Security/PermissionServiceTests.cs` |
| Tests — PermissionProvider | `src/Tests/Grand.Business.Common.Tests/Services/Security/PermissionProviderTests.cs` |
| Tests — AdminSiteMapService | `src/Tests/Grand.Web.Common.Tests/Services/Admin/AdminSiteMapServiceTests.cs` |

## Expected Output
Produce one of these results:
- A new permission constant, `StandardPermission` entry, `PermissionProvider` update, migration, controller attributes, and sitemap entry.
- A review report listing missing or incorrectly applied permission checks.
- A plan when the feature scope or customer group mapping cannot be determined from the repository.

Include changed files, permission system name, customer group assignment, sitemap location, migration status, and remaining risks.

## Validation Checklist
- [ ] `PermissionSystemName` constant added.
- [ ] `StandardPermission` static field added with correct `Area`, `Category`, and `Actions`.
- [ ] Permission registered in `PermissionProvider.GetPermissions()`.
- [ ] Default group assignment added in `PermissionProvider.GetDefaultPermissions()`.
- [ ] Migration adds the permission for existing installations (idempotent check by `SystemName`).
- [ ] Admin controllers carry `[AuthorizeAdmin]` at class level.
- [ ] Vendor controllers carry `[AuthorizeVendor]` at class level.
- [ ] Store controllers carry `[AuthorizeStore]` at class level.
- [ ] Feature controllers carry `[PermissionAuthorize(PermissionSystemName.X)]` at class level.
- [ ] Action-level `[PermissionAuthorizeAction(...)]` used only on controllers that already carry `[PermissionAuthorize(...)]`.
- [ ] Sitemap leaf entry added to `StandardAdminSiteMap` under the correct parent node.
- [ ] Sitemap migration adds the entry for existing installations (idempotent check by `SystemName`).
- [ ] Localization resource key added to `DefaultLanguage.xml` for the menu label.

## Examples

### Example 1: New Admin Feature with Permission and Menu Entry
Input: Add a "Manage Rewards" feature in the Catalog section for administrators.

Output:
1. Add `public const string Rewards = "ManageRewards"` to `PermissionSystemName`.
2. Add `public static readonly Permission ManageRewards` to `StandardPermissionCatalog.cs` with `Area = "Admin area"`, `Category = CategoryCatalog`, `Actions = [List, Create, Edit, Delete]`.
3. Register in `PermissionProvider.GetPermissions()` and add a `DefaultPermission` mapping to `Administrators` in `GetDefaultPermissions()`.
4. Create a migration that inserts the permission when `SystemName == "ManageRewards"` does not yet exist.
5. Decorate `RewardsController` with `[AuthorizeAdmin]` and `[PermissionAuthorize(PermissionSystemName.Rewards)]`. Add `[PermissionAuthorizeAction(PermissionActionName.Create)]` on the POST create action.
6. Add a sitemap leaf entry to the Catalog `ChildNodes` in `StandardAdminSiteMap` with `PermissionNames = ["ManageRewards"]` and the `Admin.Catalog.Rewards.Manage` resource key.
7. Add a migration that inserts the sitemap entry by `SystemName` when it does not yet exist.
8. Add `admin.catalog.rewards.manage` to `DefaultLanguage.xml`.

### Example 2: Restrict an Existing Action
Input: The "Delete" action on the blog controller should be available to administrators only, not store managers.

Output:
1. Verify `[PermissionAuthorize(PermissionSystemName.Blog)]` is already on the controller.
2. Add `[PermissionAuthorizeAction(PermissionActionName.Delete)]` to the DELETE action method.
3. In the admin ACL configuration, remove the `Delete` action from the `ManageBlog` permission for the StoreManagers customer group by updating the default in `PermissionProvider.GetDefaultPermissions()` or via a migration that updates the allowed actions.

### Example 3: Vendor-Specific Permission Check
Input: A vendor controller action should verify the user is in the Vendors group and has the `ManageVendorReviews` permission before loading reviews.

Output:
1. Confirm `[AuthorizeVendor]` is at class level (enforces panel access and active vendor account).
2. Add `[PermissionAuthorize(PermissionSystemName.VendorReviews)]` at class level to enforce the feature permission on top of panel access.
3. No sitemap change is needed because vendor navigation is not database-driven.
