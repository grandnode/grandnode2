# Best Practice: Security

Patterns from `Grand.Web`, `Grand.Module.Api`, `Grand.Business.*`. Complementary to `.ai/skills/security-review.md`.

---

## Authorization

### Check permission before touching data

Every API and admin controller action must enforce permissions (either via `[PermissionAuthorize]` / `[PermissionAuthorizeAction]` attributes or explicit `IPermissionService.Authorize` checks) before returning any data or performing any mutation.

```csharp
public async Task<IActionResult> Get()
{
    if (!await _permissionService.Authorize(PermissionSystemName.Brands))
        return Forbid();
    
    return Ok(await _mediator.Send(new GetGenericQuery<BrandDto, Brand>()));
}
```

Return `Forbid()` (403), not `Unauthorized()` (401), when the user is authenticated but lacks the required permission. 401 implies "not logged in".

### Use `[PermissionAuthorize]` for simple cases

For a controller where every action requires the same permission, prefer the attribute over per-action code:

```csharp
[PermissionAuthorize(PermissionSystemName.Brands)]
public class BrandController : BaseAdminController { ... }
```

---

## Input Validation

### Use FluentValidation with record input types

Define a record for the validator's subject to make the input immutable and explicit:

```csharp
public record ShoppingCartStandardValidatorRecord(
    Customer Customer,
    Product Product,
    ShoppingCartItem ShoppingCartItem);

public class ShoppingCartStandardValidator : AbstractValidator<ShoppingCartStandardValidatorRecord>
{
    public ShoppingCartStandardValidator(ITranslationService translationService, IAclService aclService)
    {
        RuleFor(x => x).Custom((value, context) =>
        {
            if (!value.Product.Published)
                context.AddFailure(translationService.GetResource("ShoppingCart.ProductUnpublished"));
        });
    }
}
```

### Guard at service entry points

Use `ArgumentNullException.ThrowIfNull` / `ThrowIfNullOrEmpty` at the top of every service method that accepts reference parameters:

```csharp
public async Task SaveDeliveryDate(DeliveryDate deliveryDate)
{
    ArgumentNullException.ThrowIfNull(deliveryDate);
    // ...
}
```

---

## Output Encoding

### HTML-encode user-controlled text before rendering

Any text that originates from user input and is rendered into HTML must be encoded with `WebUtility.HtmlEncode`:

```csharp
name = WebUtility.HtmlEncode(_product.GetTranslation(x => x.Name, _language.Id));
```

In Razor views, `@value` auto-encodes. Never use `@Html.Raw(userInput)`.

---

## Query Safety

### Never build MongoDB filter strings from user input

Use LINQ expression predicates or typed `FilterDefinition<T>`. The MongoDB LINQ provider translates them to safe queries — there is no query injection risk through typed expressions.

```csharp
// safe — LINQ expression
var product = await _repository.GetOneAsync(p => p.Sku == userInput);

// never — would open injection if the driver used string parsing
var product = await collection.Find($"{{ sku: '{userInput}' }}").FirstOrDefaultAsync();
```

---

## Secrets

Never commit connection strings, API keys, or credentials to source. Use `appsettings.json` with `UserSecrets` in development and environment variables / Azure Key Vault in production. No hardcoded credentials anywhere in code.

---

## Summary Checklist

- [ ] Every action checks `IPermissionService.Authorize` before data access
- [ ] Returns `Forbid()` not `Unauthorized()` for permission failures
- [ ] Business rule validation uses FluentValidation with record input types
- [ ] Service entry points guard null arguments with `ArgumentNullException.ThrowIfNull`
- [ ] User text rendered as HTML goes through `WebUtility.HtmlEncode`
- [ ] MongoDB queries use LINQ expressions, never string interpolation
- [ ] No secrets in source code
