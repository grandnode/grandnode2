# Prompt: Upgrade a Kendo View

## Purpose
Move an admin view - a plugin's configuration screen, a popup, an older view of a third-party plugin - off Kendo UI, Bootstrap 4, Font Awesome and simple-line-icons onto what the panels run now: `<admin-grid>`, `window.GrandAdmin`, Bootstrap 5 and bootstrap-icons, laid out on the component layer.

Nothing of the old stack is loaded any more and there is no compatibility layer: `$.fn.kendoGrid`, the other Kendo widgets, `kendo.*`, the `k-*` classes, `$(el).data('kendoGrid')` and the Bootstrap 4 `data-toggle` / `data-target` / `data-dismiss` attributes all do nothing. A view that still uses them is broken, not just old-looking.

## When To Use
- A view calls `kendoGrid(...)`, `kendoWindow(...)`, `kendoNumericTextBox(...)`, `kendoDropDownList(...)`, `kendoMultiSelect(...)`, `kendoTabStrip(...)`, `kendo.toString` / `kendo.htmlEncode` / `kendo.culture`, or reads `.data('kendoGrid')`.
- A view carries `k-*` classes, Bootstrap 4 class names or data attributes, `fa fa-*` or `icon-*` icons.
- A plugin written for an older GrandNode stopped working in the admin panel.

## When Not To Use
- The storefront (`Grand.Web` views, themes, `vueapp`) - it never used Kendo.
- A view already on `<admin-grid>` that only needs the component-layer look: use `.ai/prompts/convert-admin-screens.md` and `.ai/skills/admin-ui-component-layer.md`.

## Inputs Required
- The view(s) to upgrade, and the controller actions they call.
- The panel(s) the view is served in: Admin, Store, Vendor (a plugin may ship `Areas/Admin` and `Areas/Store` copies - upgrade both).

## Read First
- `.ai/standards/razor-frontend.md`, sections *Admin views* and *Admin grids* - the full `<admin-grid>` reference; this prompt maps onto it and does not repeat it.
- `.ai/skills/admin-ui-component-layer.md` - the page, card, field, popup and button rules the upgraded view must follow.
- Reference views already upgraded: `src/Plugins/Tax.CountryStateZip/Areas/Admin/Views/TaxCountryStateZip/Configure.cshtml` (inline-edited grid on a page), `src/Plugins/Shipping.ByWeight/Areas/Admin/Views/ShippingByWeight/Configure.cshtml`, `src/Web/Grand.Web.Admin/Areas/Admin/Views/Measure/Partials/Weights.cshtml`, `src/Web/Grand.Web.AdminShared/Views/AdminShared/Product/Partials/CreateOrUpdate.ProductAttributes.TabAttributes.cshtml` (detail grids).

## Steps

1. **Inventory.** List every Kendo call, template, `k-*` class, Bootstrap 4 name and old icon in the view and in whatever it loads over AJAX (popups, partials). List the ids other views or scripts look up - a popup posting back by `btnId` / `formId`, a refresh button `btnRefresh...` - and keep them.
2. **Grids.** Replace each `$('#x').kendoGrid({...})` script and its `<div id="x">` with one `<admin-grid id="x">` using the mapping below. The controller actions stay as they are: `DataSourceRequest` in, `DataSourceResult` out.
3. **Grid lookups.** Replace every `$(el).data('kendoGrid')` with `GrandAdmin.grids.get(el)` (an id, `'#id'`, a selector, an element or a jQuery object). The methods on the result keep their Kendo names.
4. **Widgets.** Replace the other Kendo widgets with the editor templates, the tag helpers or `window.GrandAdmin` (mapping below). Prefer markup (`<admin-field>`, an editor template) over script.
5. **Markup.** Bootstrap 4 → 5, `k-*` → Bootstrap, icons → bootstrap-icons (mapping below). Then put the screen on the component layer: `<admin-page>` / `<admin-popup>`, `<admin-card>`, `<admin-field>`, one `btn-primary`.
6. **Text.** Titles, commands and cell text come from resources: `title="@Loc[...]"`, `<grid-text name="x" value="@Loc[...]"/>` read in a cell as `{{ $texts.x }}`. Never `@Html.Raw` inside `<cell-template>`.
7. **Verify** (see *Verification*).

## Mapping

### Kendo grid configuration → `<admin-grid>`

| Kendo | `<admin-grid>` |
|---|---|
| `dataSource.transport.read/create/update/destroy.url` | `read-url` / `create-url` / `update-url` / `destroy-url` (or `controller` + `read-action` ...) |
| `transport.read.data: fn` (extra search fields) | `additional-data="fn"` (global function returning an object), or `search-form="#selector"` |
| `pageSize`, `pageable` | `page-size`, `pager="Full"` (sizes from `AdminAreaSettings`) or `pager="Compact"`; none: `server-paging="false"` returns every row |
| `autoBind: false` | `auto-bind="false"` (a grid in a tab is loaded by `tabstrip_on_tab_show`) |
| `columns: [{ field, title, width, format }]` | `<grid-column field title width format>`; conditional columns are `@if` around them |
| `columns.template` (`#= Name #`, `#: Name #`, `# if (x) { # ... # } #`) | `<cell-template>`: `{{ Name }}` encoded, `{{{ Name }}}` raw server-built HTML only, `{{ Price \| n2 }}` formatted, `data-if="x"` / `data-else`, `data-grid-click="fn"` receives `(dataItem, event, grid)` |
| `editable: "inline"` + `command: ["edit", "destroy"]` | `edit-mode="Inline"` + `<grid-commands edit="true" destroy="true"/>` |
| `editable: "incell"` + `toolbar: ["save", "cancel"]` | `edit-mode="Batch"` + `<grid-toolbar-save/>` `<grid-toolbar-cancel/>`; `batch-prefix="products"` posts `products[i].Field` in one request |
| `toolbar: ["create"]` | `<grid-toolbar-create/>` (in a card it moves to the card header by itself) |
| `editable.confirmation` | `confirm-destroy="true"` |
| a custom `command: [{ name, text, click }]` | `<grid-command name text click="fn" icon class visible-if/>` inside `<grid-commands>` |
| `schema.model.fields` types / `columns.editor` | `editor="Text"`, `Numeric` (`decimals`), `Integer`, `Checkbox`, `Date`, `DateTime`, `Select` (`options`, or `options-url` + `options-filter="startswith"` + `text-field`), `Custom` (`editor-name`, registered with `GrandAdmin.grids.editors.register`); `editable="false"` for a read-only column |
| `selectable: "multiple, row"` with a checkbox template | `selectable="Checkbox"` (+ `selection-name` to post the ticked ids with the form, `<admin-bulkbar>` for the actions) |
| `selectable: true` | `selectable="Row"` + `on-change="fn"` |
| `detailInit` with a nested `kendoGrid` | `<grid-detail read-url param="id:Id">` with its own `<grid-column>`s; `on-detail-init` for extra work |
| `dataBound`, `edit`, `save`, `change`, `requestStart`, `requestEnd`, `error` | `on-data-bound`, `on-edit`, `on-save`, `on-change`, `on-request-start`, `on-request-end`, `on-error` (names of global functions; `on-data-bound` can run again for the same rows) |
| `columns.minScreenWidth` | `min-screen-width` |
| a local `dataSource.data` | `data="@Model.Rows"` without `read-url` |

### Grid API

`GrandAdmin.grids.get(el)` returns an object with the Kendo names views used: `dataSource.read()`, `.page(n)`, `.pageSize()`, `.total()`, `.data()`, `.view()`, `.get(id)`, `.remove(item)`, `.sync()`, `.hasChanges()`, `.cancelChanges()`, and on the grid `dataItem(tr)`, `select()`, `clearSelection()`, `refresh()`, `resize()`, `addRow()`, `editRow()`, `saveRow()`, `cancelRow()`, `saveChanges()`, `removeRow()`. Anything else is intentionally absent - rewrite the call instead of adding it.

### Other widgets

| Kendo | Now |
|---|---|
| `kendoWindow` / `.data('kendoWindow').open()/.close()` | `GrandAdmin.modal.open(target, { title, width })` / `GrandAdmin.modal.close(target)`; a popup view is `<admin-popup>` |
| `confirm(...)`, `onclick="return confirm(...)"` | `await GrandAdmin.modal.confirm(message, { title, confirmText, cancelText })`, or `data-grand-confirm="message"` on the button or link |
| `kendoNumericTextBox` | the numeric editor template (`<admin-field asp-for>` on a number), `data-grand-numeric`, or `GrandAdmin.numeric.create(el, options)` |
| `kendoDatePicker` / `kendoDateTimePicker` | the Date / DateTime editor templates, `data-grand-date`, `GrandAdmin.dateInput.create(el, options)` |
| `kendoDropDownList` / `kendoMultiSelect` | `asp-items` on `<admin-field>`, the `Category` / `Brand` / `Collection` / `Stores` / `MultiSelect` editor templates, `data-grand-select`, `GrandAdmin.select.create(el, options)` |
| `kendoTabStrip` | `<admin-tabstrip>` with `<tabstrip-item>`s (`GrandAdmin.tabs` behind it) |
| `kendo.toString(value, format)` | `GrandAdmin.format(value, format)`, `GrandAdmin.formatDate`, `GrandAdmin.formatNumber` (request culture) |
| `kendo.htmlEncode` | `GrandAdmin.htmlEncode` |
| `kendo.culture()` | `GrandAdmin.culture()` |
| content loaded later with widgets in it | `GrandAdmin.ui.init(root)` (content added to the page is picked up by itself) |

### Classes, attributes and icons

- `k-button` → `btn btn-outline-secondary` (`btn-primary` for the one committing action); `k-link`, `k-icon`, `k-input`, `k-widget`, `k-header` → plain Bootstrap 5 markup; `k-state-active` → the tag helpers set it.
- Bootstrap 4 → 5 as listed in `.ai/standards/razor-frontend.md` (*Admin views*): `data-bs-toggle` / `data-bs-target` / `data-bs-dismiss`, `ms-*` / `me-*`, `form-select`, `btn-close`, `visually-hidden`, `text-bg-*`, no `form-group`.
- `fa fa-*` and `icon-*` → `bi bi-*` (https://icons.getbootstrap.com/, `bi-fw` for `fa-fw`). The table the admin site map was converted with is in `src/Modules/Grand.Module.Migration/Migrations/2.4/MigrationUpdateAdminSiteMapIcons.cs`.

## Mandatory Rules
1. Keep every `id`, `name`, widget zone and the controller contract; scripts in other views look them up.
2. Upgrade every copy of the view: a plugin's `Areas/Admin` and `Areas/Store`, and the Vendor panel's own view if it has one.
3. No Kendo name survives: no `kendo*(`, `kendo.`, `data('kendoGrid')`, `k-*` class, Bootstrap 4 data attribute or old icon class.
4. Cell templates hold markup only - nothing in them is evaluated as JavaScript.
5. No new UI library and no CDN: Bootstrap 5.3, bootstrap-icons, Tabulator and Tom Select through `adminapp` are the whole kit.

## Verification
1. `grep` the upgraded files for `kendo`, `k-`, `data-toggle`, `data-target`, `data-dismiss`, `fa fa-`, `icon-` - nothing left.
2. Build the owning project (views compile into the DLL; a plugin: `dotnet build <plugin>.csproj`), restart Grand.Web in Development (`ASPNETCORE_ENVIRONMENT=Development`), open the screen.
3. In the running panel: the grid loads and pages, create / edit / delete (or Save changes) reach the server and the grid refreshes, popups open and post back, and the browser console is clean.

## Output Format
- **Upgraded**: each file, and the panels it reaches.
- **Mapped**: each Kendo grid or widget and what replaced it.
- **Kept**: the ids and endpoints that other code depends on.
- **Verified**: the grep, the build and what was exercised in the running panel.
- **Left**: anything not upgraded and why.
