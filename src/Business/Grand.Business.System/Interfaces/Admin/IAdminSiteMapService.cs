using Grand.Domain.Admin;

namespace Grand.Business.System.Interfaces.Admin
{
    public interface IAdminSiteMapService
    {
        Task<IList<AdminSiteMap>> GetSiteMap();
    }
}
