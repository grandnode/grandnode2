using Grand.Business.Common.Interfaces.Security;
using MediatR;

namespace Grand.Business.System.Commands.Models.Security
{
    public class InstallPermissionsCommand : IRequest<bool>
    {
        public IPermissionProvider PermissionProvider { get; set; }
    }
}
