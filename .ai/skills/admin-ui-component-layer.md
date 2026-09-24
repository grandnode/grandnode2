# Admin UI Component Layer

## Purpose
Bring an admin screen - list, detail, settings page, popup - onto the one visual style the panel is moving to, and keep every screen converted after it looking the same. This is the style the dashboard and the statistics screens were built in, promoted to a panel-wide component layer: tokens, cards, fields above-the-control, one primary button per screen.

The reference screens are the product list and the whole product edit screen, every tab and popup of it (commits `3b5eee299` .. `b7fbe0a8b` on `featrure/admin-front`). When this skill and those files disagree, the files win - say so and update this skill.

## When To Use
Use this skill when you create an admin view, or change one that still uses the old chrome: `x_panel` / `x_title` / `x_content`, `form-horizontal` with `admin-label` + `div.col-md-9.col-sm-9` rows, `note note-info` used as a section heading, `mt-checkbox` wrappers, `btn green|red|blue|purple|default`, `btn-group-devided`, inline `style=`.

Use it together with `.ai/skills/admin-area-changes.md` (which panel owns the file, what Store and Vendor get) and `.ai/skills/frontend-bundle-workflow.md` (building and committing the bundles).

## When Not To Use
Not for the storefront (Grand.Web views, themes, vueapp). Not for plugin views you do not own - third-party plugins keep the legacy classes working, which is why none of them is deleted yet.

## The Style In One Screen
- **Page:** a header - title (1.5rem/600) with an accent icon, an optional subtitle and back link, and the actions at the end - above a column of cards spaced by `--grand-gap` (1rem).
- **Card:** white surface (dark: `--bs-tertiary-bg`), 1px `--grand-border`, 12px radius, soft shadow. A titled header (1rem/600) with its buttons at the end, a padded body, or a flush body when the card holds only a grid.
- **Field:** the label **above** the control (.875rem/500), the hint below it (.8125rem, muted), the validation message last. Two fields to a row where the pane is wide enough, one where it is not. A width is a size (sm 8rem, md 18rem, lg 32rem), never a column count.
- **Yes/no:** a Bootstrap switch in the accent colour, label beside it, hint on the line below.
- **Unit:** `USD`, `lb(s)` as the end of the control's input group, same height, flat where they meet.
- **Grid:** rows at .9375rem, column heads .8125rem/600 on the sunken surface, rules from `--grand-rule`, row tick 1.05rem, pager footer .875rem muted with the current page on the accent, summary "1-15 of 54". State columns are chips (`.grand-chip--green|red|amber`), a secondary line in a cell is `.grand-cell-sub` (the SKU under the product name).
- **Buttons:** exactly one `btn-primary` per screen (accent teal, the action that commits - Save, Add new on a list); everything else `btn-outline-secondary`; destructive `btn-outline-danger`, and on a detail screen inside the `⋮` overflow menu so Delete never sits next to Save.
- **Add new for a grid:** always in the header of the card that holds the grid, at its **start** - right after the card's title, or first when the card has none - `btn btn-outline-secondary btn-sm` with `bi-plus-lg`. The `bi-plus-lg` icon is what moves it there (`.grand-card__header:has(.bi-plus-lg)` in `_components.scss`), so an add button without it lands at the far end. Never under the grid, never in the grid's own toolbar.
- **Tabs:** text tabs with a 2px accent underline on the active one, one rule across the strip, the pane set off by `--grand-gap`.
- **Colour:** only tokens. Accent `--grand-accent` (#0b7285, dark #66d9e8); tints `--grand-teal|violet|amber|red|green|slate` each with a `-soft` background. Both colour modes are defined once in `adminapp/src/styles/_tokens.scss`.

## The Building Blocks
All tag helpers live in `src/Web/Grand.Web.Common/TagHelpers/Admin/`, all component CSS in `adminapp/src/styles/_components.scss` (tokens in `_tokens.scss`, the card in `_dashboard.scss`, the tab strip restyle in `ui/ui.css`).

| Write | Renders | Replaces |
|---|---|---|
| `<admin-page title icon meta back-url back-text sticky>` + `<page-actions>` | `.grand-page` with `.grand-page__header` | `.row > .col-md-12 > .x_panel > .x_title > .caption + .actions` |
| `<admin-card title icon flush>` + `<card-actions>` | `.grand-card` > `__header` (`__title`, `__actions`) + `__body` / `__body--flush` | `x_content`, `panel panel-default`, `note note-info` as a heading |
| `<admin-field asp-for size asp-items asp-required asp-template hint suffix asp-label-for>` | `.grand-field` > label, `__control`, `__hint`, validation | `div.mb-3 > admin-label + div.col-md-9.col-sm-9 > control + span[asp-validation-for]` |
| `<div class="grand-fields grand-fields--2col">` | two columns where they fit | `.row > .col-md-6` pairs |
| `<admin-filters id submit-id expanded secondary-submit>` + `<filters-search>` | `.grand-filters`: one search field, Filters toggle, collapsed two-column panel | `.main-header` + `.drop-filters-container` + ten stacked 3/9 rows |
| `<admin-bulkbar grid-id>` | `.grand-bulkbar`, shown only while rows are ticked, "{0} selected" | "Delete selected" always visible next to Add new |
| `<admin-action-menu text>` + `<admin-action text icon url submit name id destructive>` | the `⋮` dropdown | export/import/copy/delete buttons in the header row |
| `<admin-popup title icon>` + `<div class="grand-popup__actions">` | `.grand-page.grand-popup` | the `x_panel` chrome each popup built by hand |
| `<div class="grand-reveal">` > trigger field + `<div class="grand-reveal__body grand-fields grand-fields--2col">` | a switch (or select) and the fields it reveals as one full-row block, the revealed ones indented right under it; the body hides itself when nothing in it is visible, no script needed | a switch in one grid cell and the fields it opens wherever the two-column flow put them (`UseMultipleWarehouses` with the warehouse table at the bottom of the card) |
| `.grand-facts` > `.grand-fact__label` + `.grand-fact__value` | read-only pairs | `<label class="control-label">value</label>` |
| `.grand-range` around two `<admin-field>` | a from/to pair (dates of one filter) side by side in one grid cell, so "to" never drops to the next row | two fields in separate rows |
| `ul.grand-picture-choice` > `li` > radio + `label` > `img` (add `--large` and a `span.grand-picture-choice__caption` for a preview with its name under it) | a choice between pictures, the checked one ringed in the accent; as child content of `<admin-field>` it keeps the field's label, hint and validation (`Setting/Partials/GeneralCommon.TabStoreInformationSettings.cshtml`, the store theme) | floated `li`s with inline widths and paddings |
| `<admin-field asp-for>` > `div.grand-sentence` with the text and `<admin-input>`s | a value that reads as a sentence with its fields inside it, the numeric boxes narrowed to fit the line; validation for a second property is written in by hand (`Setting/Partials/Sales.TabLoyaltyPoints.cshtml`, "Each [10] USD spent will earn [1] loyalty points.") | text and 22rem-wide inputs strung along a `col-md-8` cell |
| `.grand-empty` (`__icon`, `__title`) | what a card shows instead of an empty grid | nothing |
| `GrandAdmin.modal.confirm(message, { title, confirmText, cancelText })` -> `Promise<bool>` | a panel modal | `confirm()` |
| `data-grand-confirm="message"` (+ `data-confirm-title`, `data-confirm-ok`, `data-confirm-cancel`) on a submit button or link | the panel modal first; on yes the same element is clicked again, so a submit still posts its own `name` / `formaction` | `onclick="return confirm('...')"` |

`<admin-field>` details:
- Builds the control from the model metadata through the same editor templates `<admin-input>` uses, so dates stay date pickers and numbers stay numeric boxes.
- `asp-items` (any `IEnumerable<SelectListItem>`) renders a `form-select`.
- A `bool` renders a switch (`form-check form-switch`, `role="switch"`), never the framework's `.check-box`.
- **Child content replaces the built control** - use it for a textarea (`<admin-textarea>`), a localized editor, a picture or download picker, an input group with a button (see `GoDirectlyToSku` in `Product/List.cshtml`).
- `asp-label-for` takes the label (and its `.Hint`) from another property when `asp-for` has no display name (`ProductAttributeId` -> `ProductAttribute`).
- The label and hint come from the display-name resource and its `.Hint` resource, as `admin-label` did.

A grid with `<grid-toolbar-create>` inside a `.grand-card` gets its add button moved into the card's header by `grid.js` (`cardActionsOf`); you write nothing for it. A detail grid, or a grid outside a card, keeps its toolbar.

## Instructions

### Mandatory Rules
1. Never change an `id`, a `name`, a `data-val-*` attribute, a widget zone (`<partial name="Partials/WidgetZone...">`, `<vc:admin-widget>`), or a tab index. Scripts in the same view and in *other* views look elements up by id - the product Info tab's script shows and hides panels in other tabs, popups post back by `btnId` / `formId`. Grep every id you move, in the view and in whatever opens or refreshes it.
2. Keep a script block byte-identical unless the change is the point. Moving markup is safe; editing the script beside it is a separate change.
3. Check the owning project first. A view in `Grand.Web.AdminShared` reaches Admin, Store and Vendor at once; Store and Vendor also keep their own copies of some partials (`CreateOrUpdate.Info`, `CreateOrUpdate.Prices`, `AttributeCombinationPopup`). Say which panels your change reaches.
4. Exactly one `btn-primary` per screen or popup. If you find two, one of them is wrong. A batch-edit grid (`edit-mode="Batch"`) renders its own primary Save changes, so its screen's filter bar takes `secondary-submit="true"` and Search becomes an outline button (`Product/BulkEdit.cshtml`).
5. Every `Add new` of a grid goes in the card header: `<card-actions>` in an `<admin-card>`, or inside `.grand-card__header` in a hand-written card. The hidden refresh button (`<input type="submit" id="btnRefreshX" style="display: none"/>`) moves with it.
6. No new chrome classes: no `x_*`, `form-horizontal`, `col-md-3/col-md-9` field rows, `mt-checkbox`/`control__indicator`, `btn green|red|blue|purple|default`, `btn-group-devided`, `portlet`, `note note-*` as a heading. `note note-info` stays only for a genuine message ("save the product before adding pictures").
7. No inline `style=` except where jQuery toggles the element: `d-none` and `[hidden]` are `!important` and beat `.show()` / `fadeIn()`, so an element a script shows starts with `style="display: none"`.
8. A field that shows or hides other fields sits in a `.grand-reveal` with those fields in its body, directly under it - never elsewhere in the grid. Keep each revealed field's own `pnl*` id and the script unchanged: the body keys off the `display: none` jQuery writes (`Product/Partials/CreateOrUpdate.Inventory.cshtml`, `Additional.Downloads.cshtml` for a nested reveal). A select as the trigger takes `size="md"`, or `auto-fit` stretches it across the row. A field shown when a switch is *off* (`MaxNumberOfDownloads` under `UnlimitedDownloads`, `TaxCategoryId` beside `IsTaxExempt`) goes next to the switch in the same row, not in a body.
9. A panel a script shows keeps its layout: a `.grand-field` is `display: flex`, and jQuery's `.show()` restores that - but check with `getComputedStyle` after a toggle, it is the first thing to break.
10. Text comes from resources. New keys go in **both** `src/Web/Grand.Web/App_Data/Resources/DefaultLanguage.xml` (UTF-16 LE, lowercase keys, sorted, CRLF - grep does not see into it, use Python or PowerShell) and `Upgrade/en_240.xml`. A 2.4 development database never imports `en_240.xml` (see the migration version gate), so a new key shows raw there: prefer existing keys, and leave a card untitled rather than invent a heading key. Tag helper chrome text goes through `AdminText.Resource(key, englishFallback)`, which never shows a raw key.
11. A gap in a tag helper or component is fixed in the foundation, with a test, not worked around in the view.

### Traps Already Paid For
- **Bundle order.** `admin.ui.css` loads after `admin.bootstrap.css`. A rule in `_components.scss` loses to one in `ui/ui.css` at equal specificity - the tab strip lost its active colour that way. A rule that must beat `ui.css` goes in `ui.css`.
- **The blanket radius reset.** `_custom.scss` (~line 5643) has `.a, code, div, img, label, li, p, pre, select, span, svg, table, td, textarea, th, ul { border-radius: 0 !important }`. Any component that owns a radius needs `!important` until that rule is deleted. It also squared the numeric spinner and every dashboard card.
- **Legacy `.btn` geometry.** `_custom.scss` gives `.btn` gentelella padding and Bootstrap's `btn-primary` is blue; the accent buttons exist only inside `.grand-page` (and so `.grand-popup`). A button outside `<admin-page>` / `<admin-popup>` still looks old.
- **Selector surgery.** Adding a scope to many selectors with a text replacement once turned `.grand-page .btn-sm {` into `.grand-page .btn, .grand-popup .btn-sm {` - every button on every page would have become small and red. Edit selectors by hand, then read every changed selector in the diff. Prefer a class the new scope already carries (`<admin-popup>` renders `grand-page grand-popup` for exactly this reason).
- **`auto-fit` columns.** `repeat(auto-fit, minmax(16rem, 1fr))` gives six columns at 1800px. Two columns is `minmax(max(16rem, 45%), 1fr)`.
- **Nested input groups.** The numeric widget brings its own `.grand-numeric-group.input-group`; a unit (`suffix`) folds it with `display: contents`. Do not wrap a numeric field in another `.input-group` by hand.
- **The empty option.** In a filter an empty value is "nothing chosen" and is drawn as the placeholder; in a grid cell it is a real value ("All customer groups", "All stores") and the grid's select editor draws it (`emptyIsChoice`). Do not flip either default.
- **Static asset fingerprints.** After `npm run build`, restart Grand.Web: assets are served with an immutable cache under a fingerprint computed at startup, and the browser keeps the old stylesheet otherwise.
- **Bundle noise.** `npm run build` rewrites every bundle; restore the ones whose diff is line endings only (`git diff --stat --ignore-cr-at-eol`) before committing.
- **A filter bar inside a tab.** A tab pane is not a flex column, so `<admin-filters>` followed by a grid card needs the `.grand-tabstrip .grand-filters ~ .grand-card` gap rule (`_components.scss`); the affiliate Orders tab is the example. In a tab whose screen already has Save, the bar takes `secondary-submit="true"`.
- **`disabled="@x"` on `<admin-input>` does nothing.** The helper suppresses its own element, so an unbound `disabled` never reaches the control. A read-only form (a global attribute a store owner opens) wraps its fields in `<fieldset disabled="@isReadOnly">` instead (`CustomerAttribute/Partials/CreateOrUpdate.TabInfo.cshtml`).
- **The store scope owns its card.** `@await Component.InvokeAsync("StoreScope")` renders a whole `.grand-card` with the scope select as a field, and nothing at all on a single-store installation. Write it straight into `<admin-page>`; an `<admin-card>` around it stays behind as an empty 34px box (`Payment/Settings.cshtml`).
- **`.grand-page .btn` is `display: inline-flex`.** It comes after `_custom.scss` at the same weight as a legacy `.x .some-state { display: none }`, so a button a state class used to hide shows again once its screen moves into `<admin-page>` - the plugin list showed Install on installed plugins. Restate the hiding rule one class heavier in `_components.scss` (`.grand-card .plugins-table .button-installed-true`).
- **Views compile into the DLL.** AdminShared, Store and Vendor views are precompiled; a view change needs `dotnet build src/Web/Grand.Web/Grand.Web.csproj` and a restart, not only a refresh.

### Procedure For One Screen
1. List the ids the view and its scripts use (`grep -o 'id="[^"]*"'`, `$('#...')`), and who opens or refreshes it.
2. Replace the shell with `<admin-page>` (or `<admin-popup>`); move the header buttons into `<page-actions>`, destructive ones into `<admin-action-menu>`.
3. Turn each `note note-info` heading + `form-horizontal` block into an `<admin-card>`; each field row into `<admin-field>`; pair related fields in `.grand-fields--2col`; units into `suffix`; checkboxes stay `<admin-field>` (they become switches).
4. Put each grid in a flush card; move its Add new into the card header.
5. Replace `confirm()` / `alert()` with `GrandAdmin.modal.confirm`; an inline `onclick="return confirm(...)"` on a submit button or link becomes `data-grand-confirm` (`adminapp/src/ui/confirm.js`, one capture-phase listener for the whole page, content loaded later included).
6. Build (`npm run build` if CSS/JS changed; `dotnet build` the owning project and Grand.Web), restart, open the screen.

## Constraints
- Do not delete legacy classes from `_custom.scss` while any view - including third-party plugin views - may still use them.
- Do not introduce another UI library; Bootstrap 5.3, bootstrap-icons, Tabulator and Tom Select are the whole kit (MIT/Apache only).
- Do not scope new rules to a single screen's id; if a screen needs something, every screen will.

## Expected Output
The converted files with before/after line counts (a converted view gets shorter), the panels each change reaches, what was verified in the running app and how, and what was left.

## Validation Checklist
- [ ] Every id, name, widget zone and tab index the view had is still there; every script selector still matches.
- [ ] One `btn-primary`; destructive actions `btn-outline-danger` / in the overflow menu.
- [ ] Every grid's Add new is in its card header.
- [ ] Toggled panels keep their layout after hide/show.
- [ ] Light and dark mode read from tokens only (no hex in the view).
- [ ] No legacy chrome class, no inline `style=` except script-toggled elements.
- [ ] New resources in both resource files, or none.
- [ ] `npx vitest run` (adminapp) and `dotnet test src/Tests/Grand.Web.Common.Tests` pass when shared code changed.
- [ ] Bundles rebuilt and committed with the source; line-ending-only bundle diffs restored.
- [ ] Opened in the running app after a restart; console clean.

## Examples
- List screen: `src/Web/Grand.Web.Admin/Areas/Admin/Views/Product/List.cshtml` - page, overflow menu, filters with a quick search, bulk bar, grid in a flush card, name + SKU cell, Published chip.
- Detail screen: `src/Web/Grand.Web.AdminShared/Views/AdminShared/Product/Edit.cshtml` and `Product/Partials/CreateOrUpdate.Info.cshtml` - sticky page, overflow menu with Delete, cards of two-column fields, facts.
- Units and prices: `Product/Partials/CreateOrUpdate.Prices.cshtml` - `suffix`, a card with `<card-actions>` for a popup-opening Add new.
- Grid tabs: `Product/Partials/CreateOrUpdate.RelatedProducts.cshtml` - a hand-written card with the Add new link in its header.
- Popup: `Product/TierPriceCreatePopup.cshtml` + `Partials/CreateOrUpdateTierPrice.cshtml` - `<admin-popup>`, fields, `.grand-popup__actions` with one primary.
- The style was first drawn as a standalone mockup; it is not kept in the repository because the reference screens above superseded it wherever the two differ.
