# Development

How to build GrandNode from source, rebuild the frontend bundles and run the application locally.
For the tools and versions you need, see [Prerequisites](../README.md#prerequisites).

## Building the backend

```bash
dotnet restore GrandNode.slnx
dotnet build GrandNode.slnx
```

`GrandNode.slnx` contains the whole application: the core libraries, the web
project, the modules under `src/Modules` (installer, migrations, REST API,
scheduled tasks) and the plugins under `src/Plugins`. Building the solution
builds them all and copies each module and plugin into
`src/Web/Grand.Web/Modules` / `Plugins`, so a plain `dotnet build` is enough.

Two things worth knowing:

* **Plugins that ship views compile those views into the plugin DLL.** After
  editing a `.cshtml` file in, for example, `src/Plugins/Theme.Modern`, rebuild
  that plugin (`dotnet build src/Plugins/Theme.Modern`) - the running site will
  not pick the change up otherwise. Razor runtime compilation covers only
  `Grand.Web`'s own views. Stop the site before rebuilding a plugin, or the
  build fails on a locked DLL.
* Building a plugin on its own is fine and is what the Docker image does; you
  only need the full solution build after changing shared code.

## Building the frontend

The repository has two npm projects: the storefront (below) and the Admin, Store
and Vendor panels (at the end of this section).

The storefront UI (Vue 3, Bootstrap 5) lives in `src/Web/Grand.Web/vueapp`:

```bash
cd src/Web/Grand.Web/vueapp
npm install
npm run build
```

That writes into `src/Web/Grand.Web/wwwroot/bundles`:

| output | contents |
| --- | --- |
| `app.runtime.bundle.js` | Vue 3, the compatibility layer, the per-page view-models and the shared DOM behaviours |
| `libs.css` | Bootstrap, Bootstrap Icons, animate.css, Pikaday |
| `style.min.css`, `style.rtl.min.css` | the theme stylesheets from `wwwroot/theme/css`, concatenated in cascade order and minified |

**This output is committed to the repository**, which is why neither the CI
workflows nor the Dockerfile install Node - they build the .NET solution against
the bundles already in the tree. The flip side is that when you change anything
under `vueapp/src` or `wwwroot/theme/css` you have to run `npm run build` and
commit the regenerated bundles together with the source change, otherwise your
change simply will not be on the page.

Other scripts: `npm run dev` (watch build), `npm run lint` (ESLint over
`vueapp/src`), `npm run audit:prod`. More detail in
[`vueapp/README.md`](../src/Web/Grand.Web/vueapp/README.md).

The Admin, Store and Vendor panels (Bootstrap 5, the Tabulator-based
`<admin-grid>`, vanilla JS widgets) are built by
`src/Web/Grand.SharedUIResources/adminapp`:

```bash
cd src/Web/Grand.SharedUIResources/adminapp
npm ci
npm run lint && npm test && npm run build
```

That writes `admin.bootstrap.js/.css/.rtl.css`, `admin.grid.js/.css`,
and `admin.ui.js/.css` into
`src/Web/Grand.SharedUIResources/wwwroot/administration/bundles`. The same rule
applies: the bundles are committed, so rebuild and commit them with every change
under `adminapp/src`. The panels no longer use Kendo UI. Scripts, styles, the
grid markup, the plugin compatibility shim and the e2e smoke tests are described in
[`adminapp/README.md`](../src/Web/Grand.SharedUIResources/adminapp/README.md).

## Running locally

```bash
dotnet run --project src/Web/Grand.Web
```

`Grand.Web` does not reference the plugins - they install themselves into its
output directory when *they* are built. So build the solution once
(`dotnet build GrandNode.slnx`) before the first run; after that you can start the
web project alone.

The Kestrel profile listens on <https://localhost:5001> and
<http://localhost:5000>; the Visual Studio IIS Express profile uses
<https://localhost:44350>.

> **Set `ASPNETCORE_ENVIRONMENT=Development`.** Both launch profiles already do.
> If you start the application without it, the static web assets manifest is not
> consulted, every file served from `_content/...` returns 404 and the admin
> panel loads with no CSS and no JavaScript at all. It looks like a broken
> install; it is only the missing environment variable.

On the first run the application redirects to `/install`, where you enter the
MongoDB connection string (for example `mongodb://localhost/grandnode`) and the
administrator account, and choose whether to load the sample data. The installer
writes the connection string to `src/Web/Grand.Web/App_Data/Settings.cfg`, which
is not tracked by git - delete that file to run the installer again against a
fresh database.

