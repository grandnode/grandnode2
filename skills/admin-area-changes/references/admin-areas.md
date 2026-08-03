# GrandNode Admin Areas

## Panel Inventory
- Main Admin panel: `src/Web/Grand.Web.Admin`
- Store Owner panel: `src/Web/Grand.Web.Store`
- Vendor panel: `src/Web/Grand.Web.Vendor`
- Shared admin contracts: `src/Web/Grand.Web.AdminShared`

## Controller Locations
Controllers are directly under each web project:
- Admin: `src/Web/Grand.Web.Admin/Controllers`
- Store Owner: `src/Web/Grand.Web.Store/Controllers`
- Vendor: `src/Web/Grand.Web.Vendor/Controllers`

Use the matching base controller:
- Admin: `BaseAdminController`
- Store Owner: `BaseStoreController`
- Vendor: `BaseVendorController`

## View Locations
Views are under area folders:
- Admin: `src/Web/Grand.Web.Admin/Areas/Admin/Views`
- Store Owner: `src/Web/Grand.Web.Store/Areas/Store/Views`
- Vendor: `src/Web/Grand.Web.Vendor/Areas/Vendor/Views`

Use nearby view patterns for:
- `List.cshtml`
- `Create.cshtml`
- `Edit.cshtml`
- popup views
- `Partials/CreateOrUpdate.cshtml`
- `Partials/CreateOrUpdate.Tab*.cshtml`
- `Partials/*.cshtml`

## Shared Admin Contracts
Shared models, validators, and mapper profiles live in:
- `src/Web/Grand.Web.AdminShared/Models`
- `src/Web/Grand.Web.AdminShared/Validators`
- `src/Web/Grand.Web.AdminShared/Mapper`

Check `AdminShared` first when a model is used by more than one panel.

Common shared areas include:
- catalog and product models
- order, shipment, payment transaction, and merchandise return models
- customer, customer group, and vendor models
- discount models
- message template models
- store and settings models
- validators for create, edit, delete, popup, and relation models
- mapper profiles between domain entities and admin models

## Parallel Workflow Check
When changing an entity, search for matching names across all panels.

Examples:
- Product: check Admin `ProductController`, Store `ProductController`, Vendor `ProductController`, product views, `ProductModel`, `ProductValidator`, and `ProductProfile`.
- Order: check Admin `OrderController`, Store `OrderController`, Vendor `OrderController`, order views, `OrderModel`, order validators, and order-related reports.
- Shipment: check Admin `ShipmentController`, Store `ShipmentController`, Vendor `ShipmentController`, shipment views, `ShipmentModel`, and shipment permissions.
- Vendor: check Admin `VendorController`, Vendor `VendorInfoController`, vendor settings, vendor review flows, `VendorModel`, `VendorProfile`, and `VendorValidator`.
- Message templates: check Admin and Store `MessageTemplateController`, views, `MessageTemplateModel`, validator, mapper, and DotLiquid message template behavior.

## Permission Rules
Use existing permission patterns from nearby controllers.

Check:
- `StandardPermission` usage
- `_permissionService.Authorize(...)`
- area-specific access checks
- menu permission names in `src/Modules/Grand.Module.Installer/Utilities/StandardAdminSiteMap.cs`
- seeded permission names in installer utilities when adding new management areas

Do not expose Admin-only permissions to Store Owner or Vendor panels.

## Scope Rules
Admin can usually access global data when permission allows it.

Store Owner workflows must enforce store scope. Check:
- selected store context
- store-limited entities
- store-specific settings
- ACL and store mapping helpers
- search filters that include store ID

Vendor workflows must enforce vendor scope. Check:
- current vendor identity
- product vendor ID
- order item vendor ownership
- shipment and merchandise return ownership
- vendor editable settings
- vendor-specific reports

Server-side scope is mandatory. UI hiding is not enough.

## Navigation Rules
Main Admin menu entries are seeded in:
- `src/Modules/Grand.Module.Installer/Utilities/StandardAdminSiteMap.cs`

When adding a new admin page:
- Add or update the site map entry only if the page should appear in navigation.
- Use an existing resource key naming pattern.
- Use the correct controller and action.
- Attach the correct permission names.
- Check whether Store Owner or Vendor navigation also requires a corresponding entry.

## Form And Grid Rules
Follow existing area conventions for:
- admin tag helpers
- tab partials
- popup forms
- Kendo grids
- AJAX data functions
- `addAntiForgeryToken(data)`
- validation summary and field validation spans
- localized labels through `GrandResourceDisplayName`

When adding a field:
- Update model.
- Update mapper profile.
- Update validator.
- Update create/edit views.
- Update list/search models if the field is searchable or visible in grids.
- Update controller binding and save logic.
- Update localization resources.
- Update tests.

## Settings Rules
When changing settings:
- Check settings model in `AdminShared`.
- Check mapper profile.
- Check Admin settings controller and view.
- Check store-scope override behavior.
- Check whether Store Owner can configure the setting.
- Check whether Vendor panel needs read-only behavior or visibility changes.
- Update resource keys and validation.

## Test Targets
Prefer narrow tests:
- controller action authorization and scope tests
- validator tests
- mapper profile tests
- service tests for scoped queries
- UI model preparation tests

Run a targeted project build when tests are not available.

