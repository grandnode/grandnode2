# MongoDB Review

## Purpose
Review MongoDB-related repository changes for query correctness, index fit, data integrity, migration safety, concurrency behavior, and production performance.

## When To Use
Use this skill when reviewing MongoDB collections, repository methods, filters, projections, sorting, pagination, indexes, aggregation pipelines, update operations, migrations, seed data, connection configuration, or MongoDB driver usage.

Use this skill when a change reads, writes, transforms, migrates, imports, exports, or deletes persisted MongoDB data.

## When Not To Use
Do not use this skill for changes that do not touch MongoDB-backed persistence, repository behavior, data migrations, or data access configuration.

Do not use this skill as the primary review for authentication, authorization, secrets, or business workflow correctness; combine it with the relevant review skill when those concerns are present.

## Inputs Required
- Repository root.
- Change set or target files to review.
- MongoDB driver version and data access abstractions.
- Affected domain models, repository methods, migrations, and configuration files.
- Existing indexes and expected data volume, if available.
- Deployment and rollback process for data migrations, if available.

## Instructions

### Mandatory Rules
1. Identify every affected collection, document type, repository method, migration, and query path.
2. Check filters for correct tenant, store, vendor, customer, language, currency, status, and soft-delete scoping when those fields apply.
3. Check queries for missing filters, incorrect boolean logic, case-sensitivity mistakes, timezone mistakes, and null or empty value handling.
4. Check sorting and pagination for deterministic ordering, stable page boundaries, and index support.
5. Check projections for over-fetching, missing required fields, accidental sensitive field exposure, and shape mismatches.
6. Check update operations for atomicity, lost-update risk, unintended full-document replacement, array update correctness, and upsert safety.
7. Check delete operations for scope, cascade expectations, soft-delete behavior, and irreversible data loss.
8. Check aggregation pipelines for stage order, memory risk, cardinality expansion, lookup cost, grouping correctness, and index use before blocking stages.
9. Check indexes for query coverage, selectivity, uniqueness requirements, sort support, redundant overlap, write overhead, and migration safety.
10. Check migrations for idempotency, batching, resumability, runtime duration, lock impact, rollback behavior, and compatibility with mixed application versions.
11. Check transactions and multi-document workflows for atomicity requirements, retry behavior, write concern, read concern, and partial failure handling.
12. Check repository abstractions for materialization points, repeated enumeration, N+1 access patterns, and client-side filtering after broad reads.
13. Check configuration for connection string handling, database name selection, timeouts, retry behavior, pooling, TLS, and environment-specific settings.
14. Prioritize findings by risk to data integrity, tenant isolation, availability, and performance.
15. Provide file and line references for each finding when possible.
16. State when no MongoDB issues are found.

### Recommendations
1. Prefer query filters that include all required scoping fields at the database level.
2. Prefer atomic update operators over read-modify-write when concurrent writers can touch the same document.
3. Prefer keyset pagination for large or frequently changing result sets.
4. Prefer expand-and-contract data migrations for breaking document shape changes.
5. Recommend representative data-volume testing for expensive queries, indexes, or migrations.
6. Recommend query plan inspection when performance depends on index selection.
7. Separate confirmed defects from scalability warnings and cleanup suggestions.

## Constraints
- Never assume production collections are small unless explicitly documented.
- Never recommend dropping data, fields, indexes, or collections without a migration, backup, rollback, and usage check.
- Never rely on application-side filtering for tenant, store, vendor, or permission isolation when database-side filtering is possible.
- Never ignore deployment order between application code and data migrations.
- Never present an index recommendation without naming the query pattern it supports.
- Never expose real connection strings, secrets, tokens, or personal data in the output.

## Expected Output
Produce a MongoDB review report with:
- Findings ordered by severity.
- Each finding containing affected collection or query path, data risk, performance or availability impact, evidence, and remediation.
- Index and migration concerns.
- Commands or validation steps executed or recommended.
- Open questions or assumptions, if any.
- A validation checklist result.

## Validation Checklist
- [ ] Affected collections, models, repository methods, and migrations were identified.
- [ ] Tenant, store, vendor, customer, and permission scoping were checked where relevant.
- [ ] Filters, projections, sorting, and pagination were checked.
- [ ] Update, delete, and upsert operations were checked.
- [ ] Aggregation pipelines were checked where relevant.
- [ ] Index fit and write overhead were checked.
- [ ] Migration safety and deployment order were checked.
- [ ] Concurrency and partial failure behavior were checked.
- [ ] Configuration and connection handling were checked where relevant.
- [ ] Findings distinguish confirmed defects from risk-based recommendations.

## Examples

### Example 1: Missing Store Scope
Input: A repository method loads discount records by coupon code only.

Output finding: The query does not include store scope. In a multi-store deployment, a coupon from one store can be applied in another store if codes collide. Add `StoreId` or the repository's established store filter to the MongoDB query and add a cross-store negative test.

### Example 2: Unstable Pagination
Input: A product listing sorts only by `CreatedOnUtc` and uses skip/limit pagination.

Output finding: Pagination is unstable when multiple products share the same timestamp or new products are inserted between requests. Add a deterministic tie-breaker such as `_id`, ensure the compound sort has index support, and consider keyset pagination for large catalogs.

### Example 3: Unsafe Read-Modify-Write
Input: A service reads an order document, increments a counter in memory, and replaces the whole document.

Output finding: Concurrent writers can lose updates because the operation replaces a stale document snapshot. Use an atomic `$inc` or a conditional update with version checking, and test concurrent updates.

