using Grand.Data;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Fixes inconsistent wording of a set of standard permission names (mixed casing, singular/plural
///     mismatches, leftover typos) that were baked into already-installed databases at install time.
///     New installations already get the corrected wording from <see cref="StandardPermission" />.
/// </summary>
public class MigrationUpdateStandardPermissionNames : IMigration
{
    //permissions whose display Name was inconsistent; the corrected Name/Area now live on the
    //StandardPermission constants themselves, so we reuse them here instead of duplicating strings
    private static readonly Permission[] RenamedPermissions = [
        StandardPermission.ManageAccessAdminPanel,
        StandardPermission.ManageAccessVendorPanel,
        StandardPermission.ManageAccessStoreManagerPanel,
        StandardPermission.ManageOrderStatus,
        StandardPermission.ManageGiftVouchers,
        StandardPermission.ManagePaymentTransactions,
        StandardPermission.ManageContactAttribute,
        StandardPermission.ManageMessageContactForm,
        StandardPermission.HtmlEditorManagePictures,
        StandardPermission.ManagePushEvents,
        StandardPermission.EnableShoppingCart,
        StandardPermission.EnableWishlist,
        StandardPermission.PublicStoreAllowNavigation,
        StandardPermission.AccessClosedStore,
        StandardPermission.AllowUseApi
    ];

    public int Priority => 2;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("DAEE04E0-573A-4001-9613-E682BE45BCE3");
    public string Name => "Unify wording of standard permission names (casing, plurals, typos)";

    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var permissionRepository = serviceProvider.GetRequiredService<IRepository<Permission>>();
        var translationRepository = serviceProvider.GetRequiredService<IRepository<TranslationResource>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationUpdateStandardPermissionNames>>();

        foreach (var renamed in RenamedPermissions)
            try
            {
                var permission = permissionRepository.Table.FirstOrDefault(x => x.SystemName == renamed.SystemName);
                if (permission != null && permission.Name != renamed.Name)
                {
                    permission.Name = renamed.Name;
                    permissionRepository.Update(permission);
                }

                //the display text is also cached as a translation resource, under a couple of historical
                //key/casing variants - update every variant we can find so the corrected text actually shows up
                var candidateKeys = new[] {
                    $"Permission.{renamed.SystemName}",
                    $"permission.{renamed.SystemName}",
                    $"permission.{renamed.SystemName}".ToLowerInvariant()
                };

                var translations = translationRepository.Table
                    .Where(x => candidateKeys.Contains(x.Name))
                    .ToList();

                var prefix = $"{renamed.Area}. ";
                foreach (var translation in translations)
                {
                    //some rows carried an "{Area}. " prefix for the grouped ACL view - keep that prefix
                    //where it was already there, drop it otherwise, instead of guessing per permission
                    var newValue = translation.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        ? prefix + renamed.Name
                        : renamed.Name;

                    if (translation.Value == newValue) continue;

                    translation.Value = newValue;
                    translationRepository.Update(translation);
                }
            }
            catch (Exception ex)
            {
                logService.LogError(ex, "UpgradeProcess - MigrationUpdateStandardPermissionNames ({SystemName})",
                    renamed.SystemName);
            }

        return true;
    }
}
