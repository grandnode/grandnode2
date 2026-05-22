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

## Important: Flag, MarkAsNew, and CreatedOnUtc Are Distinct

The domain model and existing codebase contain several related but separate concepts. Do not conflate them.

**`product.Flag` / `ProductDetailsModel.Flag` — admin-managed, do not overwrite**

`Flag` is a free-text string field set by administrators in the product admin panel (e.g. "Sale", "Hot", "Limited"). It is already mapped and rendered on both the product details page and catalog listing views. This field must not be set programmatically by this feature. Overwriting it would destroy admin-configured values.

**`product.MarkAsNew` / `MarkAsNewStartDateTimeUtc` / `MarkAsNewEndDateTimeUtc` — admin-configured date range, different scope**

These fields exist on the `Product` domain entity and drive a "New" indicator on catalog listing pages (`GetProductOverviewHandler`). They are admin-controlled date ranges, not derived from creation date. Do not reuse this mechanism for the product details page feature without verifying it applies to the details page context.

**`product.CreatedOnUtc` — the correct source field for this feature**

This feature must derive badge visibility from `Product.CreatedOnUtc`. This field represents when the product was created and is not admin-configurable.

**Correct implementation approach:**

- Add a new computed boolean property to `ProductDetailsModel`, for example `ShowNewBadge` or `IsNew`.
- In `GetProductDetailsPageHandler`, set the property: `ShowNewBadge = product.CreatedOnUtc >= DateTime.UtcNow.AddDays(-30)`.
- Render the badge conditionally in `ProductLayout.Simple.cshtml` using the new property.
- Do not modify `Flag`, `MarkAsNew`, or any other existing badge fields.

## Default Rule

A product is considered new when:

CreatedOnUtc >= current UTC date - 30 days

## Validation

Run:

dotnet build GrandNode.sln

Run relevant tests if available.