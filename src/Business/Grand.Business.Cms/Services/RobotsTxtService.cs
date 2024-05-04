using Grand.Business.Core.Interfaces.Cms;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Cms.Services;

public class RobotsTxtService : IRobotsTxtService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="robotsRepository">Robots repository</param>
    /// <param name="mediator">Mediator</param>
    /// <param name="cacheBase">Cache manager</param>
    public RobotsTxtService(
        IRepository<RobotsTxt> robotsRepository,
        IMediator mediator,
        ICacheBase cacheBase)
    {
        _robotsRepository = robotsRepository;
        _mediator = mediator;
        _cacheBase = cacheBase;
    }

    #endregion


    /// <summary>
    ///     get robotsTXT by store ident
    /// </summary>
    /// <param name="storeId"></param>
    /// <returns></returns>
    public virtual async Task<RobotsTxt> GetRobotsTxt(string storeId = "")
    {
        var key = string.Format(CacheKey.ROBOTS_BY_STORE, storeId);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var robotsTxt = await _robotsRepository.GetOneAsync(x => x.StoreId == storeId);
            return robotsTxt;
        });
    }

    /// <summary>
    ///     Insert a robotsTxt
    /// </summary>
    /// <param name="robotsTxt">robotsTxt</param>
    public virtual async Task InsertRobotsTxt(RobotsTxt robotsTxt)
    {
        ArgumentNullException.ThrowIfNull(robotsTxt);

        await _robotsRepository.InsertAsync(robotsTxt);

        await _cacheBase.RemoveByPrefix(CacheKey.ROBOTS_ALL_KEY);

        //event notification
        await _mediator.EntityInserted(robotsTxt);
    }

    /// <summary>
    ///     Update a robotsTxt
    /// </summary>
    /// <param name="robotsTxt">robotsTxt</param>
    public virtual async Task UpdateRobotsTxt(RobotsTxt robotsTxt)
    {
        ArgumentNullException.ThrowIfNull(robotsTxt);

        await _robotsRepository.UpdateAsync(robotsTxt);

        await _cacheBase.RemoveByPrefix(CacheKey.ROBOTS_ALL_KEY);

        //event notification
        await _mediator.EntityUpdated(robotsTxt);
    }

    /// <summary>
    ///     Delete a robotsTxt
    /// </summary>
    /// <param name="robotsTxt">robotsTxt</param>
    public virtual async Task DeleteRobotsTxt(RobotsTxt robotsTxt)
    {
        ArgumentNullException.ThrowIfNull(robotsTxt);

        await _robotsRepository.DeleteAsync(robotsTxt);

        await _cacheBase.RemoveByPrefix(CacheKey.ROBOTS_ALL_KEY);

        //event notification
        await _mediator.EntityDeleted(robotsTxt);
    }

    #region Fields

    private readonly IRepository<RobotsTxt> _robotsRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;

    #endregion
}