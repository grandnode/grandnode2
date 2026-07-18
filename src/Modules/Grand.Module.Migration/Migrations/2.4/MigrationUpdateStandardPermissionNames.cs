using System.Reflection;
using Grand.Data;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Unifies the wording of standard permission display names in already-installed databases.
///     Two problems existed side by side:
///     1) a handful of names had inconsistent casing/singular-plural wording (e.g. "Manage Order status",
///     "Manage Contact Attribute" vs. "Manage Address Attributes") - now corrected on <see cref="StandardPermission" />.
///     2) about 40 permissions additionally had a duplicate translation resource row with an
///     "{Area}. " prefix (e.g. "Admin area. Manage Brands") left over from an old resource export. The
///     Area field on TranslationResource is never actually used to filter which row is loaded, so whichever
///     row was inserted first simply won - meaning some permissions showed the prefixed text and others
///     (with no duplicate) didn't, for no functional reason. This drops the prefix everywhere so every
///     permission consistently shows its plain name.
/// </summary>
public class MigrationUpdateStandardPermissionNames : IMigration
{
    public int Priority => 2;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("DAEE04E0-573A-4001-9613-E682BE45BCE3");
    public string Name => "Unify wording of standard permission names and drop leftover 'Area. ' prefixes";

    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var permissionRepository = serviceProvider.GetRequiredService<IRepository<Permission>>();
        var translationRepository = serviceProvider.GetRequiredService<IRepository<TranslationResource>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationUpdateStandardPermissionNames>>();

        var standardPermissions = typeof(StandardPermission)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Permission))
            .Select(f => (Permission)f.GetValue(null)!);

        foreach (var standard in standardPermissions)
            try
            {
                var permission = permissionRepository.Table.FirstOrDefault(x => x.SystemName == standard.SystemName);
                if (permission != null && permission.Name != standard.Name)
                {
                    permission.Name = standard.Name;
                    permissionRepository.Update(permission);
                }

                //the display text is also cached as translation resources, under a couple of historical
                //key/casing variants (and sometimes duplicated with an "{Area}. " prefix) - normalize every
                //variant we can find to the plain name
                var candidateKeys = new[] {
                    $"Permission.{standard.SystemName}",
                    $"permission.{standard.SystemName}",
                    $"permission.{standard.SystemName}".ToLowerInvariant()
                };

                var translations = translationRepository.Table
                    .Where(x => candidateKeys.Contains(x.Name))
                    .ToList();

                foreach (var translation in translations.Where(x => x.Value != standard.Name))
                {
                    translation.Value = standard.Name;
                    translationRepository.Update(translation);
                }
            }
            catch (InvalidOperationException ex)
            {
                logService.LogError(ex, "UpgradeProcess - MigrationUpdateStandardPermissionNames ({SystemName})",
                    standard.SystemName);
            }
            catch (SystemException ex)
            {
                logService.LogError(ex, "UpgradeProcess - MigrationUpdateStandardPermissionNames ({SystemName})",
                    standard.SystemName);
            }

        return true;
    }
}
