# Checklists

Cross-cutting gates. Skills carry their own domain checklists (a widget plugin's `Validation Checklist` belongs in the widget skill); these cover what no single skill owns.

| Checklist | Run it |
|---|---|
| `definition-of-done.md` | Before calling any change complete |
| `code-review.md` | When reviewing someone else's diff, or your own before opening a PR |
| `security.md` | When the change touches auth, input, scoped data, secrets, or file handling |
| `performance.md` | When the change adds a query, a loop over entities, or a page render path |
| `data-change.md` | When the change touches entities, migrations, settings, or persisted identities |
| `plugin-release.md` | Before shipping a plugin or theme |

## How to use

1. Run `definition-of-done.md` on every change. Add the situational ones the change triggers.
2. A checklist item is a question to answer, not a box to tick by reflex. "Yes, because …" is the passing answer.
3. Report items that do not apply as N/A with a reason. Silence reads as an unchecked box.
4. An item you cannot verify is a risk to state in the PR, not an item to skip.
