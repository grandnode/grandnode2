using Grand.Business.Core.Interfaces.System.Admin;
using Grand.Data;
using Grand.Domain.Admin;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.System.Services.Admin;

public class AdminSiteMapService : IAdminSiteMapService
{
    private readonly IRepository<AdminSiteMap> _adminSiteMapRepository;
    private readonly ICacheBase _cacheBase;
    private readonly IMediator _mediator;

    public AdminSiteMapService(
        IRepository<AdminSiteMap> adminSiteMapRepository,
        ICacheBase cacheBase,
        IMediator mediator)
    {
        _adminSiteMapRepository = adminSiteMapRepository;
        _cacheBase = cacheBase;
        _mediator = mediator;
    }

    public virtual async Task<IList<AdminSiteMap>> GetSiteMap()
    {
        return await _cacheBase.GetAsync(CacheKey.ADMIN_SITEMAP_KEY, () =>
        {
            var query = from c in _adminSiteMapRepository.Table
                orderby c.DisplayOrder
                select c;

            return Task.FromResult(query.ToList());
        });
    }

    public virtual async Task InsertSiteMap(AdminSiteMap entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _adminSiteMapRepository.InsertAsync(entity);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.ADMIN_SITEMAP_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(entity);
    }

    public virtual async Task UpdateSiteMap(AdminSiteMap entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _adminSiteMapRepository.UpdateAsync(entity);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.ADMIN_SITEMAP_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(entity);
    }

    public virtual async Task DeleteSiteMap(AdminSiteMap entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _adminSiteMapRepository.DeleteAsync(entity);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.ADMIN_SITEMAP_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(entity);
    }
}