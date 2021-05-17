using Grand.Domain;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Shipping.ShippingPoint.Domain;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shipping.ShippingPoint.Services
{
    public class ShippingPointService : IShippingPointService
    {
        #region Constants

        private const string PICKUP_POINT_PATTERN_KEY = "Grand.ShippingPoint.";

        #endregion

        #region Fields

        private readonly ICacheBase _cacheBase;
        private readonly IRepository<ShippingPoints> _shippingPointRepository;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheBase">Cache base</param>
        /// <param name="ShippingPointRepository">Store pickup point repository</param>
        public ShippingPointService(ICacheBase cacheBase,
            IRepository<ShippingPoints> ShippingPointRepository)
        {
            _cacheBase = cacheBase;
            _shippingPointRepository = ShippingPointRepository;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all pickup points
        /// </summary>
        /// <param name="storeId">The store identifier; pass "" to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Pickup points</returns>
        public virtual async Task<IPagedList<ShippingPoints>> GetAllStoreShippingPoint(string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from gp in _shippingPointRepository.Table
                        where (gp.StoreId == storeId || string.IsNullOrEmpty(gp.StoreId)) || storeId == ""
                        select gp;

            var records = query.ToList();

            //paging
            return await Task.FromResult(new PagedList<ShippingPoints>(records, pageIndex, pageSize));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pickupPointId"></param>
        /// <returns></returns>
        public virtual Task<ShippingPoints> GetStoreShippingPointByPointName(string pointName)
        {
            return Task.FromResult((from shippingOoint in _shippingPointRepository.Table
                    where shippingOoint.ShippingPointName == pointName
                    select shippingOoint).FirstOrDefault());
        }

        /// <summary>
        /// Gets a pickup point
        /// </summary>
        /// <param name="pickupPointId">Pickup point identifier</param>
        /// <returns>Pickup point</returns>
        public virtual Task<ShippingPoints> GetStoreShippingPointById(string pickupPointId)
        {
            return _shippingPointRepository.GetByIdAsync(pickupPointId);
        }

        /// <summary>
        /// Inserts a pickup point
        /// </summary>
        /// <param name="pickupPoint">Pickup point</param>
        public virtual async Task InsertStoreShippingPoint(ShippingPoints pickupPoint)
        {
            if (pickupPoint == null)
                throw new ArgumentNullException(nameof(pickupPoint));

            await _shippingPointRepository.InsertAsync(pickupPoint);
            await _cacheBase.RemoveByPrefix(PICKUP_POINT_PATTERN_KEY);
        }

        /// <summary>
        /// Updates the pickup point
        /// </summary>
        /// <param name="pickupPoint">Pickup point</param>
        public virtual async Task UpdateStoreShippingPoint(ShippingPoints pickupPoint)
        {
            if (pickupPoint == null)
                throw new ArgumentNullException(nameof(pickupPoint));

            await _shippingPointRepository.UpdateAsync(pickupPoint);
            await _cacheBase.RemoveByPrefix(PICKUP_POINT_PATTERN_KEY);
        }

        /// <summary>
        /// Deletes a pickup point
        /// </summary>
        /// <param name="pickupPoint">Pickup point</param>
        public virtual async Task DeleteStoreShippingPoint(ShippingPoints pickupPoint)
        {
            if (pickupPoint == null)
                throw new ArgumentNullException(nameof(pickupPoint));

            await _shippingPointRepository.DeleteAsync(pickupPoint);
            await _cacheBase.RemoveByPrefix(PICKUP_POINT_PATTERN_KEY);
        }
        #endregion
    }
}
