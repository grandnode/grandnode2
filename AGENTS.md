# AGENTS.md

## Project

GrandNode2 is a monolithic e-commerce application built with ASP.NET Core, C#, Razor views, JavaScript, and MongoDB.

This repository is being prepared for AI-powered delivery using Superpowers.

## AI Agent Rules

AI agents working in this repository must:

1. Analyze the task before making changes.
2. Create an implementation plan before coding.
3. Use TDD where practical.
4. Keep changes small and focused.
5. Avoid unrelated refactoring.
6. Follow existing project structure and naming conventions.
7. Run build/tests before completing the task.
8. Use git branches and meaningful commits.

## Development Workflow

For each feature:

1. Brainstorm the solution.
2. Identify affected files.
3. Write or update tests first.
4. Implement the minimum required code.
5. Run validation.
6. Commit changes.

## Current Feature

Display a simple "New" badge on the product details page for products created within the last 30 days.

See:

`docs/ai-delivery/features/new-product-badge.md`