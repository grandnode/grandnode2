using Grand.Business.Catalog.Interfaces.Collections;
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

namespace Grand.Business.Catalog.Services.Collections
{
    /// <summary>
    /// Collection layout service
    /// </summary>
    public partial class CollectionLayoutService : ICollectionLayoutService
    {
        #region Fields

        private readonly IRepository<CollectionLayout> _collectionLayoutRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="collectionLayoutRepository">Collection layout repository</param>
        /// <param name="cacheBase">Cache base</param>
        /// <param name="mediator">Mediator</param>
        public CollectionLayoutService(IRepository<CollectionLayout> collectionLayoutRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _collectionLayoutRepository = collectionLayoutRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all collection layouts
        /// </summary>
        /// <returns>Collection layouts</returns>
        public virtual async Task<IList<CollectionLayout>> GetAllCollectionLayouts()
        {
            return await _cacheBase.GetAsync(CacheKey.COLLECTION_LAYOUT_ALL, async () =>
            {
                var query = from pt in _collectionLayoutRepository.Table
                            orderby pt.DisplayOrder
                            select pt;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets a collection layout
        /// </summary>
        /// <param name="collectionLayoutId">Collection layout id</param>
        /// <returns>Collection layout</returns>
        public virtual Task<CollectionLayout> GetCollectionLayoutById(string collectionLayoutId)
        {
            string key = string.Format(CacheKey.COLLECTION_LAYOUT_BY_ID_KEY, collectionLayoutId);
            return _cacheBase.GetAsync(key, () => _collectionLayoutRepository.GetByIdAsync(collectionLayoutId));
        }

        /// <summary>
        /// Inserts collection layout
        /// </summary>
        /// <param name="collectionLayout">Collection layout</param>
        public virtual async Task InsertCollectionLayout(CollectionLayout collectionLayout)
        {
            if (collectionLayout == null)
                throw new ArgumentNullException(nameof(collectionLayout));

            await _collectionLayoutRepository.InsertAsync(collectionLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.COLLECTION_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(collectionLayout);
        }

        /// <summary>
        /// Updates the collection layout
        /// </summary>
        /// <param name="collectionLayout">Collection layout</param>
        public virtual async Task UpdateCollectionLayout(CollectionLayout collectionLayout)
        {
            if (collectionLayout == null)
                throw new ArgumentNullException(nameof(collectionLayout));

            await _collectionLayoutRepository.UpdateAsync(collectionLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.COLLECTION_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(collectionLayout);
        }

        /// <summary>
        /// Delete collection layout
        /// </summary>
        /// <param name="collectionLayout">Collection layout</param>
        public virtual async Task DeleteCollectionLayout(CollectionLayout collectionLayout)
        {
            if (collectionLayout == null)
                throw new ArgumentNullException(nameof(collectionLayout));

            await _collectionLayoutRepository.DeleteAsync(collectionLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.COLLECTION_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(collectionLayout);
        }


        #endregion
    }
}
