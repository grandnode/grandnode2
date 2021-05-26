using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Brands
{
    /// <summary>
    /// Brand service
    /// </summary>
    public partial class BrandService : IBrandService
    {
        #region Fields

        private readonly IRepository<Brand> _brandRepository;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public BrandService(ICacheBase cacheBase,
            IRepository<Brand> brandRepository,
            IWorkContext workContext,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _brandRepository = brandRepository;
            _workContext = workContext;
            _mediator = mediator;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Gets all brands
        /// </summary>
        /// <param name="brandName">Brand name</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Brands</returns>
        public virtual async Task<IPagedList<Brand>> GetAllBrands(string brandName = "",
            string storeId = "",
            int pageIndex = 0,
            int pageSize = int.MaxValue,
            bool showHidden = false)
        {
            var query = from m in _brandRepository.Table
                        select m;

            if (!showHidden)
                query = query.Where(m => m.Published);
            if (!String.IsNullOrWhiteSpace(brandName))
                query = query.Where(m => m.Name != null && m.Name.ToLower().Contains(brandName.ToLower()));

            if ((!CommonHelper.IgnoreAcl || (!String.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)))
            {
                if (!showHidden && !CommonHelper.IgnoreAcl)
                {
                    //Limited to customer groups rules
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;

                }
                if (!String.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)
                {
                    //Limited to stores rules
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(storeId)
                            select p;
                }
            }
            query = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name);
            return await PagedList<Brand>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets a brand
        /// </summary>
        /// <param name="brandId">Brand id</param>
        /// <returns>Brand</returns>
        public virtual Task<Brand> GetBrandById(string brandId)
        {
            string key = string.Format(CacheKey.BRANDS_BY_ID_KEY, brandId);
            return _cacheBase.GetAsync(key, () => _brandRepository.GetByIdAsync(brandId));
        }

        /// <summary>
        /// Inserts a brand
        /// </summary>
        /// <param name="brand">Brand</param>
        public virtual async Task InsertBrand(Brand brand)
        {
            if (brand == null)
                throw new ArgumentNullException(nameof(brand));

            await _brandRepository.InsertAsync(brand);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(brand);
        }

        /// <summary>
        /// Updates the brand
        /// </summary>
        /// <param name="brand">Brand</param>
        public virtual async Task UpdateBrand(Brand brand)
        {
            if (brand == null)
                throw new ArgumentNullException(nameof(brand));

            await _brandRepository.UpdateAsync(brand);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(brand);
        }
        /// <summary>
        /// Deletes a brand
        /// </summary>
        /// <param name="brand">Brand</param>
        public virtual async Task DeleteBrand(Brand brand)
        {
            if (brand == null)
                throw new ArgumentNullException(nameof(brand));

            await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);

            await _brandRepository.DeleteAsync(brand);

            //event notification
            await _mediator.EntityDeleted(brand);

        }

        /// <summary>
        /// Gets a discount brand mapping 
        /// </summary>
        /// <param name="discountId">Discount id mapping id</param>
        /// <returns>Product brand mapping</returns>
        public virtual async Task<IList<Brand>> GetAllBrandsByDiscount(string discountId)
        {
            var query = from c in _brandRepository.Table
                        where c.AppliedDiscounts.Any(x => x == discountId)
                        select c;

            return await Task.FromResult(query.ToList());
        }

        #endregion

    }
}
