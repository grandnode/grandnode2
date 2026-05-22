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
