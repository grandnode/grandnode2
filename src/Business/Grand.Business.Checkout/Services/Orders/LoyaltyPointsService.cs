using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     LoyaltyPoints service interface
/// </summary>
public class LoyaltyPointsService : ILoyaltyPointsService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="rphRepository">LoyaltyPointsHistory repository</param>
    /// <param name="loyaltyPointsSettings">Loyalty points settings</param>
    /// <param name="mediator">Mediator</param>
    /// <param name="resourceLock">Lock</param>
    public LoyaltyPointsService(IRepository<LoyaltyPointsHistory> rphRepository,
        LoyaltyPointsSettings loyaltyPointsSettings,
        IMediator mediator,
        IResourceLock resourceLock)
    {
        _rphRepository = rphRepository;
        _loyaltyPointsSettings = loyaltyPointsSettings;
        _mediator = mediator;
        _resourceLock = resourceLock;
    }

    #endregion

    #region Fields

    private readonly IRepository<LoyaltyPointsHistory> _rphRepository;
    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly IMediator _mediator;
    private readonly IResourceLock _resourceLock;

    #endregion

    #region Methods

    /// <summary>
    ///     Get loyalty points for customer
    /// </summary>
    /// <param name="customerId">Customer Id</param>
    /// <param name="storeId">Store ident</param>
    /// <returns>PointsBalance</returns>
    public virtual async Task<int> GetLoyaltyPointsBalance(string customerId, string storeId)
    {
        var query = from p in _rphRepository.Table
            select p;

        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(rph => rph.CustomerId == customerId);
        if (!_loyaltyPointsSettings.PointsAccumulatedForAllStores)
            query = query.Where(rph => rph.StoreId == storeId);
        //Id as tie-breaker - entries created within the same millisecond share CreatedOnUtc
        query = query.OrderByDescending(rph => rph.CreatedOnUtc).ThenByDescending(rph => rph.Id);

        var lastRph = await _rphRepository.FirstOrDefaultAsync(query);
        return lastRph?.PointsBalance ?? 0;
    }

    /// <summary>
    ///     Add loyalty points
    /// </summary>
    /// <param name="customerId">Customer Id</param>
    /// <param name="points">Points</param>
    /// <param name="storeId">Store ident</param>
    /// <param name="message">Message</param>
    /// <param name="usedWithOrderId">Used with OrderId</param>
    /// <param name="usedAmount">Used amount</param>
    /// <returns>LoyaltyPointsHistory</returns>
    public virtual async Task<LoyaltyPointsHistory> AddLoyaltyPointsHistory(string customerId, int points,
        string storeId, string message = "",
        string usedWithOrderId = "", double usedAmount = 0)
    {
        //each record carries the balance after it, read from the record before it
        await using var _ = await _resourceLock.Acquire([LockKey(customerId, storeId)]);

        return await InsertLoyaltyPointsHistory(customerId, points, storeId, message, usedWithOrderId, usedAmount);
    }

    public virtual async Task<LoyaltyPointsHistory> TryRedeemLoyaltyPoints(string customerId, int points,
        string storeId, string message, string usedWithOrderId, double usedAmount)
    {
        await using var _ = await _resourceLock.Acquire([LockKey(customerId, storeId)]);

        if (await GetLoyaltyPointsBalance(customerId, storeId) < points)
            return null;

        return await InsertLoyaltyPointsHistory(customerId, -points, storeId, message, usedWithOrderId, usedAmount);
    }

    public virtual async Task<IList<LoyaltyPointsHistory>> GetLoyaltyPointsHistory(string customerId = "",
        string storeId = "", bool showAll = false)
    {
        var query = from p in _rphRepository.Table
            select p;

        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(rph => rph.CustomerId == customerId);
        if (!showAll && !_loyaltyPointsSettings.PointsAccumulatedForAllStores)
            //filter by store
            if (!string.IsNullOrEmpty(storeId))
                query = query.Where(rph => rph.StoreId == storeId);
        query = query.OrderByDescending(rph => rph.CreatedOnUtc).ThenByDescending(rph => rph.Id);

        return await _rphRepository.ToListAsync(query);
    }

    #endregion

    #region Utilities

    /// <summary>
    ///     Inserts a record; the caller holds the balance's lock
    /// </summary>
    private async Task<LoyaltyPointsHistory> InsertLoyaltyPointsHistory(string customerId, int points,
        string storeId, string message, string usedWithOrderId, double usedAmount)
    {
        var loyaltyPointsHistory = new LoyaltyPointsHistory {
            CustomerId = customerId,
            UsedWithOrderId = usedWithOrderId,
            Points = points,
            PointsBalance = await GetLoyaltyPointsBalance(customerId, storeId) + points,
            UsedAmount = usedAmount,
            Message = message,
            StoreId = storeId
        };
        await _rphRepository.InsertAsync(loyaltyPointsHistory);

        //event notification
        await _mediator.EntityInserted(loyaltyPointsHistory);

        return loyaltyPointsHistory;
    }

    /// <summary>
    ///     One key per balance: shared by all stores, or one per store
    /// </summary>
    private string LockKey(string customerId, string storeId)
    {
        return _loyaltyPointsSettings.PointsAccumulatedForAllStores
            ? $"LoyaltyPoints:{customerId}"
            : $"LoyaltyPoints:{customerId}:{storeId}";
    }

    #endregion
}