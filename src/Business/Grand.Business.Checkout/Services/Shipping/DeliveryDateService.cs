using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Data;
using Grand.Domain.Shipping;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.Shipping
{
    public class DeliveryDateService : IDeliveryDateService
    {
        #region Fields

        private readonly IRepository<DeliveryDate> _deliveryDateRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public DeliveryDateService(
            IRepository<DeliveryDate> deliveryDateRepository,
            IMediator mediator,
            ICacheBase cacheBase)
        {
            _deliveryDateRepository = deliveryDateRepository;
            _mediator = mediator;
            _cacheBase = cacheBase;
        }

        #endregion

        #region Delivery dates

        /// <summary>
        /// Gets a delivery date
        /// </summary>
        /// <param name="deliveryDateId">The delivery date identifier</param>
        /// <returns>Delivery date</returns>
        public virtual Task<DeliveryDate> GetDeliveryDateById(string deliveryDateId)
        {
            string key = string.Format(CacheKey.DELIVERYDATE_BY_ID_KEY, deliveryDateId);
            return _cacheBase.GetAsync(key, () => _deliveryDateRepository.GetByIdAsync(deliveryDateId));
        }

        /// <summary>
        /// Gets all delivery dates
        /// </summary>
        /// <returns>Delivery dates</returns>
        public virtual async Task<IList<DeliveryDate>> GetAllDeliveryDates()
        {
            return await _cacheBase.GetAsync(CacheKey.DELIVERYDATE_ALL, async () =>
            {
                var query = from dd in _deliveryDateRepository.Table
                            orderby dd.DisplayOrder
                            select dd;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Inserts a delivery date
        /// </summary>
        /// <param name="deliveryDate">Delivery date</param>
        public virtual async Task InsertDeliveryDate(DeliveryDate deliveryDate)
        {
            if (deliveryDate == null)
                throw new ArgumentNullException(nameof(deliveryDate));

            await _deliveryDateRepository.InsertAsync(deliveryDate);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(deliveryDate);
        }

        /// <summary>
        /// Updates the delivery date
        /// </summary>
        /// <param name="deliveryDate">Delivery date</param>
        public virtual async Task UpdateDeliveryDate(DeliveryDate deliveryDate)
        {
            if (deliveryDate == null)
                throw new ArgumentNullException(nameof(deliveryDate));

            await _deliveryDateRepository.UpdateAsync(deliveryDate);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(deliveryDate);
        }

        /// <summary>
        /// Deletes a delivery date
        /// </summary>
        /// <param name="deliveryDate">The delivery date</param>
        public virtual async Task DeleteDeliveryDate(DeliveryDate deliveryDate)
        {
            if (deliveryDate == null)
                throw new ArgumentNullException(nameof(deliveryDate));

            await _deliveryDateRepository.DeleteAsync(deliveryDate);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);

            //clear product cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(deliveryDate);
        }

        #endregion

    }
}
