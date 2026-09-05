# Changelog

## GrandNode 2.4

_Changes since the 2.3.0 release (2025-03-16)._

### 🏬 Grand.Web.Store – new store management module (SaaS)

The biggest addition in this release: expansion of the dedicated `Grand.Web.Store` application, allowing a store owner/manager to manage their store without access to the full Admin panel (multi-tenant/SaaS):

- Per-store management of products, product attributes and specifications
- Product reviews, checkout attributes, blog, pages (CMS Pages/Topics), message templates
- Store settings, currencies, languages, shipping methods, discounts, contact attributes
- Tax, payment, and email accounts per store
- Customers and addresses per store, customer/address attributes, online customers panel
- New "Store manager" role/permissions (renamed from "Staff", with a database migration and default permissions)

### 🏗️ Architecture refactoring (ARCH-001, phases 1–19)

Consolidation of duplicated controller/service logic across Admin, Store, and Vendor into shared base classes (e.g. `BaseProductController`, `IAdminDataScope<TEntity>`) for: Product, MerchandiseReturn, Reports, VendorReview, attribute families, Brand, Discount, Blog, Page, News, GiftVoucher, ProductReview, MessageTemplate.

### 🔒 Security

- Fixed cross-store IDOR gaps in `Grand.Web.Store` (customer product price/personalization, and broader store-panel access)
- Fixed vendor product IDORs and consolidated ownership checks; added `ProductController` characterization tests and deduped Vendor/Store access checks onto `CanAccessProduct`
- Hashed customer passwords with PBKDF2, with transparent upgrade of legacy hashes; stopped storing passwords reversibly and made JWT secrets fail fast when weak (security audit items #2 & #3)
- Fixed missing CSRF/antiforgery protection across storefront controllers, admin plugin POST actions, and the admin file manager (also fixed picture URLs from subfolders while doing so)
- Replaced the NoScripts blacklist with allowlist-based HTML sanitization
- Fixed a memory DoS and file-extension bypass in attribute file uploads
- Added a regex match timeout to `ApiQueryOptions` and stopped passing raw API query options into the expression parser
- Fixed an open redirect vulnerability (code scanning alert: URL redirection from remote source)
- Updated MailKit to 4.15.1 to fix a MimeKit vulnerability (GHSA-g7hc-96xr-gvvx)
- Hardened OpenAPI metadata generation: preserved non-service parameters and hardened POST binding source handling
- Locked system-wide settings in the store panel and added an explicit "All stores" scope in admin
- Made the store scope explicit on category, brand and collection lists in the catalog
- Replaced `AuthorizeAttribute` with `ApiGroupAttribute` to determine API group membership in `IsApiFrontAuthenticated()`

### ⚡ Performance and reliability

- Cached the storefront catalog by customer group instead of by individual customer; category tree assembly moved outside the cache
- Executed paged queries and general repository queries directly on the MongoDB driver instead of blocking a thread
- Batched inventory stock writes into single repository updates
- Assigned the order number under a unique index instead of a read-max race condition
- Improved multi-instance safety: Redis cache invalidation resilience and exactly-once scheduled task execution
- Fixed the scheduled-task loop permanently stopping on reversible states
- Fixed blocking S3 calls, unawaited cache notifications, and a startup provider issue
- Shared a single `MongoClient` instance and fixed the LiteDB `TableCollection`
- Fixed the dead plugin version gate and stopped leaking host assemblies into plugin folders
- Fixed the database version stamp running before its own migrations
- Added a `/health/ready` application readiness check

### 🔧 Technology modernization

- Migrated to .NET 10; aligned Aspire on 13.4.6 and net10.0
- Migrated the storefront (`Grand.Web`) frontend to Vue 3 / Bootstrap 5 / Vite (dark mode, performance and checkout fixes)
- Replaced AutoMapper with an in-house `Grand.Mapping`
- Replaced MediatR with an in-house `Grand.Mediator`
- Migrated JsonPatch from Newtonsoft to System.Text.Json

### 🐞 Bug fixes (selected)

- Fixed store logo not appearing in PDF invoices when using an absolute store URL
- Fixed CSP `font-src` to allow `data:` URIs (swiper.js base64 font)
- Fixed the store filter override in the admin "add product" popup
- Fixed missing localization for `Admin.Catalog.Products.Pictures.Fields.IsDefault`
- Fixed storefront rendering with no CSS/JS (Razor tag helper regression)
- Fixed a partial-view name collision in Order `AddressEdit` (admin)
- Removed a spurious null-guard on `Category.PageSizeOptions` (storefront)
- Fixed DI resolution error in `ProductEmailAFriendValidator`
- Dropped a dead-condition re-check in `EditWarningCheck` across Product/Collection/Brand/Category/Blog (store)
- Removed unreachable `IsStoreManager` branches from Admin controllers
- Scoped CMS lookups to the store and let a store override a shared page
- Took the admin theme switch out of the store panel
- Renamed Mongo-specific repository methods to storage-neutral names and reduced overloads
- Unified wording of standard permission names; added missing default permissions for the "Store manager" role on install
- Added a migration to rename the "Staff" customer group to "Store manager"
- Fixed `grand.plugin.props`
- Removed the unused WKHTMLTOPDF Linux note from the PDF settings view

### 📦 Dependencies and CI/CD

- Numerous NuGet package updates across the solution
- Numerous npm/vueapp dependency bumps, including axios (1.7.4 → 1.18.0 across several bumps), lodash, js-yaml, form-data, node-forge, follow-redirects, svgo, minimatch, ajv, picomatch, flatted, shell-quote, launch-editor, fast-uri, ws, websocket-driver, postcss, serialize-javascript, terser-webpack-plugin, css-minimizer-webpack-plugin, and `@babel/plugin-transform-modules-systemjs`
- Updated frontend dependencies, regenerated bundles, added cache-busting, and pinned CI to ubuntu-22.04 with published build artifacts
- Cleaned up redundant ImageSharp references and documented the elFinder HTTP pin
- Removed the unused `Grand.Targets.props` file
- Added/updated GitHub Actions workflows: SonarCloud/SonarQube analysis, Docker Image CI (main and develop branches), Copilot setup steps, running the whole test suite on pull requests, and running the deploy pipeline tests in the configuration it actually builds
