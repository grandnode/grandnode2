# Standard: C# Style

Formatting rules enforced by `.editorconfig` at the repository root, plus the conventions the codebase applies consistently on top of it. Complementary to `.ai/knowledge/dotnet.md`, which covers idioms rather than layout.

---

## Enforced by `.editorconfig`

These are not preferences — they are configured for `[*.cs]`:

| Rule | Setting |
|---|---|
| Allman braces (methods, types, control blocks, lambdas, initializers, anonymous types) | `csharp_new_line_before_open_brace` |
| `catch` and `else` on a new line | `csharp_new_line_before_catch`, `csharp_new_line_before_else` |
| Indent switch labels and case contents | `csharp_indent_switch_labels`, `csharp_indent_case_contents` |
| `System.*` usings are **not** sorted first | `dotnet_sort_system_directives_first = false` |
| No space after a cast | `csharp_space_after_cast = false` |
| Space around `:` in inheritance clauses | `csharp_space_*_colon_in_inheritance_clause = true` |
| No space between method name and `(` | `csharp_space_between_method_call_name_and_opening_parenthesis = false` |
| Single-line blocks and statements are preserved | `csharp_preserve_single_line_*` |
| Block bodies preferred over expression bodies for methods and constructors | `csharp_style_expression_bodied_methods/constructors = false` |
| `var` for built-in types and when the type is apparent | `csharp_style_var_*` |
| Language keywords over BCL type names (`string`, not `String`) | `dotnet_style_predefined_type_*` |
| No `this.` qualification | `dotnet_style_qualification_for_* = false` |

Expression bodies are still used across the codebase for properties, single-expression interface implementations, and small members — the `false:suggestion` rules only cover methods and constructors.

## Language level

`src/Build/Grand.Common.props` sets:

- `TargetFramework` = `net10.0`
- `LangVersion` = `latest`
- `ImplicitUsings` = `true`
- `System.Text` is a global using

Do not add `using System;` and friends that implicit usings already provide.

## File layout

File-scoped namespaces throughout:

```csharp
using Grand.Infrastructure.Plugins;

namespace Theme.Modern;

public class ModernThemeView : IThemeView
{
}
```

Order inside a type: constants, fields, constructor, properties, public methods, private methods. Nested types last.

## Constructors and dependencies

Constructor injection only. Assign to `readonly` fields:

```csharp
private readonly IProductService _productService;
private readonly ICacheBase _cacheBase;

public ProductViewModelService(IProductService productService, ICacheBase cacheBase)
{
    _productService = productService;
    _cacheBase = cacheBase;
}
```

Do not resolve from `IServiceProvider` inside a service. The exception is `IMigration.UpgradeProcess(IServiceProvider)`, where the signature requires it.

## Async

- `async`/`await` all the way down; no `.Result`, `.Wait()`, or `Task.Run` to bridge sync and async.
- Accept and forward `CancellationToken` where the surrounding signatures do.
- Return `Task` directly (without `async`) only when the method is a pure pass-through.
- See `.ai/knowledge/async.md` for the full rules.

## Nullability and guards

- Use `ArgumentNullException.ThrowIfNull` / `ThrowIfNullOrEmpty` at the top of public service methods, not hand-written `if (x == null) throw`.
- Prefer result objects over exceptions for expected business failures.
- Use pattern matching (`is null`, `is not null`, switch expressions) over `== null` chains in new code.

## Comments

- XML doc comments on public interface members and on non-obvious service methods; the codebase uses `/// <summary>` widely on interfaces.
- No commented-out code.
- No `TODO` without a linked issue number.
- Match the comment density of the file you are editing.

## What not to introduce

- No new DI container, mapper, or validation library — the repository uses Microsoft DI, `Grand.Mapping`, and FluentValidation. AutoMapper is **not** referenced despite the AutoMapper-compatible profile API.
- No static mutable state.
- No `#region`.
- No reflection-based lookups where a DI registration or a provider interface exists.
