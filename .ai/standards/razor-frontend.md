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

- The panels are on **Bootstrap 5.3** with **bootstrap-icons**, both built by `adminapp`. Write Bootstrap 5 names: `ms-*`/`me-*` not `ml-*`/`mr-*`, `text-start`/`text-end`, `mb-3` not `form-group`, `form-check` not `custom-control`, `form-select` on a `<select>` (a `select.form-control` loses its arrow in Bootstrap 5), `visually-hidden` not `sr-only`, `text-bg-*` next to `badge`, `btn-close` not `close`, `data-bs-toggle` not `data-toggle`, and no input group wrapper element. Icons are `<i class="bi bi-name">`; the list is at https://icons.getbootstrap.com/, and `bi-fw` gives the fixed-width box Font Awesome's `fa-fw` did.
- Never write a `k-*` class. Kendo is gone, and nothing maps those names any more.
- The panel chrome lives in `adminapp/src/styles/_custom.scss`, not in `wwwroot`. `x_panel` / `x_title` / `x_content` are the three structural names an admin screen is built from and are driven by Bootstrap's `--bs-card-*` properties.
- Use the admin tag helpers for labels, inputs, validation, cards, tabs, and grids rather than raw Bootstrap markup.
- New and reworked admin screens, tabs and popups use the component layer - `<admin-page>`, `<admin-card>`, `<admin-field>`, `<admin-filters>`, `<admin-bulkbar>`, `<admin-action-menu>`, `<admin-popup>` - and its rules: one `btn-primary` per screen, destructive actions in the overflow menu, a grid's Add new in its card header. Follow `.ai/skills/admin-ui-component-layer.md`; the product list and product edit screens are the reference.
- New and reworked grids use `<admin-grid>` (see [Admin grids](#admin-grids)). No view builds a Kendo grid any more; do not add new `kendoGrid({...})` calls.
- No panel loads Kendo UI, Bootstrap 4, Font Awesome or simple-line-icons at all; the vendored `wwwroot/administration/kendo/` tree has been deleted. There is no Kendo compatibility layer: `$.fn.kendoGrid` and the other Kendo jQuery plugins do not exist. `$('#grid').data('kendoGrid')` returns the `<admin-grid>` API and is the only Kendo name left.
- No view calls Kendo UI at all. The widgets live in `admin.ui.js` (`Grand.SharedUIResources/adminapp/src/ui`) as `window.GrandAdmin`: `modal.open(target, { title, width })` / `modal.close(target)` for popups and confirmations, `tabs` behind `<admin-tabstrip>`, and `format` / `formatDate` / `formatNumber` / `htmlEncode` in place of `kendo.toString` and `kendo.htmlEncode`.
- Editors come from the shared templates, which post exactly what the request culture binds: `data-grand-numeric` hides the named input and keeps the posted number in it while a second input shows the formatted one; `data-grand-date` does the same next to a native `date`/`datetime-local`/`time` picker and is seeded with an ISO value from the server; `data-grand-select` is Tom Select over the `Search` endpoints. Do not write the posted value of these fields from a view - read it, or go through the widget (`element.grandNumeric.value()`).
- Keep tab partials named `CreateOrUpdate.Tab{Name}.cshtml` next to `CreateOrUpdate.cshtml`.
- Never render an action the controller's permission attribute does not allow — the view is not the security boundary, but a mismatch is a bug.

## Admin grids

`<admin-grid>` (`Grand.Web.Common/TagHelpers/Admin/AdminGridTagHelper.cs`) renders `<div id data-role="grid" data-grand-grid='{json}'>` plus inert `<template>` elements; `admin.grid.js` (an adapter over Tabulator 6, built from `Grand.SharedUIResources/adminapp/src/grid`) turns it into the grid. `HeadAdmin`, `HeadStore` and `HeadVendor` load it next to `admin.ui.js`.

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
            <button type="button" class="btn btn-default btn-sm" data-grid-click="markAsPrimaryWeight">
                <i data-if="IsPrimaryWeight" class="bi bi-check-lg"></i><i data-else class="bi bi-x-lg"></i>
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
- `edit-mode="Inline"` gives Kendo-style row editing (Edit, then Update/Cancel); `<grid-toolbar-create>` adds a row; `confirm-destroy="true"` asks before Delete; `reload-after-save="false"` keeps the page after an update (create always reloads); `visible-if` on `<grid-commands>` hides Edit/Delete per row, `edit-visible-if` / `destroy-visible-if` narrow each one on top of it, and `empty-text` is shown (muted, small) in a row left with no button - the controller still enforces access.
- `selectable="Checkbox"` adds a checkbox column whose selection survives paging: `selectedIds` on the grid API, `on-change="fn"` (`e.selectedIds`), the `grand-grid:selection` DOM event, `clearSelection()`. `selection-name="SelectedProductIds"` names the checkboxes so the ticked rows of the page post with the surrounding form.
- `selectable="Row"` selects one row by clicking it (Kendo `selectable: true`): `on-change="fn"`, `grid.dataItem(grid.select())`.
- `edit-mode="Batch"` edits cells in place (Kendo `incell`): `<grid-toolbar-save/>` sends the changed rows and the rows deleted meanwhile, `<grid-toolbar-cancel/>` restores them; `batch-prefix="products"` posts all rows in one request as `products[i].Field`, without it each row is posted on its own.
- A remote `Select` with `options-filter="startswith"` shows a search box that reloads `options-url` with the Kendo DropDownList server filter (`filter[filters][0][value]`, read by `DataSourceRequestFilterBinder`); `option-label` adds the empty choice.
- `data="@Model.Rows"` without `read-url` renders local rows (serialized with their own property names).
- `server-paging="false"` is for read URLs that return every row (a Kendo data source without `serverPaging`): pages are cut in the browser and no paging fields are sent.
- `on-data-bound` also runs when Tabulator renders the rows again on its own (a tab shown, columns hidden for the window width; `e.rerendered` is true), so handlers that bind to row elements - the magnificPopup edit links - must be safe to run again.
- `<grid-detail read-url="..." param="customerId:CustomerId">` with nested `<grid-column>` elements loads a detail grid per expanded row. `destroy-url` gets the same parameters, `visible-if` hides the expander for master rows without details, `on-data-bound` runs for each detail grid, and `on-detail-init` on the master receives `e.detailGrid` and `e.detailElement` (which also answers `$(el).data('kendoGrid')`). `recursive` makes the rows of the detail grid expand to the same detail, as deep as the data goes (knowledgebase categories to their subcategories): the read URL answers the children of any row, and `visible-if` on a child count keeps the expander off the leaves.
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
