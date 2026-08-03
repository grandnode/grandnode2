# Admin Area Changes

## Purpose
Handle GrandNode administration changes that may affect the main Admin panel, Store Owner panel, Vendor panel, shared admin models, permissions, navigation, validation, and scoped data access.

## When To Use
Use this skill when changing admin-facing features, management screens, grids, forms, reports, settings, permissions, menu items, admin shared models, validators, mapper profiles, controllers, or views.

Use this skill when a change in `Grand.Web.Admin` may also require corresponding changes in `Grand.Web.Store`, `Grand.Web.Vendor`, or `Grand.Web.AdminShared`.

Use this skill for product, order, shipment, merchandise return, payment transaction, report, customer, vendor, store, discount, message template, setting, catalog, and configuration management changes.

## When Not To Use
Do not use this skill for public storefront-only pages, API-only changes, plugin-only changes with no admin UI, or backend service changes that do not affect management workflows.

Do not use this skill as the primary review for MongoDB query safety, template markup details, or security vulnerabilities; combine it with the relevant skill when those concerns are present.

## Inputs Required
- Repository root.
- Target admin feature or changed files.
- Intended user role: system admin, store owner, vendor, or multiple roles.
- Affected entity, workflow, or setting.
- Existing closest controller, view, model, validator, mapper, and permission.
- Store, vendor, customer group, language, currency, and permission scope requirements.
- Tests, build commands, and UI validation target.

## Instructions

### Mandatory Rules
1. Identify whether the change belongs to Admin, Store Owner, Vendor, AdminShared, or multiple areas.
2. Read `references/admin-areas.md` before creating or changing an admin workflow.
3. Locate matching controllers in `src/Web/Grand.Web.Admin/Controllers`, `src/Web/Grand.Web.Store/Controllers`, and `src/Web/Grand.Web.Vendor/Controllers`.
4. Locate matching views in `src/Web/Grand.Web.Admin/Areas/Admin/Views`, `src/Web/Grand.Web.Store/Areas/Store/Views`, and `src/Web/Grand.Web.Vendor/Areas/Vendor/Views`.
5. Locate shared models, validators, and mapper profiles in `src/Web/Grand.Web.AdminShared`.
6. Determine whether the same feature must exist in the Store Owner or Vendor panel.
7. Determine whether the feature must be hidden, read-only, filtered, or unavailable for Store Owner or Vendor users.
8. Preserve controller base class conventions: admin controllers use `BaseAdminController`, store controllers use `BaseStoreController`, and vendor controllers use `BaseVendorController`.
9. Preserve permission checks using the existing `StandardPermission` or permission service pattern for the target area.
10. Preserve site map and menu behavior when adding, removing, or moving management pages.
11. Preserve store scope for Store Owner workflows.
12. Preserve vendor scope for Vendor workflows.
13. Never rely on UI-only filtering to enforce Store Owner or Vendor restrictions.
14. Update shared model, validator, mapper, and view changes together when a field or workflow changes.
15. Update list models, search models, grid columns, create/edit models, popup models, and tab partials consistently.
16. Preserve antiforgery behavior for forms and AJAX grid mutations.
17. Preserve localization resource keys for labels, validation messages, menu items, and notifications.
18. Check whether settings changes need store-scope override UI or store-specific persistence.
19. Check whether notifications or message templates must change when an admin workflow changes.
20. Check whether product, order, shipment, payment, discount, customer, vendor, or store workflows require domain-specific validation.
21. Add or update tests for controller behavior, validators, mapper profiles, permissions, and scoped filtering when applicable.
22. Run the narrowest relevant build or tests when execution is available.
23. State which panels were checked and which were not applicable.

### Recommendations
1. Prefer updating `Grand.Web.AdminShared` when Admin, Store Owner, and Vendor panels share the same model contract.
2. Prefer separate controllers and views when role-specific behavior is materially different.
3. Prefer explicit scope filters in service or query calls over filtering results after loading.
4. Prefer adding a disabled or read-only state over duplicating markup when only editing permissions differ.
5. Prefer matching existing tab, popup, grid, and partial naming conventions.
6. Prefer reviewing parallel workflows by entity name across all three panels before editing.

## Constraints
- Never add a field only to the Admin panel when Store Owner or Vendor uses the same shared model and requires the field.
- Never expose global Admin actions to Store Owner or Vendor users.
- Never allow Store Owner or Vendor users to access records outside their scope.
- Never add a menu item without matching permission and route behavior.
- Never add a form mutation without server-side permission and scope validation.
- Never break existing shared validators or mapper profiles for another panel.
- Never hardcode display text when localization resources are expected.
- Never change shared admin models without checking every panel that uses them.

## Expected Output
Produce one of these results:
- A completed admin-area change across all required panels.
- A review report listing missing Admin, Store Owner, Vendor, shared model, permission, navigation, validation, or scope updates.
- A concise implementation plan when role ownership cannot be determined from the repository.

Include affected panels, changed files, permission impact, scope impact, validation commands, and remaining risks.

## Validation Checklist
- [ ] The affected panels were identified.
- [ ] Parallel Admin, Store Owner, and Vendor workflows were checked.
- [ ] Shared models, validators, and mapper profiles were checked.
- [ ] Controllers and views were updated consistently.
- [ ] Permissions were checked server-side.
- [ ] Store and vendor scope were enforced server-side where relevant.
- [ ] Menus and site map entries were checked where relevant.
- [ ] Localization resources were checked.
- [ ] Forms and AJAX preserve antiforgery behavior.
- [ ] Tests or build commands were run or reported as not run.

## Examples

### Example 1: Product Field
Input: Add a new editable product field.

Output: Update `ProductModel`, mapper profile, validator, Admin product views, Store Owner product views, Vendor product views if vendors may edit the field, server-side save logic, permissions or read-only behavior, localization resources, and relevant tests.

### Example 2: Order Action
Input: Add an action to mark an order with an internal review flag.

Output: Add the action only where the role is allowed, enforce permission in the controller, enforce store or vendor scope before mutation, update grids or order detail tabs, add localization resources, and test unauthorized and out-of-scope access.

### Example 3: Setting
Input: Add a setting that affects vendor behavior.

Output: Update the shared settings model, settings mapper, Admin settings view, store-scope behavior if configurable per store, vendor panel visibility if the setting changes vendor UI, seeded resources, and validation.

