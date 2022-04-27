using Grand.Business.Core.Interfaces.System.Admin;
using Grand.Business.Core.Utilities.System;
using Grand.Infrastructure.Caching;
using Grand.Domain.Admin;
using Grand.Domain.Data;

namespace Grand.Business.System.Services.Admin
{
    public class AdminSiteMapService : IAdminSiteMapService
    {
        private readonly IRepository<AdminSiteMap> _adminSiteMapRepository;
        private readonly ICacheBase _cacheBase;

        public AdminSiteMapService(
            IRepository<AdminSiteMap> adminSiteMapRepository,
            ICacheBase cacheBase)
        {
            _adminSiteMapRepository = adminSiteMapRepository;
            _cacheBase = cacheBase;
        }

        public virtual async Task<IList<AdminSiteMap>> GetSiteMap()
        {
            return await _cacheBase.GetAsync($"ADMIN_SITEMAP", async () =>
            {
                var query = from c in _adminSiteMapRepository.Table
                            orderby c.DisplayOrder ascending
                            select c;

                var list = query.ToList();
                if (list.Any())
                    return list;
                else
                    return await Task.FromResult(StandardAdminSiteMap.SiteMap.OrderBy(x=>x.DisplayOrder).ToList());
            });
        }
    }
}
