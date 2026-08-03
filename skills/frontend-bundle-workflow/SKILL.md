# Skill: frontend-bundle-workflow

## Purpose

Guide agents making frontend changes in GrandNode's storefront: when to run the build, what gets rebuilt, and that the output must be committed alongside source.

---

## Project Location

Single npm project at:

```
src/Web/Grand.Web/vueapp/
```

No other npm project in the solution (the old webpack project at `Grand.Web/` root was removed).

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
