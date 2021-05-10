using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Extensions;
using MediatR;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.Orders
{
    /// <summary>
    /// LoyaltyPoints service interface
    /// </summary>
    public partial class LoyaltyPointsService : ILoyaltyPointsService
    {
        #region Fields

        private readonly IRepository<LoyaltyPointsHistory> _rphRepository;
        private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="rphRepository">LoyaltyPointsHistory repository</param>
        /// <param name="loyaltyPointsSettings">Loyalty points settings</param>
        /// <param name="mediator">Mediator</param>
        public LoyaltyPointsService(IRepository<LoyaltyPointsHistory> rphRepository,
            LoyaltyPointsSettings loyaltyPointsSettings,
            IMediator mediator)
        {
            _rphRepository = rphRepository;
            _loyaltyPointsSettings = loyaltyPointsSettings;
            _mediator = mediator;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Get loyalty points for customer
        /// </summary>
        /// <param name="customerId">Customer Id</param>
        /// <returns>PointsBalance</returns>

        public virtual async Task<int> GetLoyaltyPointsBalance(string customerId, string storeId)
        {
            var query = _rphRepository.Table;
            if (!String.IsNullOrEmpty(customerId))
                query = query.Where(rph => rph.CustomerId == customerId);
            if (!_loyaltyPointsSettings.PointsAccumulatedForAllStores)
                query = query.Where(rph => rph.StoreId == storeId);
            query = query.OrderByDescending(rph => rph.CreatedOnUtc).ThenByDescending(rph => rph.Id);

            var lastRph = await query.FirstOrDefaultAsync();
            return lastRph != null ? lastRph.PointsBalance : 0;

        }

        /// <summary>
        /// Add loyalty points
        /// </summary>
        /// <param name="customerId">Customer Id</param>
        /// <param name="points">Points</param>
        /// <param name="message">Message</param>
        /// <param name="usedWithOrderId">Used with OrderId</param>
        /// <param name="usedAmount">Used amount</param>
        /// <returns>LoyaltyPointsHistory</returns>

        public virtual async Task<LoyaltyPointsHistory> AddLoyaltyPointsHistory(string customerId, int points, string storeId, string message = "",
           string usedWithOrderId = "", decimal usedAmount = 0M)
        {

            var loyaltyPointsHistory = new LoyaltyPointsHistory
            {
                CustomerId = customerId,
                UsedWithOrderId = usedWithOrderId,
                Points = points,
                PointsBalance = await GetLoyaltyPointsBalance(customerId, storeId) + points,
                UsedAmount = usedAmount,
                Message = message,
                StoreId = storeId,
                CreatedOnUtc = DateTime.UtcNow
            };
            await _rphRepository.InsertAsync(loyaltyPointsHistory);

            //event notification
            await _mediator.EntityInserted(loyaltyPointsHistory);

            return loyaltyPointsHistory;
        }

        public virtual async Task<IList<LoyaltyPointsHistory>> GetLoyaltyPointsHistory(string customerId = "", string storeId = "", bool showAll = false)
        {
            var query = _rphRepository.Table;
            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(rph => rph.CustomerId == customerId);
            if (!showAll && !_loyaltyPointsSettings.PointsAccumulatedForAllStores)
            {
                //filter by store
                if (!string.IsNullOrEmpty(storeId))
                    query = query.Where(rph => rph.StoreId == storeId);
            }
            query = query.OrderByDescending(rph => rph.CreatedOnUtc).ThenByDescending(rph => rph.Id);

            return await query.ToListAsync();
        }

        #endregion
    }
}
