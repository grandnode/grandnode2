# Checklist: Definition of Done

Run on every change before reporting it complete.

---

## Scope

- [ ] The change does what was asked — no less.
- [ ] The change does **only** what was asked; unrelated refactors and formatting churn are out.
- [ ] Anything deliberately left out is stated explicitly, with the reason.

## Correctness

- [ ] The happy path was exercised, not just compiled.
- [ ] Boundary cases considered: empty collection, null, zero quantity, first page, last page.
- [ ] Store scope applied to every query and every cache key the change touches.
- [ ] Vendor scope applied where a vendor could reach the code.
- [ ] Behavior verified for the product types / order statuses / payment flows the change affects, or the untested ones named.

## Code quality

- [ ] No duplicated logic — the closest existing helper, service, or extension was reused. If similar code exists in three places now, that is a finding.
- [ ] Follows the closest existing file's structure, naming, and idiom.
- [ ] Naming matches `.ai/standards/naming.md` and the domain vocabulary in `.ai/glossary/`.
- [ ] No constraint from `.ai/constraints.md` violated.
- [ ] Dead code, commented-out code, and debug output removed.

## Validation

- [ ] Every input that crosses a trust boundary is validated — FluentValidation for models, guard clauses for service arguments.
- [ ] Invalid model state re-renders rather than partially saving.
- [ ] Server-side ownership re-checked for any id that arrived in a request.

## Exception handling

- [ ] Expected failures return result objects, not exceptions.
- [ ] No empty `catch`.
- [ ] Nothing catches broadly and continues as if it succeeded.
- [ ] Notification handlers and migrations cannot throw into their caller.
- [ ] Resources that need disposing are in `using` blocks.

## Logging

- [ ] Failures that an operator would need to diagnose are logged, with enough context to identify the store, entity, and operation.
- [ ] No secrets, tokens, passwords, card data, or full personal records in log messages.
- [ ] Log levels are honest: `Error` for something broken, `Warning` for something suspicious, not everything at `Information`.
- [ ] No logging inside a hot loop.

## Data safety

- [ ] Every write invalidates the affected cache prefixes.
- [ ] Every write publishes its entity event.
- [ ] New settings default to the pre-change behavior.
- [ ] New user-facing strings exist as localization resources.
- [ ] New permissions have a provider entry and a migration.
- [ ] An existing installation upgrading in place still works.

## Tests

- [ ] Tests added for new behavior, in the mirror test project.
- [ ] For a bug fix: a test that failed before the fix and passes after.
- [ ] No existing assertion weakened or deleted to make the suite pass.
- [ ] The affected test project runs green.

## Build and delivery

- [ ] The affected projects build.
- [ ] Frontend bundles rebuilt and committed if frontend source changed.
- [ ] Plugin/theme output path verified for Debug **and** Release, if applicable.
- [ ] PR follows `.ai/standards/git-and-pr.md`, with a truthful "Breaking changes" section.

## Reporting

- [ ] Commands actually run are listed, with results.
- [ ] Commands that could not be run are named.
- [ ] Remaining risk is stated plainly — not omitted because it is small.
