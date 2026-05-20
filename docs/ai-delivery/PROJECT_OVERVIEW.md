# Project Overview

## Application

GrandNode2 is an open-source e-commerce platform.

The application is a monolith with modular/plugin-based areas.

## Main Technology Stack

- ASP.NET Core
- C#
- Razor Views
- JavaScript
- MongoDB
- Docker
- Plugin architecture

## Repository Areas

Expected important areas:

- `src/Web` — web application and Razor views
- `src/Grand.Domain` — domain entities
- `src/Grand.Business` — business services
- `src/Grand.Infrastructure` — shared infrastructure
- `src/Plugins` — plugin implementations
- `test` or related test projects — automated tests

## AI Delivery Notes

AI agents should inspect the actual project structure before changing files.

Do not assume architecture details without checking the repository.

Prefer existing patterns over introducing new abstractions.