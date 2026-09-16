# Admin panel frontend

The npm project for the Admin, Store and Vendor panels. All three panels load their
assets from `Grand.SharedUIResources/wwwroot/administration/`, so the project lives
next to that directory. It is the second npm project in the solution; the storefront
has its own in `src/Web/Grand.Web/vueapp`, and the conventions here follow it.

What exists today:

| path | contents |
| --- | --- |
| `src/admin.grid.js` | bundle entry of the `<admin-grid>` runtime: registers `window.GrandAdmin.grids` |
| `src/grid/` | the adapter over Tabulator 6: data source and transport (server contract, `jQuery.param` serialization), row editing and editors, eval-free cell templates, culture formatting, pager, `$(el).data('kendoGrid')` API |
| `src/admin.ui.js` | bundle entry of the widgets that used to be Kendo UI: `window.GrandAdmin.modal`, `.tabs`, `.numeric`, `.dateInput`, `.select`, `.format` |
| `src/ui/` | tab strip, modal, numeric text box, date/time inputs, Tom Select lists, and the page culture island the `<admin-culture>` tag helper renders |
| `src/admin.legacy.js`, `src/legacy/` | the Kendo compatibility layer the three panels load: the `$.fn.kendoGrid` shim, the Kendo template compiler, the widget mini-shims (`kendoWindow`, `kendoNumericTextBox`, `kendoDropDownList`, `kendoMultiSelect`, `kendoTabStrip`) and the transitional stylesheet of the `k-*` classes the views still carry |
| `src/admin.core.js` | bundle entry; only reserves `window.GrandAdmin` |
| `scripts/codemods/analyze-kendo-grids.mjs` | read-only inventory and A/B/C classification of every `kendoGrid` in the views |
| `scripts/codemods/kendo-grid-to-admin-grid.mjs` | codemod converting `kendoGrid` initialisations into `<admin-grid>` markup |
| `scripts/codemods/lib/` | Razor masking, grid analysis, template and grid conversion used by the analyzer and the codemod (unit tested) |
| `e2e/` | Playwright smoke specs for the three panels and a HAR recorder |

`HeadAdmin`, `HeadStore` and `HeadVendor` load `admin.grid.js`, `admin.grid.css`,
`admin.ui.js`, `admin.ui.css`, `admin.legacy.js` and `admin.legacy.css`. Every grid runs on
`<admin-grid>` and every widget on `admin.ui.js`; **no panel loads Kendo any more**. The
vendored `wwwroot/administration/kendo/` tree stays in the repository, unreferenced, for one
major release so an installation that still needs it can link it back, and is deleted after
that release.

`admin.legacy.js` is there for third-party plugin views written against Kendo. It registers
`$.fn.kendoGrid` and the `kendoWindow`, `kendoNumericTextBox`, `kendoDropDownList`,
`kendoMultiSelect` and `kendoTabStrip` mini-shims over `window.GrandAdmin`, plus
`kendo.toString`, `kendo.htmlEncode`, `kendo.culture` and `kendo.parseDate`; each warns once
that it is a transitional shim. It registers nothing when Kendo itself is loaded, so an
installation that puts the Kendo tags back keeps the real widgets. `admin.legacy.css`
reproduces what the Kendo stylesheets painted for the `k-button`, `k-link`, `k-icon` and
`k-input` classes that around 800 elements still carry, with the `k-i-*` glyphs mapped onto
the Font Awesome 4.6 the panel already loads. Both halves go away with those classes.

Dependencies bundled into the panels: Tabulator 6 (MIT), Vue 3 (MIT) and
Tom Select 2 (Apache-2.0). No asset is loaded from a CDN.

## Setup

```
npm install
```

## Build

```
npm run build
```

`scripts/build.mjs` runs one Vite build per entry (an IIFE build cannot be split across
inputs) into `../wwwroot/administration/bundles/`, each loaded by a plain `<script src>`.
A plain `npm run build` writes only the bundles a panel loads (`admin.grid.js`,
`admin.grid.css`, `admin.ui.js`, `admin.ui.css`, `admin.legacy.js`, `admin.legacy.css`);
`npm run build -- admin.core` builds the unused entry on request. `emptyOutDir` is off because the output
directory sits inside the vendored `wwwroot/administration` tree, and `vue` is aliased to
the esm-bundler build that includes the template compiler.

The six shipped files are committed together with the source that produced them, as in
`vueapp`. Do not commit `admin.core.js` built on request until a panel loads it.

## Lint and test

```
npm run lint
npm test
```

`npm test` runs Vitest once over `scripts/**/*.test.mjs` and `src/**/*.test.js`. Browser-API
tests opt into jsdom with `// @vitest-environment jsdom`; the serializer tests compare
against `jQuery.param` from jQuery 2.2.4 (a dev dependency only, the panels load their own
copy).

## Kendo grid analyzer

```
npm run analyze:grids
```

Scans every `.cshtml` under `src/Web` and `src/Plugins` (without `bin`, `obj`,
`node_modules` and its own `__tests__` fixtures), masks the Razor inside `<script>` elements, parses each
`kendoGrid({ ... })` configuration with acorn and classifies it:

- **A** clean - standard options, standard transports, templates with encoded output
  and simple conditionals;
- **B** convertible with review - column `editor` functions, `parameterMap`,
  `dataBound`/`edit`/`save`/`change` handlers, detail grids, raw `#= #` output of
  non-id fields, custom commands, `@if` blocks inside the configuration;
- **C** manual - partials injecting configuration, dynamic `dataSource`/`columns`,
  configuration that does not parse after masking.

It writes `reports/kendo-grids.csv` (one row per grid) and
`reports/kendo-grids.summary.md` (totals, per project, reasons, formats, external
`.data("kendoGrid")` usage). `reports/` is git-ignored. Options:
`-- --root <repo root>`, `-- --out <dir>`.

## Kendo grid codemod

```
npm run codemod:grids -- --path Grand.Web.AdminShared/Views/AdminShared/Product   # dry run, prints a diff
npm run codemod:grids -- --path Grand.Web.AdminShared --write                     # rewrites the views
```

Stage 2 of the migration: replaces each `kendoGrid({ ... })` with `<admin-grid>` markup in
place of its `<div id="...">` placeholder, in the style of the hand-converted pilot views,
and removes the grid script (plus a `detailInit` function it owned and the
`$(document).ready` / `<script>` wrappers left empty). Kendo templates become cell templates
(`#: #` and `#= #` output is encoded, `# if #` becomes `data-if` / `data-else`, localized
text moves to `<grid-text>`), Razor `@if` column blocks are kept, and the schema field types
of inline-edit grids become editors. A checkbox column whose value is another field than the
key (`Ids` holding `id:productId`) makes that field the `key` of a read-only grid, so
`selectedIds` post what the checkboxes posted.

Every grid ends in one of three states, listed in the summary:

- **converted** - nothing to review;
- **converted with review markers** - `@* CODEMOD-REVIEW: reason *@` comments sit next to what
  a person must check (raw `#= #` output of fields that look like server-built HTML, custom
  Kendo editors, checkbox columns wired by hand, `dataBound` handlers, inline `on*` handlers
  in templates). Resolve and remove every marker before committing;
- **skipped** - left on Kendo with the reasons (partials injecting columns, the same grid
  configured in several Razor branches, `batch`/`incell` editing, custom toolbars,
  `requestEnd` handlers other than the reload pattern, template code it cannot translate).

Options: `--path <text>` (repeatable, matched against the repository path), `--write`,
`--quiet` (summary only), `--root <repo root>`. The fixtures in
`scripts/codemods/__tests__/fixtures` are pilot views before their conversion and the
output a reviewer starts from.

## End-to-end smoke tests

The specs run against an instance that is already running; nothing starts the
application for you. They only read: pages are opened, grids load, inline edit is
opened and cancelled, tabs and detail rows are expanded. Nothing is saved, deleted or
uploaded. Signing in updates the account's last-login data, and a configured language
code changes the account's working language.

Browsers are not installed by `npm install`:

```
npx playwright install chromium
```

Environment:

| variable | meaning |
| --- | --- |
| `GRAND_ADMIN_URL` | base URL of the instance, default `http://localhost:5000` |
| `GRAND_ADMIN_EMAIL`, `GRAND_ADMIN_PASSWORD` | administrator account |
| `GRAND_STORE_EMAIL`, `GRAND_STORE_PASSWORD` | store manager account |
| `GRAND_VENDOR_EMAIL`, `GRAND_VENDOR_PASSWORD` | vendor account |
| `GRAND_LANGUAGE_CODE_EN` | optional SEO code to switch to before en-US tests |
| `GRAND_LANGUAGE_CODE_PL` | SEO code of a Polish language; the `pl-PL` project is skipped without it |
| `GRAND_LANGUAGE_CODE_RTL` | SEO code of a language with `Rtl` on; the `rtl` project is skipped without it and expects `IgnoreRtlPropertyForAdminArea` off |

A panel whose credentials are not set is skipped.

```
npm run e2e                       # all locale projects
npm run e2e -- --project=en-US    # one locale
npm run e2e:har                   # record HAR files into e2e/har/
npx playwright test --config e2e/playwright.config.js --list
```

Tests run on one worker: every locale project switches the same account's language.

`npm run e2e:visual` records one full-page screenshot per entry of
`e2e/support/visual-pages.js` into `e2e/screenshots/<GRAND_SHOT_DIR>` (default `current`),
so the look of two builds can be compared page by page. `e2e/screenshots/` is git-ignored:
the pages show real store data.

`npm run e2e:har` records one HAR per page listed in `e2e/support/har-pages.js`. For
grids marked `captureUpdate` it opens inline edit, clicks Update without changes and
answers the request inside the browser, so the serialised payload is recorded
(`*.update.json`) and the server never receives it. HAR files contain session cookies,
antiforgery tokens and store data - `e2e/har/` is git-ignored and the files must not
be shared outside the team.
