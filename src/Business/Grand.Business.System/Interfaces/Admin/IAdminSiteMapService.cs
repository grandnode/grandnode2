using Grand.Domain.Admin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Interfaces.Admin
{
    public interface IAdminSiteMapService
    {
        Task<IList<AdminSiteMap>> GetSiteMap();
    }
}
