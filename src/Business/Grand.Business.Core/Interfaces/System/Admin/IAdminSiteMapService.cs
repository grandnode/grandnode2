using Grand.Domain.Admin;

namespace Grand.Business.Core.Interfaces.System.Admin
{
    public interface IAdminSiteMapService
    {
        Task<IList<AdminSiteMap>> GetSiteMap();
    }
}
