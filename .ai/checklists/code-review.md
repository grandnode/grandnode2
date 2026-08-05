# Checklist: Code Review

For reviewing a diff. Report only high-confidence findings, each with file, line, the rule violated, the failure it causes, and a minimal fix.

Use with `.ai/prompts/review-change.md`, which handles skill selection.

---

## Read the diff twice

- [ ] First pass: does the change do what its description claims?
- [ ] Second pass: what did it change that the description does not mention?

## Layering

- [ ] No business logic in a controller.
- [ ] No view model in a business service.
- [ ] No MongoDB driver type outside `Grand.Data`.
- [ ] No HTTP or `IWorkContext` dependency in `Grand.Domain`.
- [ ] No core → plugin reference.
- [ ] Services registered in `IStartupApplication`, not `Program.cs`.

## Scoping — the highest-yield section

- [ ] Every new query filters by store.
- [ ] Vendor-reachable code filters by `VendorId` and re-checks ownership on write.
- [ ] `LimitedToStores` / `LimitedToGroups` honoured where the entity supports them.
- [ ] Every new cache key contains store id — and language id if the data is localized.
- [ ] Settings loaded and saved with the same store scope.
- [ ] No `IWorkContext` in a scheduled task, migration, or plugin install.

## Duplication and reuse

- [ ] The logic does not already exist in a service, extension, or helper.
- [ ] A near-copy of an existing method was not introduced instead of a parameter.
- [ ] A third repetition of the same block was extracted rather than added.
- [ ] Copy-pasted code had **every** identifier updated — stale names from the source are a classic defect.

## Validation

- [ ] Inputs crossing a trust boundary are validated.
- [ ] Validators cover the new fields, not just the old ones.
- [ ] Guard clauses use `ArgumentNullException.ThrowIfNull`.
- [ ] Invalid model state does not partially save.

## Exception handling

- [ ] No empty `catch`, no `catch (Exception)` that continues silently.
- [ ] Expected business failures return results, not exceptions.
- [ ] Notification handlers cannot throw into the publisher.
- [ ] Migrations return `false` instead of throwing.
- [ ] Disposables are disposed.

## Logging

- [ ] Diagnosable failures are logged with store/entity/operation context.
- [ ] No secrets or personal data in log output.
- [ ] Log levels are proportionate.
- [ ] No logging in a hot loop.

## Async

- [ ] No `.Result`, `.Wait()`, `GetAwaiter().GetResult()`.
- [ ] No `async void`.
- [ ] No `Task.Run` wrapping synchronous work.
- [ ] `CancellationToken` forwarded where the surrounding signatures carry one.

## Data lifecycle

- [ ] Every write invalidates its cache prefix.
- [ ] Every write publishes its entity event.
- [ ] Cross-family caches that embed the entity are invalidated too.
- [ ] Migration `Identity` GUID is new.
- [ ] Migration is idempotent and destroys nothing operator-owned.
- [ ] No persisted identity renamed (plugin system name, permission name, template name, task name).

## Localization

- [ ] No hardcoded user-facing string.
- [ ] Resource keys follow the naming convention and are added **and** removed symmetrically in plugin install/uninstall.
- [ ] Localized entity properties read through the translation extension, not the raw property.

## Frontend

- [ ] Storefront data attributes preserved on touched views.
- [ ] Widget zones preserved.
- [ ] No `Html.Raw` on user content.
- [ ] Bundles rebuilt if source changed.

## Tests

- [ ] New behavior has tests, in the mirror project.
- [ ] Bug fixes have a regression test.
- [ ] No assertion weakened to make the suite pass.
- [ ] Tests mock at interface boundaries and hit no real database or network.

## Backward compatibility

- [ ] An existing installation upgrading in place still works.
- [ ] New settings default to previous behavior.
- [ ] No public interface, view model, or route changed without the PR saying so under "Breaking changes".

## Before submitting the review

- [ ] Every finding verified by opening the file — no speculative claims.
- [ ] Findings ranked by severity, not by file order.
- [ ] Style and preference comments omitted unless a mandatory rule is broken.
