using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Data;
using Grand.Domain.Tax;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Tax
{
    /// <summary>
    /// Tax category service
    /// </summary>
    public partial class TaxCategoryService : ITaxCategoryService
    {
        #region Fields

        private readonly IRepository<TaxCategory> _taxCategoryRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheBase">Cache manager</param>
        /// <param name="taxCategoryRepository">Tax category repository</param>
        /// <param name="mediator">Mediator</param>
        public TaxCategoryService(ICacheBase cacheBase,
            IRepository<TaxCategory> taxCategoryRepository,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _taxCategoryRepository = taxCategoryRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all tax categories
        /// </summary>
        /// <returns>Tax categories</returns>
        public virtual async Task<IList<TaxCategory>> GetAllTaxCategories()
        {
            string key = string.Format(CacheKey.TAXCATEGORIES_ALL_KEY);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from tc in _taxCategoryRepository.Table
                            orderby tc.DisplayOrder
                            select tc;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets a tax category
        /// </summary>
        /// <param name="taxCategoryId">Tax category identifier</param>
        /// <returns>Tax category</returns>
        public virtual Task<TaxCategory> GetTaxCategoryById(string taxCategoryId)
        {
            string key = string.Format(CacheKey.TAXCATEGORIES_BY_ID_KEY, taxCategoryId);
            return _cacheBase.GetAsync(key, () => _taxCategoryRepository.GetByIdAsync(taxCategoryId));
        }

        /// <summary>
        /// Inserts a tax category
        /// </summary>
        /// <param name="taxCategory">Tax category</param>
        public virtual async Task InsertTaxCategory(TaxCategory taxCategory)
        {
            if (taxCategory == null)
                throw new ArgumentNullException(nameof(taxCategory));

            await _taxCategoryRepository.InsertAsync(taxCategory);

            await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(taxCategory);
        }

        /// <summary>
        /// Updates the tax category
        /// </summary>
        /// <param name="taxCategory">Tax category</param>
        public virtual async Task UpdateTaxCategory(TaxCategory taxCategory)
        {
            if (taxCategory == null)
                throw new ArgumentNullException(nameof(taxCategory));

            await _taxCategoryRepository.UpdateAsync(taxCategory);

            await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(taxCategory);
        }
        /// <summary>
        /// Deletes a tax category
        /// </summary>
        /// <param name="taxCategory">Tax category</param>
        public virtual async Task DeleteTaxCategory(TaxCategory taxCategory)
        {
            if (taxCategory == null)
                throw new ArgumentNullException(nameof(taxCategory));

            await _taxCategoryRepository.DeleteAsync(taxCategory);

            //clear tax categories cache
            await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);

            //clear product cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(taxCategory);
        }

        #endregion
    }
}
