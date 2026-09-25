# Skill: frontend-bundle-workflow

## Purpose

Guide agents making frontend changes in GrandNode's storefront: when to run the build, what gets rebuilt, and that the output must be committed alongside source. The admin panels' npm project is covered in [Admin Panel Project](#admin-panel-project) at the end.

---

## Project Location

Two npm projects:

| Project | Serves | Output |
|---------|--------|--------|
| `src/Web/Grand.Web/vueapp/` | storefront | `src/Web/Grand.Web/wwwroot/bundles/` (committed) |
| `src/Web/Grand.SharedUIResources/adminapp/` | Admin, Store and Vendor panels | `src/Web/Grand.SharedUIResources/wwwroot/administration/bundles/` (`admin.grid.*` committed, see below) |

The old webpack project at `Grand.Web/` root was removed. Everything up to [Admin Panel Project](#admin-panel-project) is about `vueapp`.

---

## Output Files (Committed to Git)

All output lives in `src/Web/Grand.Web/wwwroot/bundles/` and is **tracked by git**:

| File | Contents | Rebuilt by |
|------|----------|------------|
| `app.runtime.bundle.js` | Vue 3 + compat layer + views + behaviours + theme scripts | `vite build` |
| `libs.css` | Bootstrap 5 + Bootstrap Icons + Pikaday + animate.css | `vite build` |
| `style.min.css` | Six theme CSS parts minified and concatenated | `build-theme-css.mjs` |
| `style.rtl.min.css` | Same as above, RTL variants | `build-theme-css.mjs` |
| `fonts/` | Bootstrap Icons font files | written once, `emptyOutDir: false` preserves them |

`npm run build` runs both steps in sequence. Never run only `vite build` directly if theme CSS has changed.

---

## When to Run `npm run build`

Run the build after changing **any** of:

- `vueapp/src/**` — Vue views, behaviours, theme scripts, compat layer
- `wwwroot/theme/css/**` — theme stylesheets (`common/`, `header/`, `catalog/`, `product/`, `customer/`, `cart/`)
- `vueapp/package.json` — dependency change (after `npm install`)

Do **not** run the build for:
- Server-side C# / Razor changes that don't touch JS or CSS
- Changes inside `wwwroot/theme/script/app.js` or `Content/script/app.js` (theme-owned, not bundled here)
- Changes to `public.checkout.js` (loaded separately, not part of this bundle)

---

## Build Commands

```bash
# one-time setup
cd src/Web/Grand.Web/vueapp
npm install

# production build (always use this, not bare vite build)
npm run build

# lint
npm run lint

# dev server (hot reload against a running ASP.NET backend)
npm run dev
```

`npm run dev` is optional. Changes are visible instantly via the dev server but still require a `npm run build` + commit before the bundle in the repo reflects them.

---

## Theme CSS Pipeline

Source files are **not** directly served. `Head.cshtml` uses the raw files in Development but loads the minified bundles in Production.

Cascade order (must not change):

```
common → header → catalog → product → customer → cart
```

Each part folder contains a regular file and an RTL variant:
```
wwwroot/theme/css/
  common/common.css  common/common.rtl.css
  header/header.css  header/header.rtl.css
  catalog/...
  product/...
  customer/...
  cart/...
```

`build-theme-css.mjs` generates `style.min.css` from the LTR files and `style.rtl.min.css` from the RTL variants using esbuild. The script is invoked automatically by `npm run build`.

---

## Commit Rules

**Always commit the built output in the same commit as the source change.**

The CI pipeline (`azure-pipelines.yml`) has no frontend build step — bundles are intentionally version-controlled. A PR that changes source files without updating `wwwroot/bundles/` will ship stale output.

Checklist before committing a frontend change:

1. Run `npm run build` from `vueapp/`.
2. Stage source files AND the changed files in `wwwroot/bundles/` together.
3. Verify `app.runtime.bundle.js`, `libs.css`, `style.min.css`, `style.rtl.min.css` are all staged when relevant.
4. Do **not** stage `fonts/` unless bootstrap-icons was updated — they don't change on every build.

---

## Vite Config Notes

- **Format:** IIFE (not ESM). The bundle assigns `window.Vue`, `window.bootstrap`, `window.axios` etc. and is loaded via `<script src>`. Any ESM change will break this.
- **Base:** `/bundles/` — font url() references inside `libs.css` are rewritten against this prefix.
- **`emptyOutDir: false`** — intentional; prevents deleting committed fonts and theme CSS on rebuild.
- **`cssCodeSplit: false`** — all CSS from npm dependencies lands in `libs.css`.
- **Vue alias:** resolves to `vue.esm-bundler.js` (includes template compiler) because templates are parsed from the DOM at runtime.

---

## Vue Source Structure

```
vueapp/src/
  main.js          entry point — registers views and behaviours
  views/           per-page Vue apps (19 files), mounted by Razor views
  behaviours/      DOM enhancement modules (22 files), no Vue dependency
  theme/           axios-cart.js, common.js, push-notifications.js
  compat/          Vue 2 → 3 migration compatibility layer
```

Views are mounted by Razor views; behaviours run on `DOMContentLoaded` without a Vue instance.

---

## Theme Script Files Outside This Bundle

Two files are **not** part of `vueapp` and must not be moved here:

| File | Owner |
|------|-------|
| `wwwroot/theme/script/app.js` | Default theme |
| `Plugins/Theme.Modern/Content/script/app.js` | Theme.Modern plugin |
| `wwwroot/js/public.checkout.js` | Checkout-only script |

---

## Admin Panel Project

```
src/Web/Grand.SharedUIResources/adminapp/
```

npm project for the Admin, Store and Vendor panels, following the `vueapp` conventions (IIFE output, `vue` aliased to `vue.esm-bundler.js`, `emptyOutDir: false`, eslint). See its `README.md`.

Current state:

- `npm run build` (`scripts/build.mjs`) runs one IIFE build per shipped entry, then writes the right-to-left stylesheet with rtlcss. Entries: `admin.bootstrap` (Bootstrap 5.3 with Popper 2, bootstrap-icons and the panel stylesheet), `admin.grid` (the `<admin-grid>` runtime on Tabulator, `window.GrandAdmin.grids`), `admin.ui` (the widgets that used to be Kendo UI - `window.GrandAdmin.modal`, `.tabs`, `.numeric`, `.dateInput`, `.select`, `.format`, on Tom Select) and `admin.core` (reserves `window.GrandAdmin`).
- `HeadAdmin.cshtml`, `HeadStore.cshtml` and `HeadVendor.cshtml` load `bundles/admin.bootstrap.css` (or `bundles/admin.bootstrap.rtl.css` for a right-to-left working language), then `bundles/admin.grid.js`, `bundles/admin.grid.css`, `bundles/admin.ui.js` and `bundles/admin.ui.css` after `admin.common.js`, `bundles/admin.bootstrap.js` in the footer, and render `<admin-culture/>` (the request culture as a JSON island the bundles read). **These files, and `bundles/fonts/` (the bootstrap-icons webfont), are committed** and follow the commit rules above: rebuild and stage them with every change under `adminapp/src`.
- **No panel loads Bootstrap 4, Font Awesome or simple-line-icons any more.** Bootstrap 5.3 and bootstrap-icons come through `adminapp`; `bootstrap/css/`, `bootstrap/js/` (except daterangepicker), `build/css/custom.css`, `build/css/custom-rtl.css`, `build/css/font-awesome*` and `simple-line-icons/` are gone from the repository.
- **The right-to-left stylesheet is generated, not written.** `scripts/build.mjs` runs rtlcss over `admin.bootstrap.css`; never edit `admin.bootstrap.rtl.css`. What must differ in right-to-left beyond mirrored geometry goes in `adminapp/src/styles/_rtl.scss`. Geometry on the inline axis is written with logical properties (`margin-inline-start`, `inset-inline-end`, `text-align: start`), which rtlcss leaves alone and the browser flips itself; `float` stays physical and rtlcss flips it.
- **No panel loads Kendo any more.** The `<script>` and `<link>` tags are gone from the three `Head*` partials. The vendored `wwwroot/administration/kendo/` tree (scripts, styles, cultures, messages) has been deleted from the repository; a plugin that needs Kendo UI must ship it itself.
- There is no compatibility layer for Kendo or Bootstrap 4 (`admin.legacy.js` was removed): no `$.fn.kendoGrid` or other Kendo widget, no `k-*` class mapping, no rename of `data-toggle` / `data-target` / `data-dismiss`. A plugin view uses `<admin-grid>`, `window.GrandAdmin` and `data-bs-*`.
- `admin.core.js` is not loaded by any panel: a plain `npm run build` does not write it (`npm run build -- admin.core` does, on request), and it is not committed until a `Head*` partial references it.
- Everything else in `wwwroot/administration/` is still the vendored libraries the panels load directly.
- `Grand.SharedUIResources.csproj` excludes `adminapp\**` from its default items, so nothing in it becomes content, a static web asset or output; it adds the folder back as `None` items (`CopyToOutputDirectory`/`CopyToPublishDirectory` `Never`, `Pack` false) so it shows in Solution Explorer, minus the git-ignored `node_modules`, `dist`, `reports` and e2e output folders. Keep that exclude list in step with `adminapp/.gitignore`.
- Playwright is not part of `azure-pipelines.yml`; the e2e specs run locally against a running instance.

Commands (run from `adminapp/`):

| Command | Does |
|---------|------|
| `npm ci` | one-time setup from the lock file, Node 20.19+ (browsers for e2e: `npx playwright install chromium`) |
| `npm run build` | Vite build to `wwwroot/administration/bundles/` |
| `npm run lint` | eslint over the whole project (`src`, `scripts`, `e2e`, configs) |
| `npm test` | Vitest, once |
| `npm run e2e` | Playwright smoke specs for the three panels (needs `GRAND_ADMIN_URL` and panel credentials, see README) |
| `npm run e2e:har` | records HAR files of representative grid pages into git-ignored `e2e/har/` |
| `npm run e2e:payloads` | records what the admin forms would post into git-ignored `e2e/payloads/` (`GRAND_PAYLOAD_DIR` picks the directory), for comparing two builds |
| `npm run audit:prod` | `npm audit` of the production dependencies |
| `npm run e2e:visual` | records a full-page screenshot per page of `e2e/support/visual-pages.js` into git-ignored `e2e/screenshots/` (`GRAND_SHOT_DIR` picks the directory), for comparing the look of two builds |

Never commit `reports/`, `e2e/har/`, `e2e/payloads/`, `e2e/screenshots/`, `e2e/test-results/` or `e2e/playwright-report/` — they hold session cookies, form payloads and store data.
