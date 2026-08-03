# Settings and Localization

## Purpose
Create, modify, and review GrandNode settings classes, setting persistence, store-scoped overrides, localization resources, translation resource lifecycle, and localized domain and admin models.

## When To Use
Use this skill when adding or changing a settings class, loading or saving settings per store, reading or writing localization resources, adding translation keys for a plugin or module, seeding default settings or resources in the installer, building localized admin models, or working with `ITranslationService`, `ISettingService`, `IPluginTranslateResource`, or `ILanguageService`.

## When Not To Use
Do not use this skill for pure UI work without settings or localization changes; combine it with `template-creation` or `admin-area-changes` when UI is involved.

Do not use this skill as the primary review for MongoDB query safety or security; combine it with the relevant skill when those concerns apply.

## Inputs Required
- Repository root.
- Target feature, plugin, or module name.
- Whether settings are global or per-store.
- Which admin panels expose the settings (Admin, Store Owner, Vendor, plugin config page).
- Required localization resource keys and their text.
- Whether domain entities carry per-language values (`ITranslationEntity` / `Locales`).

## Instructions

### Mandatory Rules

#### Settings
1. Define core settings classes in `src/Core/Grand.Domain` and implement the marker interface `Grand.Domain.Configuration.ISettings` (plugin settings classes stay in the owning plugin project).
2. Keep settings as plain properties with no business logic. Store primitive types, enums, and simple value types.
3. Load settings with `ISettingService.LoadSetting<T>(storeId)`. Pass an empty string to load the global fallback; pass the active store ID for per-store overrides.
4. Obtain the active store ID from `IAdminStoreService.GetActiveStore()` in admin, store, and plugin controllers.
5. Save settings with `ISettingService.SaveSetting<T>(settings, storeId)`. Call `await settingService.ClearCache()` immediately after.
6. Delete all settings for a type with `ISettingService.DeleteSetting<T>()` in plugin `Uninstall`.
7. Seed default settings in `src/Modules/Grand.Module.Installer/Services/InstallDataSettings.cs` using `SaveSetting` without a store ID.
8. Map between settings and admin models using static extension methods in `src/Web/Grand.Web.AdminShared/Extensions/Mapping/Settings/`, following the `ToModel()` / `ToEntity(model, destination)` pattern backed by `MapTo`.
9. Register mapper profiles for settings models in `Grand.Web.AdminShared` when the model is shared across panels.

#### Localization
10. Identify resources by a lowercase dot-separated key, for example `plugins.payment.cashondelivery.additionalfee`.
11. Use `IPluginTranslateResource.AddOrUpdatePluginTranslateResource(name, value, area)` in plugin `Install` to register all resource keys.
12. Use `IPluginTranslateResource.DeletePluginTranslationResource(name)` in plugin `Uninstall` for every key added during install.
13. Assign the correct `TranslationResourceArea` enum value when adding resources:
    - `Common` (0) — shared across all areas.
    - `Admin` (1) — admin panel only.
    - `Front` (2) — public storefront.
    - `Plugin` (3) — plugin-owned resources.
    - `Vendor` (5) — vendor panel.
14. Seed default resources for core features in `src/Modules/Grand.Module.Installer/Services/InstallDataLocaleResources.cs` via `src/Web/Grand.Web/App_Data/Resources/DefaultLanguage.xml`.
15. Access resources in controllers and services by injecting `ITranslationService` and calling `GetResource(key)` or `GetResource(key, languageId, defaultValue)`.
16. Access resources in Razor views through the `Loc` indexer: `@Loc["key"]`. Do not call `ITranslationService` directly from views.
17. Translate enum values using `IEnumTranslationService` with keys formatted as `Enums.{TypeName}.{ValueName}`.

#### Localized Domain Entities
18. Implement `ITranslationEntity` on domain entities that require per-language field values. This adds a `IList<TranslationEntity> Locales` collection where each entry carries `LanguageId`, `LocaleKey`, and `LocaleValue`.
19. Implement `ILocalizedModel<TLocalizedModel>` on admin models for entities with localized fields. Each inner model must implement `ILocalizedModelLocal` (has a `LanguageId` property).

### Recommendations
1. Prefer `LoadSetting<T>(storeId)` over individual key lookups for settings objects with multiple properties.
2. Prefer a single `SaveSetting<T>` call that persists the whole settings object rather than saving individual keys.
3. Prefer store-scoped settings only when the feature must behave differently across stores.
4. Prefer adding all resource keys in a single `Install` method block so they are easy to match with `Uninstall` deletions.
5. Prefer using `Admin.Configuration.Updated` and `Admin.Plugins.Saved` as generic success messages rather than inventing new keys.
6. Prefer placing plugin-specific settings classes inside the plugin project, not in `Grand.Domain`.

## Constraints
- Never place a settings class outside `Grand.Domain` for core features (plugin settings are the exception and stay in the plugin project).
- Never load settings without passing a store ID when the feature supports per-store configuration.
- Never skip `ClearCache()` after saving settings.
- Never leave resource keys in `Install` that are not removed in `Uninstall`.
- Never hardcode display text in controllers or views when a resource key is available.
- Never call `ITranslationService` directly from Razor views; use `Loc[...]` instead.
- Never add `TranslationResource` documents directly to the database without going through `ITranslationService` or `IPluginTranslateResource`.

## Key Contracts

### ISettingService (`src/Business/Grand.Business.Core/Interfaces/Common/Configuration/ISettingService.cs`)
```csharp
Task<T>     LoadSetting<T>(string storeId = "") where T : ISettings, new();
Task        SaveSetting<T>(T settings, string storeId = "") where T : ISettings, new();
Task        DeleteSetting<T>() where T : ISettings, new();
Task        ClearCache();
Task<T>     GetSettingByKey<T>(string key, T defaultValue = default, string storeId = "");
Task        SetSetting<T>(string key, T value, string storeId = "");
IList<Setting> GetAllSettings();
```

### ITranslationService (`src/Business/Grand.Business.Core/Interfaces/Common/Localization/ITranslationService.cs`)
```csharp
string GetResource(string name);
string GetResource(string name, string languageId, string defaultValue = "", bool returnEmptyIfNotFound = false);
Task InsertTranslateResource(TranslationResource resource);
Task UpdateTranslateResource(TranslationResource resource);
Task DeleteTranslateResource(TranslationResource resource);
Task<TranslationResource> GetTranslateResourceByName(string name, string languageId);
```

### IPluginTranslateResource (`src/Business/Grand.Business.Core/Interfaces/Common/Localization/IPluginTranslateResource.cs`)
```csharp
Task AddOrUpdatePluginTranslateResource(string name, string value,
    TranslationResourceArea area = TranslationResourceArea.Common, string languageCulture = null);
Task DeletePluginTranslationResource(string name);
```

### IAdminStoreService (`src/Web/Grand.Web.Common/Helpers/AdminStoreService.cs`)
```csharp
Task<string> GetActiveStore();  // returns storeId or "" if single-store
```

## File Locations

| Concern | Path |
|---|---|
| Settings domain classes | `src/Core/Grand.Domain/{Area}/{Feature}Settings.cs` |
| ISettings marker | `src/Core/Grand.Domain/Configuration/ISettings.cs` |
| ISettingService interface | `src/Business/Grand.Business.Core/Interfaces/Common/Configuration/ISettingService.cs` |
| SettingService implementation | `src/Business/Grand.Business.Common/Services/Configuration/SettingService.cs` |
| Settings admin models (shared) | `src/Web/Grand.Web.AdminShared/Models/Settings/` |
| Settings mapping extensions | `src/Web/Grand.Web.AdminShared/Extensions/Mapping/Settings/` |
| ITranslationService interface | `src/Business/Grand.Business.Core/Interfaces/Common/Localization/ITranslationService.cs` |
| IPluginTranslateResource interface | `src/Business/Grand.Business.Core/Interfaces/Common/Localization/IPluginTranslateResource.cs` |
| ILanguageService interface | `src/Business/Grand.Business.Core/Interfaces/Common/Localization/ILanguageService.cs` |
| TranslationResource entity | `src/Core/Grand.Domain/Localization/TranslationResource.cs` |
| TranslationResourceArea enum | `src/Core/Grand.Domain/Localization/TranslationResourceArea.cs` |
| ITranslationEntity interface | `src/Core/Grand.Domain/Localization/ITranslationEntity.cs` |
| LocService (view localizer) | `src/Web/Grand.Web.Common/Localization/LocService.cs` |
| ILocalizedModel interface | `src/Web/Grand.Web.Common/Models/ILocalizedModel.cs` |
| Default resource XML | `src/Web/Grand.Web/App_Data/Resources/DefaultLanguage.xml` |
| Installer — settings seed | `src/Modules/Grand.Module.Installer/Services/InstallDataSettings.cs` |
| Installer — resource seed | `src/Modules/Grand.Module.Installer/Services/InstallDataLocaleResources.cs` |
| IAdminStoreService | `src/Web/Grand.Web.Common/Helpers/AdminStoreService.cs` |
| SettingService tests | `src/Tests/Grand.Business.Common.Tests/Services/Configuration/SettingServiceTests.cs` |
| TranslationService tests | `src/Tests/Grand.Business.Common.Tests/Services/Localization/TranslationServiceTests.cs` |

## Expected Output
Produce one of these results:
- A new or modified settings class, mapping extension, admin model, and controller flow.
- A new or modified set of localization resource keys with matching install and uninstall handling.
- A review report listing settings or localization issues by severity.

Include changed files, store-scope behavior, resource area classification, validation commands, and remaining risks.

## Validation Checklist
- [ ] Settings class implements `ISettings` and lives in the correct project.
- [ ] `LoadSetting` and `SaveSetting` pass the correct store ID.
- [ ] `ClearCache` is called after every `SaveSetting`.
- [ ] `DeleteSetting<T>` is called in plugin `Uninstall`.
- [ ] Every resource key added in `Install` is removed in `Uninstall`.
- [ ] Resource area is set correctly (`TranslationResourceArea`).
- [ ] Controllers use `ITranslationService.GetResource(key)` for display messages.
- [ ] Views use `@Loc["key"]` and do not call `ITranslationService` directly.
- [ ] Entities that need per-language values implement `ITranslationEntity`.
- [ ] Admin models that expose localized fields implement `ILocalizedModel<T>`.
- [ ] Settings mapping extensions follow the `ToModel()` / `ToEntity(model, destination)` pattern.
- [ ] Tests or build commands were run or reported as not run.

## Examples

### Example 1: Plugin Settings with Store Scope
Input: Add configurable additional fee to a payment plugin.

Output:
1. Add `AdditionalFee` property to `XPaymentSettings : ISettings` in the plugin project.
2. In `Configure` (GET): call `_adminStoreService.GetActiveStore()`, then `_settingService.LoadSetting<XPaymentSettings>(storeScope)`, map to model.
3. In `Configure` (POST): reload settings, update property, call `SaveSetting(settings, storeScope)`, then `ClearCache()`, then `Success(translationService.GetResource("Admin.Plugins.Saved"))`.
4. In `Install`: call `SaveSetting(new XPaymentSettings { AdditionalFee = 0 })` and `AddOrUpdatePluginTranslateResource("Plugins.Payment.X.AdditionalFee", "Additional fee", TranslationResourceArea.Plugin)`.
5. In `Uninstall`: call `DeleteSetting<XPaymentSettings>()` and `DeletePluginTranslationResource("Plugins.Payment.X.AdditionalFee")`.

### Example 2: Core Settings Field
Input: Add a new catalog setting that controls whether product reviews require approval.

Output:
1. Add `ProductReviewsMustBeApproved` property to `CatalogSettings` in `src/Core/Grand.Domain/Catalog/CatalogSettings.cs`.
2. Add matching property to `CatalogSettingsModel` in `src/Web/Grand.Web.AdminShared/Models/Settings/CatalogSettingsModel.cs`.
3. Update `CatalogSettingsMappingExtensions` to include the property in `ToModel()` and `ToEntity()`.
4. Update the Admin and Store Owner settings views and controllers to read and write the field.
5. Add a resource key `admin.configuration.settings.catalog.productreviewsmustbeapproved` to `DefaultLanguage.xml`.

### Example 3: Localized Domain Entity Field
Input: Add a per-language description to a custom entity.

Output:
1. Implement `ITranslationEntity` on the domain entity to get `IList<TranslationEntity> Locales`.
2. In the admin model, implement `ILocalizedModel<TLocalizedModel>` where the inner model has `LanguageId` and `Description`.
3. In the admin view, render a localization tab using the existing localized tab pattern.
4. In the controller save action, persist each locale entry back to `entity.Locales`.
