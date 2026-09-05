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

- Fixed multiple IDOR vulnerabilities (cross-store/cross-vendor access to products, prices, personalization)
- PBKDF2 password hashing with transparent upgrade of legacy hashes; stopped storing passwords reversibly
- JWT secret strength validation
- CSRF/antiforgery protection added across storefront controllers, admin panel, and file manager
- Replaced the NoScripts blacklist with allowlist HTML sanitization
- Fixed a file-upload memory DoS (memory limits, extension validation)
- Added regex match timeout, fixed an open redirect vulnerability

### ⚡ Performance and reliability

- Caching the storefront catalog by customer group instead of by individual customer; category tree assembly moved outside the cache
- Paged queries/repository queries executed directly on the driver instead of blocking a thread
- Batched inventory stock writes; unique index on order number (eliminating a race condition)
- Improved Redis cache resilience across multiple instances and exactly-once scheduled tasks
- Fixed blocking S3 calls, shared a single MongoClient, fixed LiteDB
- Added a `/health/ready` readiness check endpoint

### 🔧 Technology modernization

- Migrated to .NET 10
- Migrated the storefront (`Grand.Web`) frontend to Vue 3 / Bootstrap 5 / Vite (dark mode, performance and checkout fixes)
- Replaced AutoMapper with an in-house `Grand.Mapping`
- Replaced MediatR with an in-house `Grand.Mediator`
- Migrated JsonPatch from Newtonsoft to System.Text.Json

### 🐞 Bug fixes (selected)

- Localization fixes, PDF fixes (store logo, HTML/PDF generation), CSP fix for Swiper fonts
- Fixed store filter override in the "add product" popup, a partial-view name collision, storefront rendering with no CSS/JS
- Fixed the scheduled-task loop and database version stamping during migrations

### 📦 Dependencies and CI/CD

- Numerous NuGet and npm package updates (axios, lodash, js-yaml, and others, including vulnerability fixes such as MailKit/MimeKit GHSA-g7hc-96xr-gvvx)
- New/updated GitHub Actions workflows (SonarCloud, Docker Image CI, running the full test suite on PRs)
