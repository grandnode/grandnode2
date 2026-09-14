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
- New and reworked grids use `<admin-grid>` (see [Admin grids](#admin-grids)). Views that have not been converted still build Kendo grids in script; do not add new `kendoGrid({...})` calls.
- Keep tab partials named `CreateOrUpdate.Tab{Name}.cshtml` next to `CreateOrUpdate.cshtml`.
- Never render an action the controller's permission attribute does not allow — the view is not the security boundary, but a mismatch is a bug.

## Admin grids

`<admin-grid>` (`Grand.Web.Common/TagHelpers/Admin/AdminGridTagHelper.cs`) renders `<div id data-role="grid" data-grand-grid='{json}'>` plus inert `<template>` elements; `admin.grid.js` (an adapter over Tabulator 6, built from `Grand.SharedUIResources/adminapp/src/grid`) turns it into the grid. `HeadAdmin`, `HeadStore` and `HeadVendor` load it next to Kendo, which still serves every grid that has not been converted.

```cshtml
<admin-grid id="measureweight-grid"
            read-url="@Url.Action("Weights", "Measure", new { area = Constants.AreaAdmin })"
            create-url="..." update-url="..." destroy-url="..."
            pager="Compact" edit-mode="Inline">
    <grid-toolbar-create/>
    <grid-text name="markAsPrimary" value="@Loc["Admin.Configuration.Measures.Weights.Fields.MarkAsPrimaryWeight"]"/>
    <grid-column field="Name" title="@Loc["..."]" width="300" editor="Text"/>
    <grid-column field="Ratio" title="@Loc["..."]" width="200" editor="Numeric" decimals="8"/>
    <grid-column field="DisplayOrder" title="@Loc["..."]" format="{0:0}" editor="Integer"/>
    <grid-column field="Id" title="@Loc["..."]" editable="false">
        <cell-template>
            <button type="button" class="k-button" data-grid-click="markAsPrimaryWeight">
                <i data-if="IsPrimaryWeight" class="fa fa-check"></i><i data-else class="fa fa-times"></i>
                {{ $texts.markAsPrimary }}
            </button>
        </cell-template>
    </grid-column>
    <grid-commands edit="true" destroy="true" width="200"/>
</admin-grid>
```

- The server contract does not change: `DataSourceRequest` in, `DataSourceResult {Data, Total, Errors}` out, form-urlencoded POSTs serialized like `jQuery.param`, the antiforgery token read like `addAntiForgeryToken`, errors shown through `display_kendoui_grid_error`.
- The tag helper fills the defaults: page size and page sizes from `AdminAreaSettings` for `pager="Full"` (`pager="Compact"` sends no paging fields, like a Kendo grid without `pageSize`), localized pager and command texts, the request culture for number and date formats, RTL from the working language.
- URLs: `*-url` attributes, or `controller` with `read-action`/`create-action`/`update-action`/`destroy-action`, the area taken from the current route.
- Conditional columns are plain `@if` around `<grid-column>`.
- Cell templates are markup, not code: `{{ Field }}` is encoded text, `{{ Field | n2 }}` is formatted, `{{{ Field }}}` is raw HTML, `data-if` / `data-else` hold simple conditions, `data-grid-click="globalFunction"` receives `(dataItem, event, grid)`. Nothing is evaluated as JavaScript; placeholders inside `on*`, `style` and `srcdoc` attributes are dropped, and URL attributes that would run script are removed.
- `{{{ Field }}}` and `encoded="false"` are only for markup the server builds (such as `AttributeInfo`). Never put `@Html.Raw` inside `<cell-template>`; pass localized text through `<grid-text>` and read it as `{{ $texts.name }}`.
- Editors: `Text`, `Numeric` (`decimals`; posts the culture decimal separator the model binder reads), `Integer`, `Checkbox`, `Date`, `DateTime`, `Select` (`options` from a `SelectListItem` list, or `options-url` returning JSON; `text-field` names the row field holding the display text), `Custom` (`editor-name` registered with `GrandAdmin.grids.editors.register`).
- `edit-mode="Inline"` gives Kendo-style row editing (Edit, then Update/Cancel); `<grid-toolbar-create>` adds a row; `confirm-destroy="true"` asks before Delete; `reload-after-save="false"` keeps the page after an update (create always reloads); `visible-if` on `<grid-commands>` hides Edit/Delete per row - the controller still enforces access.
- `selectable="Checkbox"` adds a checkbox column whose selection survives paging: `selectedIds` on the grid API, `on-change="fn"` (`e.selectedIds`), the `grand-grid:selection` DOM event, `clearSelection()`.
- `<grid-detail read-url="..." param="customerId:CustomerId">` with nested `<grid-column>` elements loads a detail grid per expanded row.
- `min-screen-width` hides a column below that window width; `auto-bind="false"` waits for `tabstrip_on_tab_show`.
- `$(el).data('kendoGrid')` keeps working for the calls views make (`dataSource.read/page/pageSize/get/data/total/remove/sync`, `dataItem(tr)`, `select()`, `refresh()`); `on-data-bound`, `on-edit`, `on-save`, `on-request-start`, `on-request-end`, `on-error`, `on-detail-init` name global handlers.
- `adminapp` source changes need `npm run build` and the rebuilt bundle committed with them (`.ai/skills/frontend-bundle-workflow.md`).

## Themes

- A theme copies only the views it changes; the rest resolve through the fallback view locations. See `.ai/skills/theme-creation.md`.
- A theme must not change view models, route names, or controller contracts.

## Anti-patterns

- Business logic or repository calls in a view.
- `@Html.Raw` on user-supplied content.
- Inline `style` attributes where a theme class exists.
- Copying an entire view tree into a theme.
- Editing a generated bundle by hand.
