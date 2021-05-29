using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.System.Commands.Models.Security;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Security
{
    public class InstallPermissionsCommandHandler : IRequestHandler<InstallPermissionsCommand, bool>
    {
        private readonly IPermissionService _permissionService;
        private readonly IGroupService _groupService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;

        public InstallPermissionsCommandHandler(
            IPermissionService permissionService,
            IGroupService groupService,
            ITranslationService translationService,
            ILanguageService languageService)
        {
            _permissionService = permissionService;
            _groupService = groupService;
            _translationService = translationService;
            _languageService = languageService;
        }

        public async Task<bool> Handle(InstallPermissionsCommand request, CancellationToken cancellationToken)
        {
            //install new permissions
            var permissions = request.PermissionProvider.GetPermissions();
            foreach (var permission in permissions)
            {
                var permission1 = await _permissionService.GetPermissionBySystemName(permission.SystemName);
                if (permission1 == null)
                {
                    //new permission (install it)
                    permission1 = new Permission {
                        Name = permission.Name,
                        SystemName = permission.SystemName,
                        Area = permission.Area,
                        Category = permission.Category,
                        Actions = permission.Actions
                    };


                    //default customer group mappings
                    var defaultPermissions = request.PermissionProvider.GetDefaultPermissions();
                    foreach (var defaultPermission in defaultPermissions)
                    {
                        var customerGroup = await _groupService.GetCustomerGroupBySystemName(defaultPermission.CustomerGroupSystemName);
                        if (customerGroup == null)
                        {
                            //new role (save it)
                            customerGroup = new CustomerGroup {
                                Name = defaultPermission.CustomerGroupSystemName,
                                Active = true,
                                SystemName = defaultPermission.CustomerGroupSystemName
                            };
                            await _groupService.InsertCustomerGroup(customerGroup);
                        }


                        var defaultMappingProvided = (from p in defaultPermission.Permissions
                                                      where p.SystemName == permission1.SystemName
                                                      select p).Any();
                        if (defaultMappingProvided)
                        {
                            permission1.CustomerGroups.Add(customerGroup.Id);
                        }
                    }

                    //save new permission
                    await _permissionService.InsertPermission(permission1);

                    //save localization
                    await permission1.SaveTranslationPermissionName(_translationService, _languageService);
                }
            }
            return true;
        }
    }
}
