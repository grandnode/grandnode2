# Architecture Review

## Purpose
Identify architectural risks, boundary violations, maintainability problems, and design inconsistencies in a software repository before changes are accepted.

## When To Use
Use this skill when asked to review system design, module boundaries, dependency direction, layering, extensibility, coupling, deployment structure, or long-term maintainability.

Use this skill before large refactors, platform changes, shared library changes, cross-service integrations, or changes that affect public contracts.

## When Not To Use
Do not use this skill for narrow syntax fixes, formatting changes, dependency version bumps with no behavior change, or isolated test-only edits.

Do not use this skill as a substitute for security, database, performance, or framework-specific review when those domains are the primary concern.

## Inputs Required
- Repository root.
- Change set or target files to review.
- Stated feature goal or pull request summary.
- Existing architecture documentation, if available.
- Build, test, and deployment configuration relevant to the change.

## Instructions

### Mandatory Rules
1. Read the stated goal before inspecting implementation details.
2. Identify the affected components, modules, services, packages, and external contracts.
3. Map dependencies between affected areas.
4. Check whether dependency direction follows existing repository conventions.
5. Check whether responsibilities remain in the correct layer or module.
6. Check whether public APIs, events, messages, schemas, or configuration contracts changed.
7. Verify that compatibility, migration, and rollback concerns are handled when contracts change.
8. Look for duplicated abstractions, unnecessary indirection, circular dependencies, and hidden coupling.
9. Check whether error handling, observability, and operational behavior match existing patterns.
10. Prioritize findings by impact and likelihood.
11. Provide file and line references for each finding when possible.
12. State when no architectural issues are found.

### Recommendations
1. Prefer small, reversible design changes over broad rewrites.
2. Recommend existing repository patterns before introducing new abstractions.
3. Suggest tests that protect architectural contracts.
4. Separate confirmed defects from design preferences.
5. Include open questions only when missing information affects the review outcome.

## Constraints
- Never rewrite code unless explicitly asked.
- Never recommend a new framework, pattern, service, or dependency without tying it to a concrete problem.
- Never ignore existing project conventions in favor of generic architecture preferences.
- Never report stylistic preferences as architectural defects.
- Never invent requirements, constraints, or undocumented contracts.

## Expected Output
Produce a review report with:
- Findings ordered by severity.
- Each finding containing impact, evidence, and a concrete recommendation.
- Open questions or assumptions, if any.
- A brief summary of residual risk.
- A validation checklist result.

## Validation Checklist
- [ ] The review explains the change scope.
- [ ] Findings are tied to concrete files, interfaces, or runtime behavior.
- [ ] Dependency direction and layering were checked.
- [ ] Public contracts and compatibility were checked.
- [ ] Operational concerns were checked.
- [ ] Recommendations preserve repository conventions.
- [ ] No speculative issue is presented as fact.
- [ ] The report states when no issues are found.

## Examples

### Example 1: Layer Boundary Violation
Input: A web controller now directly imports a data access implementation.

Output finding: The controller bypasses the application service layer, which couples request handling to persistence and makes authorization rules easier to skip. Move the persistence call behind the existing service abstraction and add a service-level test.

### Example 2: Contract Change Without Migration
Input: An event payload field is renamed and consumers are not updated.

Output finding: The event contract changed without a compatibility path. Existing consumers may fail to deserialize messages. Add a versioned field, update consumers, and document the removal timeline.

