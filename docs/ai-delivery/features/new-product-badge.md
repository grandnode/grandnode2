# Feature: New Product Badge

## Goal

Display a simple "New" badge on the product details page when a product was created recently.

## Business Value

Customers can quickly identify newly added products.

This improves product discovery and makes fresh catalog items more visible.

## Requirement

A product should show a "New" badge on the product details page if it was created within the last 30 days.

## Acceptance Criteria

- Product created within the last 30 days shows the "New" badge.
- Product older than 30 days does not show the badge.
- Badge is visible on the product details page.
- Existing product details behavior is not broken.
- Implementation follows existing project patterns.
- Tests are added or updated where practical.

## Suggested Technical Direction

The AI assistant should investigate the repository before implementation.

Likely areas to inspect:

- product details model
- product details controller/action
- catalog service or model factory
- product details Razor view
- existing labels/badges in product views

Possible implementation:

- add boolean property such as `IsNew` or `ShowNewBadge` to product details model
- calculate it from product creation date
- render badge conditionally in the product details view

## Default Rule

A product is considered new when:

CreatedOnUtc >= current UTC date - 30 days

## Validation

Run:

dotnet build GrandNode.sln

Run relevant tests if available.