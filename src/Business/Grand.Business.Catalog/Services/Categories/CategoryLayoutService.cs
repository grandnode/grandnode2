using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;

namespace Grand.Business.Catalog.Services.Categories
{
    /// <summary>
    /// Category layout service
    /// </summary>
    public partial class CategoryLayoutService : ICategoryLayoutService
    {
        #region Fields

        private readonly IRepository<CategoryLayout> _categoryLayoutRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="categoryLayoutRepository">Category layout repository</param>
        /// <param name="cacheBase">cache base</param>
        /// <param name="mediator">Mediator</param>
        public CategoryLayoutService(IRepository<CategoryLayout> categoryLayoutRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _categoryLayoutRepository = categoryLayoutRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all existing category layout
        /// </summary>
        /// <returns>Category layouts</returns>
        public virtual async Task<IList<CategoryLayout>> GetAllCategoryLayouts()
        {
            return await _cacheBase.GetAsync(CacheKey.CATEGORY_LAYOUT_ALL, async () =>
            {
                var query = from pt in _categoryLayoutRepository.Table
                            orderby pt.DisplayOrder
                            select pt;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets a specified category layout
        /// </summary>
        /// <param name="categoryLayoutId">Category layout identifier</param>
        /// <returns>Category layout</returns>
        public virtual Task<CategoryLayout> GetCategoryLayoutById(string categoryLayoutId)
        {
            string key = string.Format(CacheKey.CATEGORY_LAYOUT_BY_ID_KEY, categoryLayoutId);
            return _cacheBase.GetAsync(key, () => _categoryLayoutRepository.GetByIdAsync(categoryLayoutId));
        }

        /// <summary>
        /// Insert new category layout
        /// </summary>
        /// <param name="categoryLayout">Category layout</param>
        public virtual async Task InsertCategoryLayout(CategoryLayout categoryLayout)
        {
            if (categoryLayout == null)
                throw new ArgumentNullException(nameof(categoryLayout));

            await _categoryLayoutRepository.InsertAsync(categoryLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.CATEGORY_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(categoryLayout);
        }

        /// <summary>
        /// Updates the category layout
        /// </summary>
        /// <param name="categoryLayout">Category layout</param>
        public virtual async Task UpdateCategoryLayout(CategoryLayout categoryLayout)
        {
            if (categoryLayout == null)
                throw new ArgumentNullException(nameof(categoryLayout));

            await _categoryLayoutRepository.UpdateAsync(categoryLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.CATEGORY_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(categoryLayout);
        }

        /// <summary>
        /// Delete category layout
        /// </summary>
        /// <param name="categoryLayout">Category layout</param>
        public virtual async Task DeleteCategoryLayout(CategoryLayout categoryLayout)
        {
            if (categoryLayout == null)
                throw new ArgumentNullException(nameof(categoryLayout));

            await _categoryLayoutRepository.DeleteAsync(categoryLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.CATEGORY_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(categoryLayout);
        }

        #endregion
    }
}
