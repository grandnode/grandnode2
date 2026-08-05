# Checklist: Plugin and Theme Release

Run before shipping a new plugin or theme, or a version of one.

Domain rules live in the per-kind skills; this is the packaging and lifecycle gate.

---

## Identity

- [ ] `SystemName` identical in `Manifest.cs`, `{Feature}Defaults`, the provider, and the output folder name.
- [ ] `SystemName` follows `{Group}.{Name}`, and `Group` is an existing group value.
- [ ] `SystemName` unchanged from the previously shipped version — it is the persisted identity.
- [ ] `Version` in the manifest bumped.
- [ ] `FriendlyName` in `{Feature}Defaults` is a **resource key**, and the provider resolves it through `ITranslationService`.

## Project

- [ ] Imports `..\..\Build\Grand.Common.props`.
- [ ] Correct SDK: `Microsoft.NET.Sdk.Razor` with `AddRazorSupportForMvc=true` and `StaticWebAssetsEnabled=false` if it has views; `Microsoft.NET.Sdk` otherwise.
- [ ] Output path set for **both** Debug and Release, to `Grand.Web/Plugins/{SystemName}/`.
- [ ] All GrandNode project references `<Private>false</Private>`, with `ExcludeAssets` matching the nearest comparable plugin.
- [ ] Package references carry no inline version.
- [ ] Added to `GrandNode.sln`.
- [ ] `logo.jpg` present and copied to output.
- [ ] Themes only: `Content/theme.jpg` present, and `Content/**` copied with `PreserveNewest`.

## Registration

- [ ] `IStartupApplication` registers the provider(s) with the right lifetime.
- [ ] `Priority` matches the convention for comparable plugins.
- [ ] `Configure` is empty unless the plugin owns middleware or endpoints.
- [ ] Nothing was added to `Program.cs` or to a core project on the plugin's behalf.

## Install and uninstall

- [ ] `Install()` saves default settings and adds every resource key, then calls `base.Install()` **last**.
- [ ] `Uninstall()` deletes settings and removes **every** key `Install()` added, then calls `base.Uninstall()` last.
- [ ] The two lists were diffed against each other, key by key.
- [ ] Install and uninstall touch nothing outside the plugin's own settings and resources.
- [ ] Install → uninstall → install leaves the store in its original state.

## Configuration screen

- [ ] `ConfigurationUrl` matches the admin controller's actual route.
- [ ] `[AuthorizeAdmin]`, `[Area("Admin")]`, `[PermissionAuthorize(...)]` all present.
- [ ] Store scope from `IAdminStoreService.GetActiveStore()` used on both GET and POST.
- [ ] `LoadSetting` before mutation, so untouched fields are not reset.
- [ ] `ClearCache()` after save.
- [ ] Invalid model state re-renders instead of saving.
- [ ] `_ViewImports.cshtml` and `_ViewStart.cshtml` present under the plugin's view folders.

## Behavior

- [ ] `LimitedToStores` and `LimitedToGroups` behave as documented.
- [ ] `Priority` is driven by a `DisplayOrder` setting the operator controls.
- [ ] Themes only: `GetViewLocations()` ends with the two default fallbacks, and `ThemeName` matches the `Views/` folder.
- [ ] Widgets only: zone names match the strings actually used in the target views.
- [ ] Consent gating present where the plugin injects third-party scripts.

## Verification in a running store

- [ ] Build output lands in `Grand.Web/Plugins/{SystemName}/` for Release as well as Debug.
- [ ] Plugin appears in the admin plugin list with its logo and friendly name.
- [ ] Install succeeds on a store that has existing data.
- [ ] Configuration screen loads, saves, and the saved value takes effect on the storefront.
- [ ] The feature works on a second store with different settings.
- [ ] Uninstall succeeds and leaves no orphaned settings or resources.

## Documentation

- [ ] PR states what the plugin does, which providers it registers, and which settings it adds.
- [ ] Breaking changes stated truthfully — including any renamed system name or removed setting.
- [ ] Themes only: the list of copied views and the upstream revision they were copied at.
