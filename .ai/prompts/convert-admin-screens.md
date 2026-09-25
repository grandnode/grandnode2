# Prompt: Convert Admin Screens

## Purpose
Move one module of the administration - its list, create/edit screens, tabs, popups and pickers - onto the panel's component layer, in all three panels: Main Admin, Store Owner and Vendor. One module per run: the whole panel is some 500 legacy views, far more than one run can convert well.

## Inputs Required
- `MODULE`: the module to convert, e.g. `Catalog / Category`, `Sales / Order`, `Configuration / Shipping`. Take it from the request; if none is given, pick the next unconverted module from the order below and say which.
- Repository root, and a running app to verify in (see step 8).

## Module Order
Most used first, the large settings batches last. Skip what is already converted (the Product module is the reference and is done, except `CreateOrUpdate.Calendar.cshtml`).

1. Catalog: Category, Brand, Collection, ProductAttribute, SpecificationAttribute (two or three per run)
2. Sales: Order, Shipment, MerchandiseReturn
3. Customers: Customer, CustomerAttribute, CustomerGroup, Vendor
4. Promotions: Discount, GiftVoucher
5. Content: Blog, Knowledgebase, Course, Document, Page
6. Configuration: Shipping, Country, AddressAttribute, ContactAttribute, CheckoutAttribute
7. Settings (about 38 views - split into two or three runs)
8. Reports and whatever is left

To see what is left: `grep -rlE 'x_panel|form-horizontal|mt-checkbox|col-md-9 col-sm-9|btn (green|red|purple|default)' src/Web/Grand.Web.{Admin,AdminShared,Store,Vendor} --include=*.cshtml`.

## Steps

1. Read, in this order, and follow them - they are binding:
   1. `.ai/skills/admin-ui-component-layer.md` - the style, the tag helpers and what each replaces, the mandatory rules, the CSS traps, the per-screen procedure, the checklist.
   2. `.ai/skills/admin-area-changes.md` - which panel owns a file and what Store and Vendor get.
   3. The reference screens the skill names: `Product/List.cshtml`, `Product/Edit.cshtml`, the `Product/Partials/CreateOrUpdate.*.cshtml` tabs, `TierPriceCreatePopup.cshtml`.
2. Find every view of `MODULE`: its controller(s) in `Grand.Web.Admin`, `Grand.Web.Store` and `Grand.Web.Vendor`, the views they return (`Areas/*/Views/<Module>/`, `Grand.Web.AdminShared/Views/AdminShared/<Module>/`), and the partials and popups those views open. List each with its owning project and whether it still uses the old chrome.
3. Convert in this order: the list screen, then create/edit, then its tabs, then its popups and pickers.
4. A view shared through AdminShared is converted once and reaches all three panels. A view Store or Vendor keep as their own copy is converted in each copy, keeping that panel's resource prefix (`Vendor.*`), its area constants, and any column or field it deliberately drops.
5. For each view, list the ids it and its scripts use - and what opens or refreshes it - before moving any markup; check them again after.
6. When a view needs something the tag helpers or components do not do, fix the foundation (`Grand.Web.Common/TagHelpers/Admin/`, `adminapp/src/styles/_components.scss`, `ui/ui.css`, `grid/grid.js`), add a test, and add the new block or trap to `.ai/skills/admin-ui-component-layer.md`.
7. Build: `npm run build` in `src/Web/Grand.SharedUIResources/adminapp` when CSS or JS changed (restore bundles whose diff is line endings only), `dotnet build src/Web/Grand.Web/Grand.Web.csproj` for any view in AdminShared, Store or Vendor - those views compile into their DLLs.
8. Verify in the running app: Grand.Web under Kestrel on `https://localhost:44350` from this worktree (`ASPNETCORE_ENVIRONMENT=Development dotnet bin/Debug/net10.0/Grand.Web.dll --urls https://localhost:44350` in `src/Web/Grand.Web`), restarted after every build - static assets are fingerprinted at startup. Open every converted screen, tab and popup: the controls are right, script-toggled panels keep their layout after hide/show, the console is clean.
9. Run `npx vitest run` in adminapp and `dotnet test src/Tests/Grand.Web.Common.Tests` when shared code changed.
10. Stop when the batch is too big to do well. Finish whole screens; never leave a view half converted.

## Mandatory Rules

1. Never change an `id`, a `name`, a `data-val-*` attribute, a widget zone or a tab index.
2. One `btn-primary` per screen or popup; destructive actions `btn-outline-danger`, on a detail screen inside `<admin-action-menu>`.
3. A grid's Add new goes in its card header (`<card-actions>`), never under the grid.
4. New resources go in both `DefaultLanguage.xml` (UTF-16) and `Upgrade/en_240.xml`; prefer existing keys and leave a card untitled rather than invent a heading key.
5. Do not delete legacy CSS - plugin views still use it.
6. Never type a password. Store and Vendor need their own login, so their screens stay unverified in the browser: say so.
7. Never save, delete or upload data while verifying; opening and closing a form is enough.
8. Screenshots through the browser extension time out in this environment: verify with DOM and `getComputedStyle` measurements and say plainly that you could not see the screens.
9. Do not commit. The person reviews the batch in the app first.

## Output Format

- The module, and the list of its views with owning project (step 2).
- Per converted view: file, owning project, before/after line count, how it was opened, what was verified.
- Foundation changes, with their tests, and what was added to the skill.
- What could not be verified, and why.
- What is left of the module, and the next module to run.
