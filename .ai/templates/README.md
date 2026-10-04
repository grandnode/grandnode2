# Templates

Copy-ready skeletons. Each file lists the artifacts for one kind of extension, in the order they should be created, with `{Placeholder}` tokens to substitute.

| Template | Use for |
|---|---|
| `plugin/base-plugin.md` | Any installable plugin — the files every plugin needs regardless of kind |
| `plugin/admin-configuration.md` | The admin configuration screen a configurable plugin adds |
| `theme/theme-plugin.md` | A storefront theme |
| `migration.md` | An upgrade migration |

## How to use

1. Read the skill for the extension kind first — the template is the shape, the skill is the contract.
2. Copy the artifacts, substituting placeholders consistently:

   | Placeholder | Meaning | Example |
   |---|---|---|
   | `{SystemName}` | plugin system name, `{Group}.{Name}` | `Payments.CashOnDelivery` |
   | `{Group}` | plugin group | `Payments` |
   | `{Name}` | plugin short name | `CashOnDelivery` |
   | `{Feature}` | type-name prefix | `CashOnDeliveryPayment` |
   | `{ThemeName}` | theme display name and view folder | `Modern` |

3. Diff your result against the closest existing plugin in `src/Plugins/`. The templates are distilled from those; where they disagree, the shipped plugin wins.
4. Run `.ai/prompts/review-change.md` on the result.

## Rules that apply to every template

- Package references carry no version — versions live in `Directory.Packages.props`.
- GrandNode project references are `<Private>false</Private>`.
- Output paths are set for **both** Debug and Release.
- `SystemName` is identical in `Manifest.cs`, `{Feature}Defaults`, and the output folder name.
- `logo.jpg` is required for the plugin to appear in the admin plugin list.
