using Grand.Business.Core.Interfaces.Common.Security;
using MediatR;

namespace Grand.Business.Core.Commands.System.Security
{
    public class UninstallPermissionsCommand : IRequest<bool>
    {
        public IPermissionProvider PermissionProvider { get; set; }
    }
}
