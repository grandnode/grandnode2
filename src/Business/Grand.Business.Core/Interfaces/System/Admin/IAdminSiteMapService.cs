using Grand.Domain.Admin;

namespace Grand.Business.Core.Interfaces.System.Admin;

public interface IAdminSiteMapService
{
    Task<IList<AdminSiteMap>> GetSiteMap();
    Task InsertSiteMap(AdminSiteMap entity);
    Task UpdateSiteMap(AdminSiteMap entity);
    Task DeleteSiteMap(AdminSiteMap entity);
}