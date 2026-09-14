# Admin panel frontend

The npm project for the Admin, Store and Vendor panels. All three panels load their
assets from `Grand.SharedUIResources/wwwroot/administration/`, so the project lives
next to that directory. It is the second npm project in the solution; the storefront
has its own in `src/Web/Grand.Web/vueapp`, and the conventions here follow it.

What exists today:

| path | contents |
| --- | --- |
| `src/admin.core.js` | bundle entry; only reserves `window.GrandAdmin` |
| `scripts/codemods/analyze-kendo-grids.mjs` | read-only inventory and A/B/C classification of every `kendoGrid` in the views |
| `scripts/codemods/lib/` | Razor masking and grid analysis used by the analyzer (unit tested) |
| `e2e/` | Playwright smoke specs for the three panels and a HAR recorder |

Nothing in this project is loaded by `HeadAdmin`, `HeadStore` or `HeadVendor` yet;
the panels run exactly on the vendored files in `wwwroot/administration`.

## Setup

```
npm install
```

## Build

```
npm run build
```

Writes `admin.core.js` to `../wwwroot/administration/bundles/` as an IIFE (loaded by a
plain `<script src>`, like the storefront bundle). `emptyOutDir` is off because the
output directory sits inside the vendored `wwwroot/administration` tree, and `vue` is
aliased to the esm-bundler build that includes the template compiler.

**The build output is not committed yet.** No view references the bundle, so shipping
it would only add an unused static web asset. Do not commit
`wwwroot/administration/bundles/` until the first change that loads it from a `Head*`
partial; from that change on the bundle is committed together with its source, as in
`vueapp`.

## Lint and test

```
npm run lint
npm test
```

`npm test` runs Vitest once over `scripts/**/*.test.mjs` and `src/**/*.test.js`.

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

`npm run e2e:har` records one HAR per page listed in `e2e/support/har-pages.js`. For
grids marked `captureUpdate` it opens inline edit, clicks Update without changes and
answers the request inside the browser, so the serialised payload is recorded
(`*.update.json`) and the server never receives it. HAR files contain session cookies,
antiforgery tokens and store data - `e2e/har/` is git-ignored and the files must not
be shared outside the team.
