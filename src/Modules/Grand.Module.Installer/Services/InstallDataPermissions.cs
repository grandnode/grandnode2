using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Module.Installer.Extensions;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallPermissions()
    {
        var permissions = PermissionExtensions.Permissions();
        foreach (var permission in permissions)
        {
            var permission1 = new Permission {
                Name = permission.Name,
                SystemName = permission.SystemName,
                Area = permission.Area,
                Category = permission.Category,
                Actions = permission.Actions
            };
            //default customer group mappings
            var defaultPermissions = PermissionExtensions.DefaultPermissions();

            foreach (var defaultPermission in defaultPermissions)
            {
                var customerGroup = _customerGroupRepository.Table.FirstOrDefault(x => x.SystemName == defaultPermission.CustomerGroupSystemName);
                var defaultMappingProvided = (from p in defaultPermission.Permissions
                                              where p.SystemName == permission1.SystemName
                                              select p).Any();

                if (defaultMappingProvided) permission1.CustomerGroups.Add(customerGroup!.Id);
            }
            //save new permission
            await _permissionRepository.InsertAsync(permission1);

            //save localization
            await SaveTranslationPermissionName(permission1);
        }
    }
    private async Task SaveTranslationPermissionName(Permission permissionRecord)
    {
        var name = $"Permission.{permissionRecord.SystemName}";
        var value = permissionRecord.Name;
        foreach (var lang in _languageRepository.Table.ToList())
        {
            var lsr = new TranslationResource {
                LanguageId = lang.Id,
                Name = name,
                Value = value
            };
            await _lsrRepository.InsertAsync(lsr);
        }
    }
}