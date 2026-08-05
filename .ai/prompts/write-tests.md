# Prompt: Write Tests

## Purpose
Add or extend unit tests for a change, using the MSTest + Moq conventions already in `src/Tests/`.

## Inputs Required
- Repository root.
- The type or behavior under test.
- The change or bug the tests must cover.

## Steps

1. Read `.ai/knowledge/tests.md` for the MSTest + Moq patterns, test structure, validator testing, and controller test setup.
2. Locate the mirror test project. Test projects mirror source projects one-to-one:

   | Source | Tests |
   |---|---|
   | `src/Business/Grand.Business.Catalog` | `src/Tests/Grand.Business.Catalog.Tests` |
   | `src/Web/Grand.Web` | `src/Tests/Grand.Web.Tests` |
   | `src/Web/Grand.Web.Admin` | `src/Tests/Grand.Web.Admin.Tests` |
   | `src/Web/Grand.Web.Store` | `src/Tests/Grand.Web.Store.Tests` |
   | `src/Core/Grand.Infrastructure` | `src/Tests/Grand.Infrastructure.Tests` |
   | `src/Modules/Grand.Module.Api` | `src/Tests/Grand.Module.Api.Tests` |
   | other modules | `src/Tests/Grand.Modules.Tests` |

3. Read the nearest existing test class in the target project and copy its structure — fixture setup, mock naming, assertion style.
4. Write tests that cover, in this order: the happy path, the boundary the change introduced, and the failure the change fixes.
5. For a bug fix, write the failing test first and confirm it fails for the stated reason before applying the fix.
6. Run only the affected test project.

## Mandatory Rules

1. Mock at the interface boundary (`IRepository<T>`, business service interfaces, `IMediator`), not concrete infrastructure.
2. Do not hit a real database, network, or file system in a unit test.
3. Each test asserts one behavior, and its name says what that behavior is.
4. Do not change production code to make a test easier unless the change is an improvement on its own terms.
5. Do not weaken or delete an existing assertion to make a suite pass — investigate why it now fails.

## Output Format

- **Under test**: type and behavior.
- **Tests added**: name + what each one pins down.
- **Bug fix proof**: for fixes, confirmation that the test failed before and passes after.
- **Run**: the exact test command and its result.
- **Not covered**: behavior that remains untested and why.
