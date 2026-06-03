# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build
```bash
dotnet restore GrandNode.sln
dotnet build GrandNode.sln --configuration Release
```

### Run (local development)
```bash
# Start MongoDB first
docker run -d -p 127.0.0.1:27017:27017 --name mongodb mongo

# Run the web application
dotnet run --project src/Web/Grand.Web
```

### Frontend assets
```bash
cd src/Web/Grand.Web
npm install
npm run build   # runs webpack
```

### Tests
```bash
# Run all tests
dotnet test GrandNode.sln

# Run a specific test project
dotnet test src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj

# Run with code coverage
dotnet test --configuration Release --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

Test framework is MSTest with Moq for mocking. Test projects mirror business project names with a `.Tests` suffix under `src/Tests/`.

## Architecture

GrandNode2 is an ASP.NET Core 10.0 e-commerce platform backed by MongoDB. The solution is organized into five layers:

### Layer dependency order (bottom → top)
1. **Core** (`src/Core/`) — foundation with no business dependencies
   - `Grand.Domain` — entity models
   - `Grand.Data` — MongoDB repository abstractions and implementations
   - `Grand.Infrastructure` — DI registration, configuration
   - `Grand.SharedKernel` — base classes, enums, shared utilities

2. **Business** (`src/Business/`) — domain logic split by bounded context
   - One project per domain: `Catalog`, `Checkout`, `Customers`, `Marketing`, `Messages`, `Cms`, `Authentication`, `Authorization`, `Storage`, `Common`
   - `Grand.Business.Core` holds interfaces and base classes shared across business projects

3. **Modules** (`src/Modules/`) — feature modules composed at the web layer
   - `Grand.Module.Api` — REST API endpoints with OpenAPI/Scalar
   - `Grand.Module.Installer` — first-run installation
   - `Grand.Module.Migration` — MongoDB schema migrations
   - `Grand.Module.ScheduledTasks` — background jobs

4. **Web** (`src/Web/`) — presentation layer
   - `Grand.Web` — entry point, routing, startup
   - `Grand.Web.Store` — customer-facing storefront controllers (SaaS per-store)
   - `Grand.Web.Admin` — admin panel (MVC/Razor)
   - `Grand.Web.Vendor` — vendor portal
   - `Grand.Web.Common` — shared web utilities, filters, extensions

5. **Plugins** (`src/Plugins/`) — optional, independently deployable extensions
   - Payment, shipping, tax, authentication, discount, widget, and theme plugins
   - At build time, plugins copy their output into the web project's `Plugins/` folder

### Key patterns
- **CQRS via MediatR** — commands and queries dispatched through `IMediator`; handlers live in Business projects
- **Repository pattern** — `IRepository<T>` abstraction over MongoDB; `Grand.Data` provides the implementation
- **Scrutor auto-registration** — services are discovered and registered by convention; explicit registration is the exception
- **Multi-store / multi-tenant** — store context (`IStoreContext`) is threaded through service calls; many controllers accept a `StoreId` parameter for SaaS scenarios
- **Plugin/Module discovery** — assemblies in `Plugins/` and `Modules/` directories are loaded at startup and participate in DI

### Frontend
Vue.js components live in `src/Web/Grand.Web/vueapp/`. Webpack bundles them; the compiled output lands in `wwwroot/`.

### Infrastructure
- Central package versions: `Directory.Packages.props` (no version attributes in individual `.csproj` files)
- Shared build properties: `src/Build/Grand.Common.props` (target framework `net10.0`, current version `2.4.0`)
- .NET Aspire orchestration for local dev: `src/Aspire/Aspire.AppHost`
- Docker: multi-stage `Dockerfile` at the repository root; `docker-compose` not present — MongoDB must be started separately
