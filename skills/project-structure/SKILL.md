# Project Structure

## Purpose
Orient an agent in the GrandNode repository so it can find the correct layer, understand the technologies in use, and extend the project consistently.

## When To Use
Use this skill before implementing a feature, locating ownership, planning a change, adding a project file, adding a service, adding UI, adding a plugin, adding a module, adding tests, or explaining where functionality belongs.

Use this skill when the task asks where something is located, how the repository is organized, what technologies are used, or how to expand the system safely.

## When Not To Use
Do not use this skill for narrow edits where the owning file and pattern are already known.

Do not use this skill as a replacement for domain-specific skills such as plugin creation, admin changes, template creation, MongoDB review, or security review.

## Inputs Required
- Repository root.
- User goal or feature description.
- Known affected entity, workflow, plugin, module, or UI area.
- Existing files mentioned by the user, if any.
- Constraints for Admin, Store Owner, Vendor, Store, Customer, or API behavior.

## Instructions

### Mandatory Rules
1. Read `references/repository-map.md` before making structural decisions.
2. Identify the requested change type: domain, business service, data access, web UI, admin UI, API, plugin, module, frontend asset, test, build, or deployment.
3. Locate the closest existing feature with the same entity or workflow.
4. Follow the existing folder, namespace, dependency, registration, model, mapper, validator, controller, view, and test patterns.
5. Keep domain entities in `Grand.Domain`.
6. Keep repository and MongoDB infrastructure in `Grand.Data`.
7. Keep cross-cutting infrastructure in `Grand.Infrastructure`.
8. Keep business interfaces in `Grand.Business.Core` and implementations in the relevant `Grand.Business.*` project.
9. Keep public storefront UI in `Grand.Web`.
10. Keep main Admin UI in `Grand.Web.Admin`.
11. Keep Store Owner UI in `Grand.Web.Store`.
12. Keep Vendor UI in `Grand.Web.Vendor`.
13. Keep shared admin models, validators, and mapper profiles in `Grand.Web.AdminShared`.
14. Keep reusable web helpers and shared UI infrastructure in `Grand.Web.Common`.
15. Keep plugins under `src/Plugins` and modules under `src/Modules`.
16. Keep tests under `src/Tests` in the closest matching test project.
17. Check whether the change affects Admin, Store Owner, Vendor, API, plugin, message templates, scheduled tasks, or frontend bundles.
18. Select and apply any more specific skill required by the change.
19. Avoid creating new architectural categories unless existing structure cannot support the change.
20. State the selected ownership path before making broad changes.

### Recommendations
1. Prefer searching by entity name across `Domain`, `Business`, `Web`, `AdminShared`, `Plugins`, `Modules`, and `Tests`.
2. Prefer extending existing services and interfaces before adding new ones.
3. Prefer existing startup registration patterns over ad hoc service registration.
4. Prefer central package management through `Directory.Packages.props`.
5. Prefer targeted project builds and tests over full solution builds during iteration.
6. Prefer updating README-level build instructions only when behavior visible to contributors changes.

## Constraints
- Never place plugin or module code directly in a core web project.
- Never place web view models in domain or data projects.
- Never place persistence logic in controllers or Razor views.
- Never bypass shared admin models when multiple admin panels use the same contract.
- Never add package versions directly to project files when central package management is used.
- Never introduce Entity Framework patterns; this repository uses MongoDB-backed data access.
- Never assume Admin, Store Owner, Vendor, API, and public storefront share the same permissions or scope.
- Never move files across layers as a refactor unless the user explicitly requests it.

## Expected Output
Produce one of these results:
- A repository orientation summary with the correct ownership path.
- A feature expansion plan listing the files and layers to modify.
- A completed change that follows the selected project structure.
- A review finding when code was placed in the wrong layer or project.

Include the selected change type, owning projects, closest existing pattern, specific files to inspect or modify, validation commands, and remaining risks.

## Validation Checklist
- [ ] The change type was identified.
- [ ] The owning project and layer were selected.
- [ ] The closest existing pattern was found.
- [ ] Cross-panel, API, plugin, module, and frontend impacts were checked.
- [ ] Domain, business, data, UI, and test responsibilities were kept separate.
- [ ] Package and build conventions were respected.
- [ ] More specific skills were used where required.
- [ ] The result states what was checked and what was intentionally out of scope.

## Examples

### Example 1: Add Product Field
Input: Add a field to products.

Output: Inspect `Grand.Domain/Catalog`, product services in `Grand.Business.Catalog`, shared admin product models and mapper profiles in `Grand.Web.AdminShared`, Admin/Store/Vendor product controllers and views, API DTOs if exposed, MongoDB persistence behavior, localization resources, and product tests.

### Example 2: Add Payment Provider
Input: Add a new payment method.

Output: Use `src/Plugins/Payments.*` as the owning area, follow payment plugin patterns, update plugin manifest, provider, settings, startup registration, configuration views, install resources, and plugin tests.

### Example 3: Add Admin Setting
Input: Add a setting visible to store owners.

Output: Update the setting domain object, AdminShared settings model and mapper, Admin and Store settings controllers and views, store-scope override behavior, localization resources, and targeted tests.

