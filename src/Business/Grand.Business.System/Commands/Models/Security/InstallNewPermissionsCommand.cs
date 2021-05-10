using Grand.Business.Common.Interfaces.Security;
using MediatR;

namespace Grand.Business.System.Commands.Models.Security
{
    public class InstallNewPermissionsCommand : IRequest<bool>
    {
        public IPermissionProvider PermissionProvider { get; set; }
    }
}
