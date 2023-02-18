﻿using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Data;
using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Checkout.Services.Shipping
{
    public class PickupPointService : IPickupPointService
    {
        #region Fields

        private readonly IRepository<PickupPoint> _pickupPointsRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public PickupPointService(
            IRepository<PickupPoint> pickupPointsRepository,
            IMediator mediator,
            ICacheBase cacheBase)
        {
            _pickupPointsRepository = pickupPointsRepository;
            _mediator = mediator;
            _cacheBase = cacheBase;
        }

        #endregion

        #region Methods
        /// <summary>
        /// Gets a pickup point
        /// </summary>
        /// <param name="pickupPointId">The pickup point identifier</param>
        /// <returns>Delivery date</returns>
        public virtual Task<PickupPoint> GetPickupPointById(string pickupPointId)
        {
            var key = string.Format(CacheKey.PICKUPPOINTS_BY_ID_KEY, pickupPointId);
            return _cacheBase.GetAsync(key, () => _pickupPointsRepository.GetByIdAsync(pickupPointId));
        }

        /// <summary>
        /// Gets all pickup points
        /// </summary>
        /// <returns>Warehouses</returns>
        public virtual async Task<IList<PickupPoint>> GetAllPickupPoints()
        {
            return await _cacheBase.GetAsync(CacheKey.PICKUPPOINTS_ALL, async () =>
            {
                var query = from pp in _pickupPointsRepository.Table
                            orderby pp.DisplayOrder
                            select pp;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets all pickup points
        /// </summary>
        /// <returns>Warehouses</returns>
        public virtual async Task<IList<PickupPoint>> LoadActivePickupPoints(string storeId = "")
        {
            var pickupPoints = await GetAllPickupPoints();
            return pickupPoints.Where(pp => pp.StoreId == storeId || string.IsNullOrEmpty(pp.StoreId)).ToList();
        }


        /// <summary>
        /// Inserts a pickup point
        /// </summary>
        /// <param name="pickupPoint">Pickup Point</param>
        public virtual async Task InsertPickupPoint(PickupPoint pickupPoint)
        {
            if (pickupPoint == null)
                throw new ArgumentNullException(nameof(pickupPoint));

            await _pickupPointsRepository.InsertAsync(pickupPoint);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(pickupPoint);
        }

        /// <summary>
        /// Updates the pickupPoint
        /// </summary>
        /// <param name="pickupPoint">Pickup Point</param>
        public virtual async Task UpdatePickupPoint(PickupPoint pickupPoint)
        {
            if (pickupPoint == null)
                throw new ArgumentNullException(nameof(pickupPoint));

            await _pickupPointsRepository.UpdateAsync(pickupPoint);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(pickupPoint);
        }

        /// <summary>
        /// Deletes a pickup point
        /// </summary>
        /// <param name="pickupPoint">pickup point</param>
        public virtual async Task DeletePickupPoint(PickupPoint pickupPoint)
        {
            if (pickupPoint == null)
                throw new ArgumentNullException(nameof(pickupPoint));

            await _pickupPointsRepository.DeleteAsync(pickupPoint);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(pickupPoint);
        }


        #endregion
    }
}
