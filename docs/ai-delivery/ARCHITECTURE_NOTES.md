# Architecture Notes

## Purpose

This document provides lightweight architecture guidance for AI-assisted development in the GrandNode2 repository.

The goal is not to fully document the entire legacy application, but to give AI agents enough context and constraints to avoid unsafe or unrelated changes.

## Architecture Style

GrandNode2 is a monolithic e-commerce application with modular and plugin-based areas.

The application uses layered architecture concepts:

- Web/UI layer
- Domain layer
- Business/service layer
- Infrastructure layer
- Plugins

AI agents must inspect the actual repository structure before making changes.

## Main Areas

### Web Layer

Expected location:

    src/Web

Responsibilities:

- Controllers
- Razor views
- View models
- UI composition
- Request handling
- Page rendering

Guidance:

- Keep UI logic in views or view models.
- Avoid placing business rules directly in Razor views.
- Follow existing Razor and view model patterns.

### Domain Layer

Expected location:

    src/Grand.Domain

Responsibilities:

- Domain entities
- Core business models
- Shared domain concepts

Guidance:

- Avoid changing domain entities unless required.
- Do not introduce new domain fields without checking persistence, mapping, and existing usage.
- Prefer using existing entity properties where possible.

### Business Layer

Expected location:

    src/Grand.Business

Responsibilities:

- Business services
- Catalog logic
- Customer logic
- Order logic
- Application-level operations

Guidance:

- Follow existing service patterns.
- Avoid duplicating business logic.
- Prefer extending existing services or model factories when appropriate.

### Infrastructure Layer

Expected location:

    src/Grand.Infrastructure

Responsibilities:

- Shared infrastructure
- Cross-cutting utilities
- Framework-level integrations

Guidance:

- Avoid changing infrastructure code for feature-specific work.
- Only modify infrastructure when the feature explicitly requires it.

### Plugins

Expected location:

    src/Plugins

Responsibilities:

- Optional or modular functionality
- Theme-specific behavior
- Extension points

Guidance:

- Check whether a feature belongs in core Web code, a theme, or a plugin.
- Avoid changing multiple plugins unless the feature requires it.

## CQRS / MediatR Handler Pattern

The Web layer uses a CQRS-style handler pattern built on MediatR. Controllers do not populate view models directly — they dispatch requests through `IMediator`, and dedicated handler classes build and return the view model.

Handler location:

    src/Web/Grand.Web/Features/Handlers/

Each handler implements `IRequestHandler<TRequest, TResponse>`. Request objects (the queries) live alongside the handlers in:

    src/Web/Grand.Web/Features/Models/

For the product details page, the relevant handler is:

    src/Web/Grand.Web/Features/Handlers/Products/GetProductDetailsPageHandler.cs

This handler receives a `GetProductDetailsPage` request and returns a fully populated `ProductDetailsModel`. It is the correct place to set computed display properties for the product details page.

**When adding a display property to the product details page:**

1. Add the property to `ProductDetailsModel` (`src/Web/Grand.Web/Models/Catalog/ProductDetailsModel.cs`).
2. Populate it inside `GetProductDetailsPageHandler`.
3. Render it in the Razor view.

Do not add product-display logic to `ProductController` directly or to business service classes.

## Product Details Data Flow

The confirmed request flow for a product details page:

    HTTP request
    → ProductController  (src/Web/Grand.Web/Controllers/ProductController.cs)
    → _mediator.Send(new GetProductDetailsPage { ... })
    → GetProductDetailsPageHandler  (src/Web/Grand.Web/Features/Handlers/Products/GetProductDetailsPageHandler.cs)
    → ProductDetailsModel  (src/Web/Grand.Web/Models/Catalog/ProductDetailsModel.cs)
    → ProductLayout.Simple.cshtml  (src/Web/Grand.Web/Views/Product/ProductLayout.Simple.cshtml)

The controller passes the resolved `Product` entity and store context into the request object. The handler performs all mapping, pricing, and enrichment. The resulting model is passed directly to the Razor view.

## Cache Impact

`GetProductDetailsPageHandler` uses `ICacheBase` to cache sub-components of the product details model (for example, product collections). Computed display properties added directly to `ProductDetailsModel` in the handler's main mapping block are **not** independently cached — they are evaluated on each request.

However, if the handler caches the full model or a sub-section that includes the new property, stale values may be served until the cache expires or is cleared.

Guidance:

- Do not change cache keys or invalidation logic unless the feature implementation proves it is necessary.
- If a property is not appearing during local testing, restart the application or clear the cache before debugging the logic.
- Verify which code path sets the property and whether it falls inside or outside a cached delegate.

## Theme and View Override Notes

Core Razor views for the storefront are under:

    src/Web/Grand.Web/Views/

The repository includes a `Theme.Modern` plugin (`src/Plugins/Theme.Modern/`) and potentially other themes. Active themes may override core views. If a theme provides its own version of `ProductLayout.Simple.cshtml` or a product partial, edits to the core view file will have no effect for stores using that theme.

Before editing a Razor view:

1. Confirm which theme is active for the store being tested.
2. Check whether the theme contains a matching override of the view you intend to edit.
3. Edit only the view that is actually served — typically the core view if no theme override exists.
4. Avoid changing multiple themes unless the feature explicitly requires it.

## Legacy Safety Rules

AI agents must treat this repository as a legacy monolith.

Before making changes:

1. Search for existing implementations.
2. Identify affected layers.
3. Prefer the smallest safe change.
4. Avoid broad refactoring.
5. Avoid changing public contracts unless required.
6. Do not modify sensitive flows unless explicitly requested.

Sensitive areas include:

- checkout
- pricing
- payment processing
- authentication
- authorization
- order placement
- tax calculation
- shipping calculation

The current feature must not change these areas.

## UI Feature Guidance

For UI-only or mostly UI features:

- Identify the view model used by the page.
- Identify where the view model is populated.
- Add derived display properties when possible.
- Render conditionally in the Razor view.
- Keep business calculations outside Razor views where practical.

## Current Feature Boundary

Feature:

    New Product Badge

Goal:

    Display a visible "New" badge on the product details page for products created within the last 30 days.

Expected scope:

- product details model or view model
- product details model factory or mapping logic
- product details Razor view
- related tests

Out of scope:

- product creation flow
- admin product management
- checkout
- cart
- pricing
- payments
- order processing
- database schema changes unless absolutely necessary

## Testing Guidance

For new feature work, prefer tests around:

- date-based badge visibility logic
- model factory or mapping behavior
- view model properties

Acceptance examples:

- product created today should show the badge
- product created 30 days ago should show the badge
- product created more than 30 days ago should not show the badge

If existing test infrastructure is difficult to run locally, document the limitation and still keep the implementation minimal.

## AI Agent Constraints

AI agents must:

- read AGENTS.md before starting work
- follow docs/ai-delivery/FEATURE_WORKFLOW.md
- follow docs/ai-delivery/DEVELOPMENT_GUIDE.md
- inspect existing code before proposing changes
- create an implementation plan before coding
- use TDD where practical
- run build or explain why validation could not be completed

AI agents must not:

- perform unrelated refactoring
- rewrite architecture
- introduce new frameworks
- change database schema without explicit approval
- modify sensitive business flows for this feature
