# Database Review

## Purpose
Review database-related changes for correctness, data integrity, performance, migration safety, concurrency behavior, and maintainability.

## When To Use
Use this skill when reviewing schema changes, migrations, indexes, constraints, queries, stored procedures, ORM mappings, seed data, transactions, reporting queries, or data access patterns.

Use this skill before deploying changes that modify persistent data shape, query behavior, data retention, or database configuration.

## When Not To Use
Do not use this skill for code changes that do not read, write, migrate, or configure persistent data.

Do not use this skill as the primary review for authentication, authorization, or cryptographic storage; combine it with a security review when sensitive data is involved.

## Inputs Required
- Repository root.
- Change set or target files to review.
- Database engine and version, if known.
- Migration files, schema definitions, ORM mappings, and query code.
- Existing indexes, constraints, and relevant data volume assumptions.
- Rollback or deployment process, if available.

## Instructions

### Mandatory Rules
1. Identify all tables, collections, views, indexes, constraints, migrations, and queries affected by the change.
2. Check schema changes for backward compatibility with currently deployed application versions.
3. Check migrations for lock risk, runtime duration, batching needs, transactional safety, idempotency, and rollback behavior.
4. Check column and type changes for data loss, truncation, precision loss, nullability conflicts, default values, and timezone behavior.
5. Check constraints for existing-data compatibility and intended enforcement.
6. Check indexes for query support, selectivity, write overhead, uniqueness needs, and redundant overlap.
7. Check query changes for filtering correctness, join cardinality, sort behavior, pagination stability, N+1 access, and avoidable full scans.
8. Check transactions for atomicity, isolation assumptions, retry behavior, deadlock risk, and partial update failure.
9. Check concurrency behavior for lost updates, duplicate creation, stale reads, and optimistic or pessimistic locking requirements.
10. Check ORM mappings for cascade behavior, tracking behavior, lazy loading, required relationships, and migration consistency.
11. Check seed or reference data changes for environment safety and repeatability.
12. Prioritize findings by risk to data integrity, availability, and performance.
13. Provide file and line references for each finding when possible.
14. State when no database issues are found.

### Recommendations
1. Prefer expand-and-contract migrations for breaking schema changes.
2. Prefer database-enforced integrity for invariants that must survive concurrent writers.
3. Recommend representative data-volume testing for expensive migrations or queries.
4. Recommend query plan inspection when performance depends on index selection.
5. Separate confirmed defects from scalability warnings.

## Constraints
- Never assume production data is small unless explicitly documented.
- Never recommend destructive migrations without a backup, rollout, and rollback plan.
- Never rely only on application validation for critical data integrity.
- Never ignore compatibility between application deployment order and migration order.
- Never propose dropping columns, indexes, constraints, or data without proving they are unused or safely replaced.

## Expected Output
Produce a database review report with:
- Findings ordered by severity.
- Each finding containing data risk, operational impact, evidence, and remediation.
- Migration and rollback concerns.
- Performance concerns and recommended validation.
- Open questions or assumptions, if any.
- A validation checklist result.

## Validation Checklist
- [ ] Affected schema objects and queries were identified.
- [ ] Migration safety and deployment order were checked.
- [ ] Data loss and compatibility risks were checked.
- [ ] Constraints and indexes were checked.
- [ ] Transaction and concurrency behavior were checked.
- [ ] ORM mappings were checked where relevant.
- [ ] Performance validation was recommended where needed.
- [ ] Destructive operations include safety requirements.

## Examples

### Example 1: Non-Nullable Column Without Default
Input: A migration adds a required column to an existing large table without a default value.

Output finding: The migration can fail on existing rows or require a long exclusive lock while backfilling. Add the column as nullable, backfill in batches, enforce not-null after validation, and define rollback behavior.

### Example 2: Offset Pagination Instability
Input: A query uses offset pagination ordered only by a non-unique timestamp.

Output finding: Results can be duplicated or skipped when multiple rows share the same timestamp or new rows are inserted. Add a deterministic tie-breaker and consider keyset pagination for large result sets.

