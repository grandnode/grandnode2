# .NET Review

## Purpose
Review .NET repository changes for correctness, framework consistency, maintainability, runtime safety, MongoDB-backed data access, and test coverage.

## When To Use
Use this skill when reviewing C#, F#, Visual Basic, ASP.NET Core, MongoDB repository usage, NuGet package configuration, MSBuild files, background services, middleware, dependency injection, LINQ, async code, or .NET test projects.

Use this skill for changes that affect request handling, domain logic, persistence, serialization, configuration, build behavior, or package versions.

## When Not To Use
Do not use this skill for non-.NET files unless they directly affect .NET build, deployment, runtime configuration, or generated code.

Do not use this skill as the primary review for database design, MongoDB index strategy, or security-sensitive changes; combine it with the relevant review skill.

## Inputs Required
- Repository root.
- Change set or target files to review.
- Target framework versions.
- Solution, project, package, and build files relevant to the change.
- Existing tests and test framework conventions.
- Runtime configuration files relevant to the change.

## Instructions

### Mandatory Rules
1. Identify affected projects, target frameworks, package references, and build properties.
2. Check that code follows existing repository conventions for naming, layering, dependency injection, logging, validation, and error handling.
3. Check async code for missing awaits, sync-over-async, unobserved tasks, blocking calls, cancellation token misuse, and context leaks.
4. Check dependency injection registrations for lifetime mismatches, duplicate registrations, missing registrations, and service locator patterns.
5. Check nullable reference type handling when enabled.
6. Check LINQ and collection usage for repeated enumeration, client-side evaluation risk, null handling, and avoidable materialization.
7. Check ASP.NET Core changes for model binding, validation, filters, middleware order, status codes, routing, antiforgery behavior, and response serialization.
8. Check MongoDB-backed repository usage for filter correctness, projection shape, pagination stability, update atomicity, transaction boundaries, concurrency handling, and avoidable N+1 access patterns.
9. Check configuration access for missing defaults, unsafe environment assumptions, and inconsistent option binding.
10. Check package changes for version conflicts, central package management consistency, and unnecessary dependencies.
11. Check tests for meaningful assertions, correct isolation, deterministic behavior, and coverage of changed behavior.
12. Run or recommend the narrowest relevant build and test commands when execution is available.
13. Prioritize findings by runtime impact and maintainability risk.
14. Provide file and line references for each finding when possible.
15. State when no .NET issues are found.

### Recommendations
1. Prefer repository-established helpers, base classes, extension methods, and test utilities.
2. Prefer explicit cancellation support for I/O, background work, and request-scoped operations.
3. Prefer options validation for required configuration.
4. Recommend integration tests for framework pipeline behavior.
5. Separate correctness issues from style or modernization suggestions.

## Constraints
- Never introduce a new package recommendation when the platform or repository already provides the needed capability.
- Never assume MongoDB query behavior without checking filters, projections, sorting, pagination, and materialization points.
- Never ignore project-level settings such as nullable annotations, implicit usings, analyzers, or central package management.
- Never recommend broad framework upgrades unless the change requires them.
- Never treat generated files as source of truth when source templates or generators are present.

## Expected Output
Produce a .NET review report with:
- Findings ordered by severity.
- Each finding containing runtime impact, evidence, and remediation.
- Build and test commands executed or recommended.
- Open questions or assumptions, if any.
- A validation checklist result.

## Validation Checklist
- [ ] Affected projects and target frameworks were identified.
- [ ] Build and package configuration were checked.
- [ ] Async, DI, nullable, LINQ, and configuration patterns were checked where relevant.
- [ ] ASP.NET Core behavior was checked where relevant.
- [ ] MongoDB-backed repository behavior was checked where relevant.
- [ ] Tests were reviewed for changed behavior.
- [ ] Recommendations match existing repository conventions.
- [ ] The report states whether commands were run.

## Examples

### Example 1: Scoped Service Captured By Singleton
Input: A singleton background dispatcher receives a scoped repository through constructor injection.

Output finding: The singleton captures a scoped dependency, which can cause disposed context usage and cross-request state leakage. Resolve scoped services inside a created scope for each operation and add a background service test.

### Example 2: Missing Await
Input: A controller calls an asynchronous save method without awaiting it.

Output finding: The response may return before persistence completes and exceptions may be unobserved. Await the save operation, pass the request cancellation token, and assert persistence in the controller test.
