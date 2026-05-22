# Project Overview

## Application

GrandNode2 is an open-source e-commerce platform built on ASP.NET Core and MongoDB.

The application follows a monolithic architecture with modular/plugin-based areas.

This repository is being prepared for AI-powered delivery using Superpowers workflows and AI-assisted engineering practices.

## Main Technology Stack

- ASP.NET Core
- C#
- Razor Views
- JavaScript
- MongoDB
- Docker
- Plugin architecture

## Repository Structure

Important repository areas:

- `src/Web` — web application, controllers, Razor views, UI rendering
- `src/Grand.Domain` — domain entities and core business models
- `src/Grand.Business` — business services and application logic
- `src/Grand.Infrastructure` — infrastructure and shared utilities
- `src/Plugins` — plugin implementations
- test projects — automated tests and validation

The exact structure should always be verified before implementation.

## Build and Validation

Typical commands:

    dotnet restore GrandNode.sln
    dotnet build GrandNode.sln
    dotnet test GrandNode.sln

Some tests or integrations may require additional local setup.

## AI Delivery Notes

AI assistants working in this repository should:

- inspect existing patterns before changing code
- prefer minimal and focused implementations
- avoid unrelated refactoring
- follow existing architecture and naming conventions
- use TDD where practical
- document assumptions during implementation

## Current Feature Focus

Current feature under development:

New Product Badge

Goal:
Display a simple “New” badge on the product details page for recently created products.
