# Admin panel frontend

The npm project that builds the scripts and stylesheets of the Admin, Store and Vendor
panels. All three panels load their assets from
`Grand.SharedUIResources/wwwroot/administration/`, so the project lives next to that
directory. It is the second npm project in the solution; the storefront has its own in
`src/Web/Grand.Web/vueapp`, and the conventions here follow it (IIFE output, `vue` aliased
to the esm-bundler build, `emptyOutDir` off, eslint flat config).

The folder is visible in Visual Studio's Solution Explorer under `Grand.SharedUIResources`
as plain `None` items. `Grand.SharedUIResources.csproj` keeps it out of the library's
content and static web assets (`DefaultItemExcludes`) and never copies, publishes or packs
it; `node_modules`, `dist`, `reports` and the e2e output folders are left out of the tree.

## Prerequisites

- **Node.js 20.19 or newer** (with npm). `package.json` has no `engines` field and there is
  no `.nvmrc`; the floor comes from the dependencies: sass requires Node >= 20.19, jsdom and
  Playwright >= 20. The project is developed on Node 23.
- Only needed when you change something under `adminapp/`. The bundles are committed, so
  the panels run without Node, and neither the CI workflows nor the Dockerfile install it.

## Setup

```
cd src/Web/Grand.SharedUIResources/adminapp
npm ci
```

`npm ci` installs exactly what `package-lock.json` pins. Use `npm install <package>` only
to add or upgrade a dependency, and commit the lock file with it. Playwright's browser is
not installed by npm; the e2e scripts need it once:

```
npx playwright install chromium
```

## Scripts

| command | what it does |
| --- | --- |
| `npm run build` | builds the shipped bundles into `../wwwroot/administration/bundles/` (see below); `npm run build -- <entry>` builds one named entry, e.g. `admin.core` |
| `npm run lint` | eslint over the whole project (`src`, `scripts`, `e2e`, config files) |
| `npm test` | Vitest, once, over `src/**/*.test.js` |
| `npm run e2e` | Playwright smoke specs for the three panels against a running instance (all projects; pass `-- --project=en-US` etc. to narrow) |
| `npm run e2e:visual` | one full-page screenshot per page of `e2e/support/visual-pages.js` |
| `npm run e2e:payloads` | records what the admin forms would post, without sending it |
| `npm run e2e:har` | records one HAR file per page of `e2e/support/har-pages.js` into `e2e/har/` |
| `npm run audit:prod` | `npm audit` of the production dependencies at moderate level |

## What the build produces

`scripts/build.mjs` runs one Vite build per entry (an IIFE build cannot be split across
inputs), then mirrors the stylesheet into its right-to-left copy with rtlcss. Output, in
`src/Web/Grand.SharedUIResources/wwwroot/administration/bundles/`:

| file | from | contents |
| --- | --- | --- |
| `admin.bootstrap.js`, `admin.bootstrap.css` | `src/admin.bootstrap.js`, `src/styles/` | Bootstrap 5.3 with Popper 2 (`window.bootstrap`, plus Bootstrap's jQuery plugins), bootstrap-icons and the panel stylesheet |
| `admin.bootstrap.rtl.css` | generated from `admin.bootstrap.css` | the right-to-left stylesheet: rtlcss output plus `src/styles/_rtl.scss` |
| `fonts/bootstrap-icons.woff`, `.woff2` | `node_modules/bootstrap-icons` | the icon font |
| `admin.grid.js`, `admin.grid.css` | `src/admin.grid.js`, `src/grid/` | the `<admin-grid>` runtime, an adapter over Tabulator 6: `window.GrandAdmin.grids` |
| `admin.ui.js`, `admin.ui.css` | `src/admin.ui.js`, `src/ui/` | the widgets: `window.GrandAdmin.modal`, `.tabs`, `.numeric`, `.dateInput`, `.select`, `.format`, `.ui.init` (Tom Select for the lists) |

`admin.core.js` (`src/admin.core.js`, only reserves `window.GrandAdmin`) is not loaded by
any panel; a plain `npm run build` does not write it.

**The bundles are committed together with the source that produced them.** Every change
under `adminapp/src` (or a dependency upgrade) needs `npm run build` and the rebuilt files
in the same commit; never edit a bundle by hand. A rebuild of unchanged sources must leave
`git status` clean - that is the quickest check that the committed bundles match.

## How the panels load the bundles

`HeadAdmin.cshtml`, `HeadStore.cshtml` and `HeadVendor.cshtml` (the `Views/Shared/Partials`
of each area) load, under `Constants.WwwRoot` + `/administration/`:

1. `admin.theme.js` synchronously in `<head>`, before anything paints (dark mode, below);
2. the vendored stylesheets still in use (daterangepicker, magnific-popup, summernote,
   the jQuery UI smoothness theme, elFinder in Admin, farbtastic), then
   `bundles/admin.grid.css`, `bundles/admin.ui.css`, and
   `bundles/admin.bootstrap.css` - or `bundles/admin.bootstrap.rtl.css` when the working
   language is right-to-left and `IgnoreRtlPropertyForAdminArea` is off;
3. in the head: jQuery, jQuery UI, moment, daterangepicker, typeahead (with
   `admin.search.js` in Admin), jquery.validate (+ unobtrusive), `admin.common.js`, then
   `bundles/admin.grid.js`, `bundles/admin.ui.js`;
4. in the footer: `bundles/admin.bootstrap.js`, `build/js/smartresize.js`,
   `build/js/custom.js`, summernote, elFinder (Admin), magnific-popup, jquery.tmpl,
   farbtastic.

They also render `<admin-culture/>`, the request culture as a JSON island the bundles read.
The login layouts (`_AdminLoginLayout`, `_StoreLoginLayout`, `_VendorLoginLayout`) load
only `admin.theme.js`, `admin.bootstrap.css/js`, jQuery, moment, jQuery UI and validation.
Views load a few libraries themselves: Chart.js (`chart.min.js`) in the report and
dashboard chart components, CodeMirror in the views that edit CSS/JS/HTML, the tag editor
in product, blog and customer edit, Fine Uploader through the picture and download editor
templates.

## Styles

`src/styles/admin.scss` is the one stylesheet, in this order:

| file | role |
| --- | --- |
| `_variables.scss` | Bootstrap settings read before Bootstrap: 14px base size, radii, dense table cells, the icon font directory |
| `bootstrap/scss/bootstrap` | Bootstrap 5.3 |
| `bootstrap-icons` + `_icons.scss` | the icon font and the two helpers Font Awesome used to give |
| `_custom.scss` | the panel chrome (top bar, `x_panel` cards, buttons, notes) and the dark palette |
| `_sidebar.scss` | the side menu |
| `_forms.scss` | form fields, switches, tag editor, summernote |
| `_dashboard.scss` | the dashboard and Statistics pages of the three panels: header, welcome card, stat cards, report cards |
| `_topbar.scss` | the top bar of the three panels: search field, store / vendor pill, icon buttons, user menu, theme switch, drop-downs; one flex row whose buttons run row-reverse, so right-to-left mirrors it without rules of its own |
| `_zindex.scss` | what stacks above the chrome (popups, pickers), in terms of Bootstrap's z-index properties |

**Light and dark.** Everything is written against Bootstrap's `--bs-*` custom properties,
so Bootstrap 5.3's colour mode (`data-bs-theme` on `<html>`) themes it. `_custom.scss`
gives the dark mode the panel's slate palette by redefining those variables under
`[data-bs-theme="dark"]`; the `[data-theme="dark"]` rules are for chrome Bootstrap does not
draw. The grid paints itself from `--grand-grid-*` tokens in `src/grid/grid.css` - a
Bootstrap variable where the light look is one, otherwise a light value with its dark one
in a single `[data-bs-theme="dark"]` block; the last block hands Tabulator's own dark
surfaces back to those tokens. The widgets (`src/ui/ui.css`) follow the same
variables. Add a new colour as a token with both
values, never as a literal in a rule.

**Right-to-left.** `admin.bootstrap.rtl.css` is generated: rtlcss mirrors the left-to-right
output, so the two cannot drift. Write inline-axis geometry with logical properties
(`margin-inline-start`, `inset-inline-end`, `text-align: start`), which rtlcss leaves alone
and the browser flips itself; `float` stays physical and rtlcss flips it. What must differ
beyond mirrored geometry goes in `src/styles/_rtl.scss`, compiled separately and prepended
to the RTL file - today the IRANSans webfont, read from `wwwroot/administration/build/fonts/`.

`x_panel`, `x_title` and `x_content` keep their names and are written in terms of
Bootstrap's card custom properties; a panel becomes a bordered card by setting
`--bs-card-border-width` on it.

## The culture comes from the server, never from the browser

Everything the grid and the widgets format or parse is decided by `<admin-culture>`
(`Grand.Web.Common/TagHelpers/Admin/AdminCultureTagHelper.cs`): number and date patterns,
month and day names, first day of the week and the few texts the widgets show.
`src/ui/culture.js` reads it; `src/grid/format.js` formats with it and
`src/ui/dateparse.js` parses with it. Nothing may fall back to `navigator.language`,
`Intl`, `Date#toLocaleString` or a native `<input type="date">`: what the panels post is
read by the MVC model binder in the **store's** request culture. The `foreign-locale`
Playwright project drives the widget spec from an ar-SA browser to keep it honest.

## Adding a grid

Grids are markup, rendered by the `<admin-grid>` tag helper
(`Grand.Web.Common/TagHelpers/Admin/AdminGridTagHelper.cs`) and brought to life by
`admin.grid.js`. The server contract is unchanged: `DataSourceRequest` in,
`DataSourceResult { Data, Total, Errors }` out.

```cshtml
<admin-grid id="measureweight-grid"
            read-url="@Url.Action("Weights", "Measure", new { area = Constants.AreaAdmin })"
            update-url="..." destroy-url="..."
            pager="Compact" edit-mode="Inline">
    <grid-text name="markAsPrimary" value="@Loc["Admin.Configuration.Measures.Weights.Fields.MarkAsPrimaryWeight"]"/>
    <grid-column field="Name" title="@Loc["..."]" width="300" editor="Text"/>
    <grid-column field="Ratio" title="@Loc["..."]" editor="Numeric" decimals="8"/>
    <grid-column field="Id" title="@Loc["..."]" editable="false">
        <cell-template>
            <a class="btn btn-default btn-sm" data-grid-click="markAsPrimaryWeight">
                <i data-if="IsPrimaryWeight" class="bi bi-check-lg"></i><i data-else class="bi bi-x-lg"></i>
                {{ $texts.markAsPrimary }}
            </a>
        </cell-template>
    </grid-column>
    <grid-commands edit="true" destroy="true" width="200"/>
    <grid-detail read-url="..." param="weightId:Id">
        <grid-column field="Name" title="@Loc["..."]"/>
    </grid-detail>
</admin-grid>
```

- `<grid-column>`: `field`, `title`, `width`, `format`, `editor` (`Text`, `Numeric`,
  `Integer`, `Checkbox`, `Date`, `DateTime`, `Select`, `Custom`), `editable`,
  `min-screen-width`; conditional columns are plain `@if` around the element.
- `<cell-template>`: `{{ Field }}` is encoded text, `{{ Field | n2 }}` formatted,
  `{{{ Field }}}` raw HTML (only for markup the server builds), `data-if` / `data-else` for
  simple conditions, `data-grid-click="globalFunction"` receives `(dataItem, event, grid)`.
  Nothing is evaluated as JavaScript. Localized text goes through `<grid-text>` and
  `{{ $texts.name }}`, never `@Html.Raw` inside a template.
- `<grid-commands>`: Edit/Delete buttons, `visible-if` per row; `<grid-toolbar-create/>`
  adds a row; `edit-mode="Batch"` with `<grid-toolbar-save/>` / `<grid-toolbar-cancel/>`
  edits cells in place.
- `<grid-detail>`: a detail grid per expanded row, parameters from the master row.
- `selectable="Checkbox"` or `"Row"`, `on-change`, `on-data-bound`, `auto-bind="false"` and
  the rest are listed in `.ai/standards/razor-frontend.md` (section *Admin grids*), which is
  the full reference.
- `GrandAdmin.grids.get('#products-grid')` (an id, a selector, an element or a jQuery
  object) returns a grid's API for the calls views make (`dataSource.read`,
  `dataItem(tr)`, `select()`, `refresh()` ...).

Only views change for a new grid; no rebuild is needed unless you touch `src/grid`.

## Adding a widget

Widgets live in `src/ui/`, one module per widget with a `*.test.js` next to it (Vitest,
`// @vitest-environment jsdom` for DOM tests):

1. write `src/ui/<widget>.js` exporting a `create...(element, options)` and an
   `init...(root)` that upgrades every `[data-grand-<widget>]` in `root` (as `numeric.js`,
   `datetime.js` and `select.js` do); read the
   culture and texts from `culture.js`, never from the browser;
2. register it in `src/admin.ui.js` on `window.GrandAdmin` and in `GrandAdmin.ui.init`,
   and add its attribute to the `MutationObserver` selector so markup inserted later
   (popups, AJAX partials) is upgraded too;
3. style it in `src/ui/ui.css` with `--bs-*` variables so light, dark and RTL follow;
4. emit the `data-grand-<widget>` attribute from an editor template in
   `Grand.SharedUIResources/Views/Shared/EditorTemplates/` rather than from each view;
5. `npm run lint && npm test && npm run build`, and commit the rebuilt `admin.ui.*` with
   the source.

## For plugin authors: what was removed

Kendo UI, Bootstrap 4, Font Awesome and simple-line-icons are no longer loaded by any
panel, and their files are gone from `wwwroot/administration/`. So are Roxy Fileman, the
unused jQuery UI sources/stylesheets/themes, the glyphicons font, unused webfonts, the
unminified duplicates of elFinder and Fine Uploader, and the CodeMirror demo pages. A
plugin that linked any of those paths directly must ship its own copy.

There is no compatibility layer either: `$.fn.kendoGrid`, the other Kendo widgets,
`kendo.*` helpers, the `k-*` classes and the Bootstrap 4 `data-toggle` / `data-target` /
`data-dismiss` attributes do nothing in the panels. A plugin view uses `<admin-grid>`,
`window.GrandAdmin` and Bootstrap 5 (`data-bs-*`). A grid's API is
`GrandAdmin.grids.get('#grid')`; `$('#grid').data('kendoGrid')` is gone as well.
The step-by-step upgrade, with the Kendo-to-`<admin-grid>` mapping, is
`.ai/prompts/upgrade-kendo-view.md`.

## Lint and test

```
npm run lint
npm test
```

The serializer tests compare against `jQuery.param` from jQuery 2.2.4, a dev dependency
only (the panels load their own copy).

## End-to-end smoke tests

The specs run against an instance that is already running; nothing starts the application
for you. They only read: pages open, grids load, inline edit is opened and cancelled, tabs
and detail rows expand. Signing in updates the account's last-login data, and a configured
language code changes the account's working language. `e2e:payloads` answers the form
requests inside the browser, so the server never receives them.

Environment variables (names only - never commit values):

| variable | meaning |
| --- | --- |
| `GRAND_ADMIN_URL` | base URL of the instance, default `http://localhost:5000` |
| `GRAND_ADMIN_EMAIL`, `GRAND_ADMIN_PASSWORD` | administrator account |
| `GRAND_STORE_EMAIL`, `GRAND_STORE_PASSWORD` | store manager account |
| `GRAND_VENDOR_EMAIL`, `GRAND_VENDOR_PASSWORD` | vendor account |
| `GRAND_LANGUAGE_CODE_EN` | optional SEO code to switch to before the en-US tests |
| `GRAND_LANGUAGE_CODE_PL` | SEO code of a Polish language; the `pl-PL` project is skipped without it |
| `GRAND_LANGUAGE_CODE_RTL` | SEO code of a right-to-left language; the `rtl` project is skipped without it and expects `IgnoreRtlPropertyForAdminArea` off |
| `GRAND_SHOT_DIR` | `e2e:visual` target: a name under `e2e/screenshots/` or an absolute path (default `current`) |
| `GRAND_THEME` | `dark` makes `e2e:visual` record the dark mode |
| `GRAND_PAYLOAD_DIR` | `e2e:payloads` target directory (default `e2e/payloads/current`) |

A panel whose credentials are not set is skipped. Tests run on one worker: every locale
project switches the same account's language.

```
npm run e2e -- --project=en-US --project=foreign-locale
npx playwright test --config e2e/playwright.config.js --list
```

`reports/`, `e2e/har/`, `e2e/payloads/`, `e2e/screenshots/`, `e2e/test-results/` and
`e2e/playwright-report/` are git-ignored: they hold session cookies, antiforgery tokens,
form payloads and store data. Never commit or share them outside the team.

## Dark mode

The switch in the panel header stores the choice in `localStorage` (`theme`: `dark` or
`light`). `wwwroot/administration/admin.theme.js`, loaded synchronously in `<head>` of the
three panels and their login pages, applies it before the first paint as `data-bs-theme`
and `data-theme` on `<html>`; nothing stored means light.

## Dependencies

Bundled into the panels: Bootstrap 5.3 (MIT), bootstrap-icons (MIT), Tabulator 6 (MIT),
Vue 3 (MIT) and Tom Select 2 (Apache-2.0); sass and rtlcss (both MIT) build the
stylesheet. No asset is loaded from a CDN. The tab strip, numeric field and date picker
are this repository's own code. Only free licenses: no paid UI library may be added.
