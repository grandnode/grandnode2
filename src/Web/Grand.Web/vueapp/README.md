# Storefront frontend

The only npm project in the solution. It builds everything the storefront loads
from `wwwroot/bundles`:

| output | contents |
| --- | --- |
| `app.runtime.bundle.js` | Vue 3 + the compat layer, the per-page view-models (`src/views`), the DOM behaviours (`src/behaviours`) and the shared theme scripts (`src/theme`) |
| `libs.css` | Bootstrap, Bootstrap Icons, animate.css, Pikaday |
| `style.min.css`, `style.rtl.min.css` | the six theme stylesheets from `wwwroot/theme/css`, concatenated in cascade order and minified (`scripts/build-theme-css.mjs`) |

`Grand.Web` used to carry a second npm project at its root whose only job was
building the two theme stylesheets with webpack. `npm run build` here produces
them, so that project was removed.

## Setup

```
npm install
```

## Build

```
npm run build
```

Run this after changing anything under `src/` or `wwwroot/theme/css/`. The output
is committed, so include it in the same commit as the source change.

## Develop

```
npm run dev
```

## Lint

```
npm run lint
```

Everything under `src/` is linted, including the former theme scripts. Bundled
code runs in strict mode, so mistakes that sloppy mode used to swallow - an
implicit global, `this` in a plain callback - now surface here or throw at
runtime.

## Audit production dependencies

```
npm run audit:prod
```

## Notes

- The bundle is a single IIFE loaded by a plain `<script src>` and assigns
  `window.Vue`, `window.bootstrap`, `window.axios` and friends, so it must not be
  emitted as an ES module. See the comments in `vite.config.js`.
- `emptyOutDir` is off on purpose: the committed fonts live in the same output
  directory and a clean build would delete them.
- Each theme still ships its own root application (`wwwroot/theme/script/app.js`
  and Theme.Modern's `Content/script/app.js`), and `public.checkout.js` is loaded
  only on checkout. Those are the last two files outside this bundle.
