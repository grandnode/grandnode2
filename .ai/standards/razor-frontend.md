# Standard: Razor and Frontend

Rules for `.cshtml`, storefront JavaScript, and theme assets. Complementary to `.ai/skills/template-creation.md` (procedure) and `.ai/knowledge/template-types.md` (where each template type lives).

---

## Razor

- Strongly typed models: `@model` at the top, matching the type the action returns.
- Localization through the injected `LocService`: `@Loc["Resource.Key"]`. Never hardcode user-facing text.
- Tag helpers come from `_ViewImports.cshtml`. Every view folder that needs them must have one — plugins and themes each need their own.
- Storefront `_ViewImports.cshtml` removes the default `InputTagHelper` to avoid duplicated checkboxes. Copy that `@removeTagHelper` line when creating a theme's `_ViewImports.cshtml`.
- URLs through `Url.RouteUrl(...)` with named routes where nearby views do; do not hand-build paths.
- Widget zones stay where they are: `@await Component.InvokeAsync("Widget", new { widgetZone = "..." })`. Removing a zone is a breaking change for every installed widget plugin.
- Forms use antiforgery. AJAX mutations call `addAntiForgeryToken(data)` where nearby views do.
- `Html.Raw` only for content that is trusted or already sanitized.

### Preserve on every product/catalog view you touch

`data-cart-action`, product IDs, quick-view URLs, wishlist and compare attributes, and image `alt` / `title` / `loading` / priority attributes. These are contracts with the storefront JavaScript, not decoration.

## Vue in Razor

- Database-sourced HTML rendered through `Html.Raw` inside a Vue-controlled subtree is compiled as a template. Wrap it in `v-pre` when it may contain `{{ }}`.
- Keep view-model JSON out of inline `<script>` blocks; use the `[data-grand-vm]` island convention.
- Do not introduce a second frontend framework for one template.

## Assets

- Storefront source assets live in the Vite app; generated bundles are committed alongside the source. Follow `.ai/skills/frontend-bundle-workflow.md` for when `npm run build` is required and which output files must be committed.
- Theme-owned CSS/JS lives under the theme plugin's `Content/` folder and is copied with `CopyToOutputDirectory=PreserveNewest`.
- Do not reference external CDNs from storefront views.

## Admin views

- Use the admin tag helpers for labels, inputs, validation, cards, tabs, and grids rather than raw Bootstrap markup.
- Follow the existing Kendo grid + AJAX conventions.
- Keep tab partials named `CreateOrUpdate.Tab{Name}.cshtml` next to `CreateOrUpdate.cshtml`.
- Never render an action the controller's permission attribute does not allow — the view is not the security boundary, but a mismatch is a bug.

## Themes

- A theme copies only the views it changes; the rest resolve through the fallback view locations. See `.ai/skills/theme-creation.md`.
- A theme must not change view models, route names, or controller contracts.

## Anti-patterns

- Business logic or repository calls in a view.
- `@Html.Raw` on user-supplied content.
- Inline `style` attributes where a theme class exists.
- Copying an entire view tree into a theme.
- Editing a generated bundle by hand.
