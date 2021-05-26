using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Brands
{
    /// <summary>
    /// Brand layout service
    /// </summary>
    public partial class BrandLayoutService : IBrandLayoutService
    {
        #region Fields

        private readonly IRepository<BrandLayout> _brandLayoutRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="brandLayoutRepository">Brand layout repository</param>
        /// <param name="cacheBase">Cache base</param>
        /// <param name="mediator">Mediator</param>
        public BrandLayoutService(IRepository<BrandLayout> brandLayoutRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _brandLayoutRepository = brandLayoutRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all brand layouts
        /// </summary>
        /// <returns>Brand layouts</returns>
        public virtual async Task<IList<BrandLayout>> GetAllBrandLayouts()
        {
            return await _cacheBase.GetAsync(CacheKey.BRAND_LAYOUT_ALL, async () =>
            {
                var query = from pt in _brandLayoutRepository.Table
                            orderby pt.DisplayOrder
                            select pt;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets a brand layout
        /// </summary>
        /// <param name="brandLayoutId">Brand layout</param>
        /// <returns>Brand layout</returns>
        public virtual Task<BrandLayout> GetBrandLayoutById(string brandLayoutId)
        {
            string key = string.Format(CacheKey.BRAND_LAYOUT_BY_ID_KEY, brandLayoutId);
            return _cacheBase.GetAsync(key, () => _brandLayoutRepository.GetByIdAsync(brandLayoutId));
        }

        /// <summary>
        /// Inserts brand layout
        /// </summary>
        /// <param name="brandLayout">Brand layout</param>
        public virtual async Task InsertBrandLayout(BrandLayout brandLayout)
        {
            if (brandLayout == null)
                throw new ArgumentNullException(nameof(brandLayout));

            await _brandLayoutRepository.InsertAsync(brandLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.BRAND_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(brandLayout);
        }

        /// <summary>
        /// Updates the brand layout
        /// </summary>
        /// <param name="brandLayout">Brand layout</param>
        public virtual async Task UpdateBrandLayout(BrandLayout brandLayout)
        {
            if (brandLayout == null)
                throw new ArgumentNullException(nameof(brandLayout));

            await _brandLayoutRepository.UpdateAsync(brandLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.BRAND_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(brandLayout);
        }

        /// <summary>
        /// Delete brand layout
        /// </summary>
        /// <param name="brandLayout">Brand layout</param>
        public virtual async Task DeleteBrandLayout(BrandLayout brandLayout)
        {
            if (brandLayout == null)
                throw new ArgumentNullException(nameof(brandLayout));

            await _brandLayoutRepository.DeleteAsync(brandLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.BRAND_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(brandLayout);
        }


        #endregion
    }
}
