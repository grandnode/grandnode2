using Grand.Business.System.Interfaces.Admin;
using Grand.Business.System.Utilities;
using Grand.Infrastructure.Caching;
using Grand.Domain.Admin;
using Grand.Domain.Data;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                            select c;

                var list = await query.ToListAsync();
                if (list.Any())
                    return list;
                else
                    return StandardAdminSiteMap.SiteMap;
            });
        }
    }
}
