using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Affiliates;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Customers.Services
{
    /// <summary>
    /// Affiliate service
    /// </summary>
    public class AffiliateService : IAffiliateService
    {
        #region Fields

        private readonly IRepository<Affiliate> _affiliateRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="affiliateRepository">Affiliate repository</param>
        /// <param name="orderRepository">Order repository</param>
        /// <param name="mediator">Mediator</param>
        public AffiliateService(IRepository<Affiliate> affiliateRepository,
            IRepository<Order> orderRepository,
            IMediator mediator)
        {
            _affiliateRepository = affiliateRepository;
            _orderRepository = orderRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets an affiliate by identifier
        /// </summary>
        /// <param name="affiliateId">Affiliate identifier</param>
        /// <returns>Affiliate</returns>
        public virtual Task<Affiliate> GetAffiliateById(string affiliateId)
        {
            return _affiliateRepository.GetByIdAsync(affiliateId);
        }

        /// <summary>
        /// Gets an affiliate by friendly url name
        /// </summary>
        /// <param name="friendlyUrlName">Friendly url name</param>
        /// <returns>Affiliate</returns>
        public virtual async Task<Affiliate> GetAffiliateByFriendlyUrlName(string friendlyUrlName)
        {
            var query = from a in _affiliateRepository.Table
                        where a.FriendlyUrlName!=null && a.FriendlyUrlName.Contains(friendlyUrlName.ToLowerInvariant())
                        select a;
            var affiliate = await Task.FromResult(query.FirstOrDefault());
            return affiliate;
        }

        /// <summary>
        /// Gets all affiliates
        /// </summary>
        /// <returns>Affiliates</returns>
        public virtual async Task<IPagedList<Affiliate>> GetAllAffiliates(string friendlyUrlName = null,
            string firstName = null, string lastName = null,
            bool loadOnlyWithOrders = false,
            DateTime? ordersCreatedFromUtc = null, DateTime? ordersCreatedToUtc = null,
            int pageIndex = 0, int pageSize = int.MaxValue,
            bool showHidden = false)
        {
            var query = from p in _affiliateRepository.Table
                        select p;

            if (!string.IsNullOrWhiteSpace(friendlyUrlName))
                query = query.Where(a => a.FriendlyUrlName != null && a.FriendlyUrlName.Contains(friendlyUrlName.ToLowerInvariant()));
            if (!string.IsNullOrWhiteSpace(firstName))
                query = query.Where(a => a.Address.FirstName != null && a.Address.FirstName.ToLower().Contains(firstName.ToLower()));
            if (!string.IsNullOrWhiteSpace(lastName))
                query = query.Where(a => a.Address.LastName != null && a.Address.LastName.ToLower().Contains(lastName.ToLower()));
            if (!showHidden)
                query = query.Where(a => a.Active);

            if (loadOnlyWithOrders)
            {
                var ordersQuery = from p in _orderRepository.Table
                                  select p;

                if (ordersCreatedFromUtc.HasValue)
                    ordersQuery = ordersQuery.Where(o => ordersCreatedFromUtc.Value <= o.CreatedOnUtc);
                if (ordersCreatedToUtc.HasValue)
                    ordersQuery = ordersQuery.Where(o => ordersCreatedToUtc.Value >= o.CreatedOnUtc);
                ordersQuery = ordersQuery.Where(o => !o.Deleted);
                var affOrder = ordersQuery.Select(x => x.AffiliateId).ToList();
                query = query.Where(x => affOrder.Contains(x.Id));
            }

            query = query.OrderByDescending(a => a.Id);

            return await PagedList<Affiliate>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Inserts an affiliate
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        public virtual async Task InsertAffiliate(Affiliate affiliate)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            await _affiliateRepository.InsertAsync(affiliate);

            //event notification
            await _mediator.EntityInserted(affiliate);
        }

        /// <summary>
        /// Updates the affiliate
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        public virtual async Task UpdateAffiliate(Affiliate affiliate)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            await _affiliateRepository.UpdateAsync(affiliate);

            //event notification
            await _mediator.EntityUpdated(affiliate);
        }

        /// <summary>
        /// Marks affiliate as deleted 
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        public virtual async Task DeleteAffiliate(Affiliate affiliate)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            await _affiliateRepository.DeleteAsync(affiliate);

            //event notification
            await _mediator.EntityDeleted(affiliate);
        }

        #endregion
    }
}