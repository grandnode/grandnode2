# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

GrandNode2 is an open-source, cross-platform e-commerce platform built on ASP.NET Core 9.0 and MongoDB. It supports multiple business models: B2B, B2C, Multi-Store, Multi-Vendor, Multi-Tenant, Multi-Language, and Multi-Currency.

**Key Technologies:**
- ASP.NET Core 9.0
- MongoDB 4.0+ (primary database) / LiteDB (embedded alternative)
- CQRS pattern (using MediatR) for business logic
- AutoMapper for entity/DTO mapping
- FluentValidation for input validation
- Azure Aspire for distributed application orchestration
- Docker for containerization

## Build and Development Commands

### Prerequisites

- .NET 9.0 SDK (specified in global.json)
- MongoDB 4.0+ running locally or via Docker
- Visual Studio 2022 (v17.12.0+) or VS Code

### Common Commands

**Restore dependencies:**
```bash
dotnet restore GrandNode.sln
```

**Build the entire solution:**
```bash
dotnet build GrandNode.sln --configuration Release
```

**Build a specific project:**
```bash
dotnet build src/Web/Grand.Web/Grand.Web.csproj
```

**Run the main web application:**
```bash
dotnet run --project src/Web/Grand.Web/Grand.Web.csproj
```

**Run the admin panel:**
```bash
dotnet run --project src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj
```

**Run the vendor portal:**
```bash
dotnet run --project src/Web/Grand.Web.Vendor/Grand.Web.Vendor.csproj
```

### Database Setup

**Start MongoDB in Docker:**
```bash
docker run -d -p 27017:27017 --name mongodb mongo
```

**Using Azure Aspire for orchestrated local development:**
```bash
dotnet run --project src/Aspire/Aspire.AppHost/Aspire.AppHost.csproj
```

This orchestrates MongoDB and all three web projects. Access the Aspire Dashboard at http://localhost:18888.

### Testing

**Run all tests:**
```bash
dotnet test GrandNode.sln --configuration Release
```

**Run tests for a specific module:**
```bash
dotnet test src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj
```

**Run a specific test:**
```bash
dotnet test src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj --filter "FullyQualifiedName~TestClassName"
```

**Run tests with code coverage:**
```bash
dotnet test GrandNode.sln --configuration Release --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

### Plugins and Modules Build

**Build all plugins:**
```bash
for plugin in src/Plugins/*; do dotnet build "$plugin" -c Release; done
```

**Build all modules:**
```bash
for module in src/Modules/*; do dotnet build "$module" -c Release; done
```

**Docker image:**
```bash
docker build -t grandnode/grandnode2:latest .
```

## Solution Structure

### Organizational Layers

**Core (src/Core/)** - Foundation layers
- Grand.Domain - Domain models and entities (BSON-serializable classes)
- Grand.SharedKernel - Shared utilities, interfaces, and base classes
- Grand.Data - Data access layer with repository pattern (MongoDB and LiteDB implementations)
- Grand.Infrastructure - DI configuration, startup hooks, type searching, plugin/module loading, validators, type converters, AutoMapper setup

**Business (src/Business/)** - Core business logic using CQRS
- Grand.Business.Core - Base interfaces, DTOs, and common utilities
- Grand.Business.Authentication - Authentication handlers and JWT
- Grand.Business.Catalog - Product, category, and inventory management
- Grand.Business.Checkout - Shopping cart, order processing, and checkout flow
- Grand.Business.Customers - Customer profiles, addresses, and segmentation
- Grand.Business.Marketing - Promotions, discounts, and marketing features
- Grand.Business.Cms - Content management (pages, blog, etc.)
- Grand.Business.Messages - Email notifications and messaging
- Grand.Business.Storage - File uploads and media management
- Grand.Business.Common - Cross-cutting concerns (system settings, ACL, etc.)

**Web (src/Web/)** - Presentation layer
- Grand.Web - Customer-facing storefront
- Grand.Web.Admin - Admin panel
- Grand.Web.Vendor - Vendor/seller portal
- Grand.Web.Common - Shared web infrastructure (controllers, components, filters, middleware, routing)
- Grand.SharedUIResources - Shared UI resources (CSS, JavaScript, images)

**Plugins (src/Plugins/)** - Extensible integrations
- Authentication plugins (Facebook, Google)
- Payment providers (Stripe, BrainTree, Cash on Delivery)
- Shipping calculators (Fixed Rate, By Weight, Shipping Points)
- Tax calculators (Fixed Rate, Country/State/Zip)
- Discount rules and exchange rate providers
- UI widgets (Google Analytics, Facebook Pixel, Slider)
- Theme.Modern - Default responsive theme

**Modules (src/Modules/)** - Core modular features
- Grand.Module.Installer - Initial setup and installation wizard
- Grand.Module.Migration - Database migrations and upgrades
- Grand.Module.Api - REST API with Swagger/OpenAPI support
- Grand.Module.ScheduledTasks - Background job scheduling and execution

**Tests (src/Tests/)** - Comprehensive unit test suite
- One test project per business/core module
- Tests are xUnit-based with Moq for mocking

**Aspire (src/Aspire/)** - Distributed application orchestration
- Aspire.AppHost - Orchestrates MongoDB and web services for local development
- Aspire.ServiceDefaults - Observability, health checks, and service defaults

## Architecture Patterns

### CQRS (Command Query Responsibility Segregation)

Business logic uses CQRS via MediatR:

**Commands** - Write operations that modify state
- Located in {Module}/Commands/ folders
- Implement IRequestHandler<TCommand, TResult>
- Example: src/Business/Grand.Business.Catalog/Commands/UpdateProductCommandHandler.cs

**Queries** - Read operations that retrieve data
- Located in {Module}/Queries/ folders with nested Handlers/ subfolders
- Implement IRequestHandler<TQuery, TResult>
- Example: src/Business/Grand.Business.Catalog/Queries/Handlers/GetProductsQueryHandler.cs

**Sending Commands/Queries:**
```csharp
var result = await _mediator.Send(new UpdateProductCommand { });
var products = await _mediator.Send(new GetProductsQuery { });
```

### Data Access

**Repository Pattern** with abstraction over MongoDB and LiteDB:
- IRepository<T> - Generic async repository interface
- MongoRepository<T> - MongoDB implementation
- LiteDBRepository<T> - LiteDB implementation
- Both are registered in DI based on configuration

**Database Context:**
- IDatabaseContext - Abstracts database operations
- MongoDBContext - MongoDB provider
- LiteDBContext - LiteDB provider
- File storage via IStoreFilesContext (GridFS for MongoDB)

### Dependency Injection & Startup

**Startup flow:**
1. Program.cs creates WebApplicationBuilder
2. Loads appsettings via AddAppSettingsJsonFile()
3. Calls StartupBase.ConfigureServices() from Grand.Infrastructure
4. StartupBase discovers and registers startup classes implementing IStartupApplication
5. Each business module has a Startup/ folder with startup classes
6. Request pipeline is configured via StartupBase.ConfigureRequestPipeline()

**Key registrations in StartupBase:**
- Database context and repositories (MongoDB or LiteDB)
- MediatR for CQRS
- AutoMapper
- FluentValidation validators
- Custom type converters
- Plugins and modules discovery and loading

### AutoMapper

Mapping profiles are registered automatically via type discovery:
- Classes implementing IAutoMapperProfile are found via ITypeSearcher
- Profiles are sorted by Order property and applied to MapperConfiguration
- Used for entity-to-DTO conversions in API and business logic

### Validation

**FluentValidation** is integrated throughout:
- Validator classes implement AbstractValidator<T>
- Validators are auto-discovered and registered
- Can be used in commands/queries for input validation
- Used in API controllers and web forms

### Plugin & Module System

**Plugin loading:**
- Plugins are built as separate class libraries in src/Plugins/
- Each plugin has a Plugin.json manifest
- Plugins are loaded dynamically at startup
- Can override and extend core functionality
- Output is placed in src/Web/Grand.Web/Plugins/ during build

**Module loading:**
- Modules are built as separate class libraries in src/Modules/
- Output is placed in src/Web/Grand.Web/Modules/ during build
- Auto-discovered and registered at startup

## Configuration

### appsettings.json

Located in src/Web/Grand.Web/App_Data/:
- Database connection settings (ConnectionString, provider type)
- Feature flags and feature management
- Store settings, SEO settings, security policies
- Cache configuration (Redis, Memory)
- Email/SMTP settings
- External service integrations

### Database Configuration

**MongoDB (default):**
```json
{
  "ConnectionString": "mongodb://localhost:27017/grandnode",
  "ConnectionStringProvider": "0"
}
```

**LiteDB:**
```json
{
  "Database": {
    "UseLiteDb": true,
    "LiteDbConnectionString": "filename=grandnode.db;upgrade=true"
  }
}
```

## Important Files & Patterns

### Key Infrastructure Files

- src/Core/Grand.Infrastructure/StartupBase.cs - Core initialization orchestrator
- src/Core/Grand.Infrastructure/Startup/StartupApplication.cs - Database registration
- src/Web/Grand.Web.Common/Extensions/ServiceCollectionExtensions.cs - Web-specific DI setup
- src/Web/Grand.Web/Program.cs - Main application entry point
- Directory.Packages.props - Centralized NuGet package version management

### Shared Abstractions

- Grand.SharedKernel.BaseModel - Base class for domain models
- Grand.SharedKernel.BaseCommand - Base class for commands
- Grand.Data.IRepository<T> - Generic repository interface
- Grand.Infrastructure.IStartupApplication - Plugin/module startup interface
- Grand.Infrastructure.Mapper.IAutoMapperProfile - AutoMapper profile interface

### Web Controller Base

- Web controllers inherit from BaseController in Grand.Web.Common
- Admin controllers inherit from BaseAdminController
- Both provide common functionality like localization, store context, customer context

## Testing Patterns

**Unit tests use:**
- MSTest framework with TestContext
- Moq for mocking dependencies
- NUnit for some test projects
- Mock repositories and services in test setup

**Test Project Structure:**
```
Grand.Business.Catalog.Tests/
├── Commands/
│   └── UpdateProductCommandHandlerTests.cs
├── Queries/
│   └── GetProductsQueryHandlerTests.cs
└── Services/
    └── CategoryServiceTests.cs
```

**Running CI/CD:**
- GitHub Actions workflow (.github/workflows/aspnetcore.yml) runs on every push
- Azure Pipelines (.azure-pipelines.yml) runs tests and publishes artifacts
- CodeQL security scanning enabled
- SonarCloud integration for code quality

## Common Development Scenarios

### Adding a New Feature

1. Define the domain model in Grand.Domain with BSON serialization
2. Create repository queries/commands in the relevant Business module:
   - Add YourFeatureQuery.cs and Handlers/YourFeatureQueryHandler.cs in Queries/
   - Add YourFeatureCommand.cs and YourFeatureCommandHandler.cs in Commands/
3. Register any new services in a Startup/YourFeatureStartup.cs implementing IStartupApplication
4. Create API endpoints in Grand.Module.Api or web controllers
5. Add validation via FluentValidation validators
6. Create mapping profiles in Grand.Infrastructure/Mapper/ if needed
7. Write unit tests in the corresponding Tests project

### Adding a Payment Gateway

1. Create new plugin in src/Plugins/Payments.{ProviderName}/
2. Implement payment handler interface
3. Add configuration UI in admin panel via views
4. Extend Grand.Module.Api for API integration if needed
5. Register in plugin's startup class

### Creating Custom Validators

1. Create Validators/ folder in your module
2. Extend AbstractValidator<T> from FluentValidation
3. Use auto-discovery - validator will be found automatically
4. Use in commands: public YourCommandValidator() { RuleFor(...) }

## First Run / Installation

1. Set the MongoDB connection string in `src/Web/Grand.Web/App_Data/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "Mongodb": "mongodb://user:password@localhost:PORT/grandnode?authSource=admin"
   }
   ```

2. Run the app and open `http://localhost:5000` — the installation wizard will appear.

3. Fill in company name, admin email/password. The database field will use the connection string already set in appsettings.

4. After the wizard completes, disable the installer in `appsettings.json`:
   ```json
   "FeatureManagement": {
     "Grand.Module.Installer": false
   }
   ```

5. Restart the app. The storefront is at `/` and the admin panel at `/admin`.

## Debugging Tips

- Use Azure Aspire Dashboard (http://localhost:18888) to see all service logs
- MongoDB can be inspected with MongoDB Compass or Studio 3T
- Enable detailed logging in appsettings by setting log levels
- Breakpoints in handlers will pause execution at CQRS boundaries
- Check App_Data/Logs/ for application logs if configured

## Version and Release Info

- Current Version: 2.3.0 (see src/Build/Grand.Common.props)
- Target Framework: .NET 9.0
- License: GNU General Public License v3.0
- Repository: https://github.com/grandnode/grandnode2

## Related Documentation

- Official docs: https://docs.grandnode.com/
- Online demo: https://demo.grandnode.com/
- Admin demo: https://demo.grandnode.com/admin (admin@yourstore.com / 123456)
- Contributing guide: See CONTRIBUTING.md
