# Plugin — Discount Rules Provider

## Purpose
Create, modify, and review GrandNode discount rule plugins that implement `IDiscountProvider` and one or more `IDiscountRule` classes.

## When To Use
Use this skill when building a new discount requirement rule, adding a rule type to an existing plugin, changing rule validation logic, building a rule configuration UI, or reviewing discount rule correctness.

## When Not To Use
Do not use this skill for discount domain entity management, pricing calculation, or general plugin infrastructure; combine with `plugin-module` when foundational setup is needed.

## Inputs Required
- Repository root.
- The condition the rule validates (e.g., customer group, minimum spend, product ownership).
- Whether the rule needs admin configuration per discount instance.
- What data the rule needs at validation time (customer, cart, store, etc.).

## Instructions

### Mandatory Rules

#### Provider and Rule Interfaces
1. Implement `IDiscountProvider` from `Grand.Business.Core.Interfaces.Catalog.Discounts`. This extends `IProvider` requiring `ConfigurationUrl`, `SystemName`, `FriendlyName`, `Priority`, `LimitedToStores`, `LimitedToGroups`.
2. Implement `IDiscountProvider.GetRequirementRules()` to return all `IDiscountRule` instances this plugin provides.
3. For each rule type, create a class implementing `IDiscountRule`:

| Member | Requirement |
|---|---|
| `string SystemName` | Unique identifier in the format `"{PluginSystemName}.{RuleName}"`, e.g. `"DiscountRules.Standard.MustBeAssignedToCustomerGroup"`. |
| `string FriendlyName` | Human-readable rule name shown in the admin discount editor. |
| `Task<DiscountRuleValidationResult> CheckRequirement(request)` | Validation logic. Return `result.IsValid = true` on pass; set `result.UserError` when the rule fails and you want a customer-facing message. |
| `string GetConfigurationUrl(discountId, requirementId)` | Return the URL of the rule's admin configuration page. Format: `"/{ControllerName}/Configure/?discountId={discountId}&discountRequirementId={requirementId}"`. Return `""` if the rule needs no configuration. |

4. Keep `CheckRequirement` idempotent and side-effect free — it is called on every cart recalculation.
5. Read rule configuration from `request.DiscountRule.Metadata` (a string stored per `DiscountRule` instance). Parse the format you write during configuration (plain string, ID, JSON, etc.).

#### DiscountRuleValidationRequest (key fields)
```csharp
Discount      Discount     { get; }   // the discount being validated
DiscountRule  DiscountRule { get; }   // the rule instance (carries Metadata)
Customer      Customer     { get; }   // current customer
Store         Store        { get; }   // current store
```

#### Admin Configuration Controller
6. Create a storefront-accessible controller (no `[Area("Admin")]`) for each rule that requires configuration. The route must match `GetConfigurationUrl`.
7. The controller renders a configuration form that saves the chosen value (customer group ID, amount, product ID, etc.) back to `DiscountRule.Metadata` via the discount service.
8. Inject `IDiscountService` to load and update the `DiscountRule` entity during save.
9. Protect the configuration controller with `[AuthorizeAdmin]` since it is accessed from the admin discount edit page via an iframe or popup. Use `[Area("Admin")]` when it is admin-routed; omit the area when it is accessed from the public route prefix (follow the nearest existing rule controller pattern).

#### DI Registration
10. Register the provider and all rule classes in `StartupApplication.ConfigureServices`:
    ```csharp
    services.AddScoped<IDiscountProvider, YourDiscountProvider>();
    services.AddScoped<YourRule1>();
    services.AddScoped<YourRule2>();
    ```
    Inject the rule instances into the provider constructor and return them from `GetRequirementRules()`.

#### Project Structure
11. Use `Microsoft.NET.Sdk.Razor` when configuration views are present; `Microsoft.NET.Sdk` otherwise.
12. Set output path to `..\..\Web\Grand.Web\Plugins\{SystemName}\`.
13. Mark all GrandNode project references `Private="false"`.
14. Include `logo.jpg` with `CopyToOutputDirectory = Always`.

#### Manifest, Defaults, Settings
15. Define `[assembly: PluginInfo(...)]` with `Group = "Discount rules"`.
16. Define a `Defaults` class with `ProviderSystemName`, `FriendlyName` (resource key), and system names for each rule type.
17. Plugin-level `ISettings` is only needed when the plugin itself has shared configuration across all rule instances. Per-instance rule config goes in `DiscountRule.Metadata`.

#### Startup and Install
18. In `Install()`: register localization keys for the plugin and each rule's `FriendlyName`. Call `base.Install()` last.
19. In `Uninstall()`: remove resource keys. Call `base.Uninstall()` last. If the plugin owns no settings, `DeleteSetting` is not needed.

### Recommendations
1. Prefer `DiscountRules.Standard` as the template — it bundles multiple rules in one plugin, showing the full pattern.
2. Prefer storing a single domain ID (group ID, product ID) as `Metadata` for simple rules rather than serializing complex objects.
3. Prefer returning an empty `UserError` when the rule fails silently (cart recalculation); populate it only when the customer should see an explanation.
4. Prefer early-return `result.IsValid = false` with no error for cases where the customer simply doesn't meet the condition yet (e.g., not enough spend).

## Key Contracts

### IDiscountProvider
```csharp
IList<IDiscountRule> GetRequirementRules();
// + all IProvider members: ConfigurationUrl, SystemName, FriendlyName, Priority, LimitedToStores, LimitedToGroups
```

### IDiscountRule
```csharp
string SystemName   { get; }
string FriendlyName { get; }
Task<DiscountRuleValidationResult> CheckRequirement(DiscountRuleValidationRequest request);
string GetConfigurationUrl(string discountId, string discountRequirementId);
```

### DiscountRuleValidationResult
```csharp
public bool   IsValid    { get; set; }
public string UserError  { get; set; }   // shown to customer; empty = silent fail
```

### DiscountRule entity (key fields used by rules)
```csharp
public string DiscountRequirementRuleSystemName { get; set; }  // matches IDiscountRule.SystemName
public string Metadata                          { get; set; }  // rule-specific config (group ID, amount, etc.)
```

### Discount entity (key fields relevant to rules)
```csharp
public bool   RequiresCouponCode  { get; set; }
public bool   IsCumulative        { get; set; }
public int    DiscountLimitationId { get; set; }
public ICollection<DiscountRule> DiscountRules { get; set; }
```

## File Locations

| Concern | Path |
|---|---|
| IDiscountProvider | `src/Business/Grand.Business.Core/Interfaces/Catalog/Discounts/IDiscountProvider.cs` |
| IDiscountRule | `src/Business/Grand.Business.Core/Interfaces/Catalog/Discounts/IDiscountRule.cs` |
| DiscountRuleValidationRequest | `src/Business/Grand.Business.Core/Utilities/Catalog/DiscountRuleValidationRequest.cs` |
| DiscountRuleValidationResult | `src/Business/Grand.Business.Core/Utilities/Catalog/DiscountRuleValidationResult.cs` |
| Discount entity | `src/Core/Grand.Domain/Discounts/Discount.cs` |
| DiscountRule entity | `src/Core/Grand.Domain/Discounts/DiscountRule.cs` |
| IDiscountService | `src/Business/Grand.Business.Core/Interfaces/Catalog/Discounts/IDiscountService.cs` |
| Example — multiple rules | `src/Plugins/DiscountRules.Standard/` |

## Validation Checklist
- [ ] `IDiscountProvider` and `IProvider` members all implemented.
- [ ] `GetRequirementRules()` returns all rule instances.
- [ ] Each `IDiscountRule` has a globally unique `SystemName`.
- [ ] `CheckRequirement` is side-effect free and reads config from `request.DiscountRule.Metadata`.
- [ ] `GetConfigurationUrl` returns a URL matching the configuration controller route, or `""` for rules with no config.
- [ ] Each rule class registered in DI individually via `AddScoped<YourRule>()`.
- [ ] Provider registered with `AddScoped<IDiscountProvider, ...>`.
- [ ] Output path set to `Grand.Web/Plugins/{SystemName}/`.
- [ ] `Install` registers resource keys; `Uninstall` removes them.

## Examples

### Example 1: Customer Group Rule
`CheckRequirement` reads `request.DiscountRule.Metadata` as a customer group ID and checks `request.Customer.Groups.Contains(groupId)`. Configuration controller shows a dropdown of customer groups and saves the selected ID to `DiscountRule.Metadata`.

### Example 2: Minimum Spend Rule
`CheckRequirement` reads a minimum amount from `Metadata`, retrieves the customer's order history total via `IOrderService`, and returns `IsValid = true` when the total meets or exceeds the threshold.

### Example 3: Has Specific Product in Cart Rule
`CheckRequirement` reads a product ID from `Metadata`, iterates the customer's current cart items (accessible via cart session or passed context), and returns `IsValid = true` when the product is found.

### Example 4: Multiple Rules in One Plugin
Pattern from `DiscountRules.Standard`: define `CustomerGroupDiscountRule`, `HadSpentAmountDiscountRule`, `HasAllProductsDiscountRule`, `HasOneProductDiscountRule`, `ShoppingCartDiscountRule` as separate classes. Register each with `AddScoped<T>()`. Inject all into `DiscountProvider` constructor and return them from `GetRequirementRules()`.
