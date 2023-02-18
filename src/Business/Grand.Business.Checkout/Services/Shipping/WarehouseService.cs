﻿using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Data;
using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Checkout.Services.Shipping
{
    public class WarehouseService : IWarehouseService
    {
        #region Fields

        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public WarehouseService(
            IRepository<Warehouse> warehouseRepository,
            IMediator mediator,
            ICacheBase cacheBase)
        {
            _warehouseRepository = warehouseRepository;
            _mediator = mediator;
            _cacheBase = cacheBase;
        }

        #endregion

        #region Warehouses

        /// <summary>
        /// Gets a warehouse
        /// </summary>
        /// <param name="warehouseId">The warehouse identifier</param>
        /// <returns>Warehouse</returns>
        public virtual Task<Warehouse> GetWarehouseById(string warehouseId)
        {
            var key = string.Format(CacheKey.WAREHOUSES_BY_ID_KEY, warehouseId);
            return _cacheBase.GetAsync(key, () => _warehouseRepository.GetByIdAsync(warehouseId));
        }

        /// <summary>
        /// Gets all warehouses
        /// </summary>
        /// <returns>Warehouses</returns>
        public virtual async Task<IList<Warehouse>> GetAllWarehouses()
        {
            return await _cacheBase.GetAsync(CacheKey.WAREHOUSES_ALL, async () =>
            {
                var query = from wh in _warehouseRepository.Table
                            orderby wh.DisplayOrder
                            select wh;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Inserts a warehouse
        /// </summary>
        /// <param name="warehouse">Warehouse</param>
        public virtual async Task InsertWarehouse(Warehouse warehouse)
        {
            if (warehouse == null)
                throw new ArgumentNullException(nameof(warehouse));

            await _warehouseRepository.InsertAsync(warehouse);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(warehouse);
        }

        /// <summary>
        /// Updates the warehouse
        /// </summary>
        /// <param name="warehouse">Warehouse</param>
        public virtual async Task UpdateWarehouse(Warehouse warehouse)
        {
            if (warehouse == null)
                throw new ArgumentNullException(nameof(warehouse));

            await _warehouseRepository.UpdateAsync(warehouse);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(warehouse);
        }

        /// <summary>
        /// Deletes a warehouse
        /// </summary>
        /// <param name="warehouse">The warehouse</param>
        public virtual async Task DeleteWarehouse(Warehouse warehouse)
        {
            if (warehouse == null)
                throw new ArgumentNullException(nameof(warehouse));

            await _warehouseRepository.DeleteAsync(warehouse);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY);
            //clear product cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(warehouse);
        }
        #endregion
    }
}
