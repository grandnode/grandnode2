# Template Creation

## Purpose
Create, modify, and review GrandNode templates consistently across Razor views, partials, view components, plugin views, theme overrides, Vue-in-Razor templates, PDF templates, and DotLiquid message templates.

## When To Use
Use this skill when asked to create or change templates, views, layouts, partials, components, admin forms, store or vendor screens, theme overrides, plugin UI, PDF views, email or notification message templates, Liquid tokens, or template seed data.

Use this skill when a change affects `.cshtml`, message template bodies, template names, DotLiquid drops, view component output, `_ViewImports.cshtml`, `_ViewStart.cshtml`, theme view paths, or template rendering behavior.

## When Not To Use
Do not use this skill for backend-only service changes, repository changes, migrations, plugin creation without UI, or API-only changes.

Do not use this skill as the primary review for payment correctness, security, MongoDB queries, or plugin architecture; combine it with the relevant skill when those concerns are present.

## Inputs Required
- Repository root.
- Template type to create or modify.
- Target area: public storefront, admin, store area, vendor area, plugin, module, theme, PDF, or message template.
- Existing closest template to use as a pattern.
- Model type, view component, controller action, message template name, or token source.
- Required localization keys, permissions, store scope, and customer or vendor scope.
- Required assets, scripts, styles, and generated bundle requirements.

## Instructions

### Mandatory Rules
1. Identify the template type before editing.
2. Read `.ai/knowledge/template-types.md` before creating a new template or changing an unfamiliar template type.
3. Locate the closest existing template in the same area and follow its folder, naming, model, layout, localization, tag helper, JavaScript, and CSS conventions.
4. Keep templates in the owning project or plugin; do not place plugin-owned views directly in `Grand.Web`.
5. Use the existing view location convention for the target area.
6. Use strongly typed models when the surrounding templates use `@model`.
7. Use existing localization access patterns such as `Loc[...]`, `@Loc[...]`, `LocalizedService.GetResource(...)`, or plugin translation resources according to the target area.
8. Use existing tag helpers and helpers for admin or store forms.
9. Preserve antiforgery behavior for forms and AJAX calls.
10. Preserve permission checks, store scope, vendor scope, customer group scope, language scope, and currency scope when the template exposes scoped data.
11. Use partials for repeated markup only when the same structure is reused or already follows local partial patterns.
12. Use view components when the UI requires independent data loading or existing component zones.
13. Keep JavaScript close to existing patterns for the same area; do not introduce a new framework for one template.
14. Keep theme overrides aligned with the base view model and route assumptions.
15. Escape user-controlled content by default.
16. Use `Html.Raw` or `v-html` only for content that is already sanitized, trusted, or intentionally HTML-formatted by the domain.
17. For message templates, use only tokens exposed by the relevant DotLiquid drops or `MessageTokenProvider`.
18. For message template seed data, update template names consistently with `MessageTemplateNames` and message-sending code.
19. For plugin templates, ensure the plugin project copies views and assets through the plugin build output pattern.
20. For Vue-in-Razor templates, preserve escaped Razor/Vue syntax and component registration JSON.
21. For frontend source changes under `vueapp` or theme CSS that affect committed bundles, run the frontend build or report that it was not run.
22. Run the narrowest relevant build or test command when execution is available.
23. State when visual verification was not performed.

### Recommendations
1. Prefer modifying the smallest existing template that owns the UI.
2. Prefer theme overrides over changing base storefront templates when the change is theme-specific.
3. Prefer plugin views over core views when the UI belongs to a plugin.
4. Prefer existing Bootstrap, Kendo, admin tag helper, and GrandNode component patterns.
5. Prefer localized text over hardcoded display text.
6. Prefer accessible labels, alt text, button text, and validation messages.
7. Prefer deterministic IDs for dynamic form controls and grid elements.
8. Prefer tests for view models, validators, message token generation, and component selection when markup behavior depends on backend logic.

## Constraints
- Never create templates without first identifying the owning area and closest existing pattern.
- Never duplicate large views when a partial, component, or theme override is the intended extension point.
- Never bypass authorization or scope checks in UI by hiding controls only on the client side.
- Never expose secrets, tokens, private customer data, or cross-store data in templates.
- Never use raw HTML output for user input unless the source is explicitly trusted or sanitized.
- Never add unversioned package dependencies or frontend frameworks for a template-only change.
- Never edit committed generated bundles without also identifying the source change that produced them.
- Never change shared layouts for a single-page need unless the change is intentionally global.

## Expected Output
Produce one of these results:
- A new or modified template set that follows GrandNode conventions.
- A review report with template findings ordered by severity.
- A creation plan when required ownership or model information is missing.

Include changed files, selected template type, closest template used, validation commands, visual verification status, and remaining risks.

## Validation Checklist
- [ ] The template type and owning area were identified.
- [ ] The closest existing pattern was followed.
- [ ] Folder path, filename, model, layout, and imports are consistent.
- [ ] Localization was used for display text.
- [ ] Forms and AJAX preserve validation and antiforgery behavior.
- [ ] Permission, store, vendor, customer group, language, and currency scope were checked where relevant.
- [ ] User-controlled content is escaped unless intentionally trusted.
- [ ] Plugin, module, and theme templates remain in their owning locations.
- [ ] Message templates use valid DotLiquid tokens.
- [ ] Frontend source and generated bundles are consistent when required.
- [ ] Build, tests, or visual verification were run or explicitly reported as not run.

## Examples

### Example 1: Admin Configuration Partial
Input: Add a settings section to a plugin admin configuration page.

Output: Update the plugin's `Areas/Admin/Views/{Controller}/Configure.cshtml` or add a partial next to it, use existing admin tag helpers, keep plugin localization resources in install and uninstall logic, preserve antiforgery behavior, and build the plugin project.

### Example 2: Storefront Theme Override
Input: Change the product card layout only for the Modern theme.

Output: Modify the matching view under `src/Plugins/Theme.Modern/Views/Modern`, preserve the base `ProductOverviewModel`, keep route and cart action attributes intact, and visually verify product listing behavior.

### Example 3: Message Template
Input: Add an email sent when a customer receives a new loyalty points reward.

Output: Add a message template name, seed the default template with DotLiquid tokens exposed by the appropriate drop, ensure sending code uses the same name, add missing token generation if needed, and test template rendering.

