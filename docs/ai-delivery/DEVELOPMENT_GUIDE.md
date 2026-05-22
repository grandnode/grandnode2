# Development Guide

## Purpose

This repository is prepared for AI-assisted software delivery using Superpowers workflows and structured engineering practices.

This guide explains how contributors and AI agents should work with the project.

## Initial Setup

Restore dependencies:

    dotnet restore GrandNode.sln

Build the solution:

    dotnet build GrandNode.sln

Run tests:

    dotnet test GrandNode.sln

Some tests or integrations may require additional local infrastructure.

## Branching Strategy

Use feature branches for all changes.

Recommended naming:

    feature/<feature-name>

Example:

    feature/new-product-badge

## Commit Guidelines

Commit messages should be descriptive and focused.

Examples:

    Add AI delivery documentation
    Implement new product badge logic
    Add tests for new product badge

Avoid combining unrelated changes into a single commit.

## AI Workflow

AI assistants should follow this workflow:

1. Read repository documentation
2. Analyze the repository structure
3. Create an implementation plan
4. Identify affected files
5. Add or update tests
6. Implement minimal required changes
7. Run validation
8. Commit changes

## TDD Guidance

Where practical, use test-driven development.

Preferred workflow:

1. Write failing test
2. Implement feature
3. Make tests pass
4. Refactor only if needed

## Implementation Rules

AI agents and contributors should:

- follow existing architecture patterns
- avoid unnecessary abstractions
- avoid unrelated refactoring
- keep changes minimal and focused
- prefer consistency over creativity
- inspect existing implementations before adding new ones

## Validation

Before completing work, run:

    dotnet build GrandNode.sln

Run tests when possible:

    dotnet test GrandNode.sln

If validation cannot run locally, document the reason.

## Current Active Feature

Feature:
New Product Badge

Goal:
Display a visible “New” badge on the product details page for products created within the last 30 days.
