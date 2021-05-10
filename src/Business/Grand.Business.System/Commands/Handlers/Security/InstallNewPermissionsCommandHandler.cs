using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.System.Commands.Models.Security;
using Grand.Domain.Permissions;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Security
{
    public class InstallNewPermissionsCommandHandler : IRequestHandler<InstallNewPermissionsCommand, bool>
    {
        private readonly IPermissionService _permissionService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;

        public InstallNewPermissionsCommandHandler(
            IPermissionService permissionService,
            ITranslationService translationService,
            ILanguageService languageService)
        {
            _permissionService = permissionService;
            _translationService = translationService;
            _languageService = languageService;
        }

        public async Task<bool> Handle(InstallNewPermissionsCommand request, CancellationToken cancellationToken)
        {
            //install new permissions
            var permissions = request.PermissionProvider.GetPermissions();
            foreach (var permission in permissions)
            {
                var permission1 = await _permissionService.GetPermissionBySystemName(permission.SystemName);
                if (permission1 == null)
                {
                    //new permission (install it)
                    permission1 = new Permission
                    {
                        Name = permission.Name,
                        SystemName = permission.SystemName,
                        Area = permission.Area,
                        Category = permission.Category,
                        Actions = permission.Actions
                    };

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
