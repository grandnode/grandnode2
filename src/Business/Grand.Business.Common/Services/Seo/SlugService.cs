using Grand.Business.Common.Interfaces.Seo;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Services.Seo
{
    /// <summary>
    /// Provides information about Slug URL Entity
    /// </summary>
    public partial class SlugService : ISlugService
    {
        #region Fields

        private readonly IRepository<EntityUrl> _urlEntityRepository;
        private readonly ICacheBase _cacheBase;
        private readonly AppConfig _config;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheBase">Cache manager</param>
        /// <param name="urlEntityRepository">URL Entity repository</param>
        /// <param name="languageSettings">Localization settings</param>
        public SlugService(ICacheBase cacheBase,
            IRepository<EntityUrl> urlEntityRepository,
            AppConfig config)
        {
            _cacheBase = cacheBase;
            _urlEntityRepository = urlEntityRepository;
            _config = config;
        }

        #endregion


        /// <summary>
        /// Gets all cached URL Entity
        /// </summary>
        /// <returns>cached URL Entities</returns>
        protected virtual async Task<IList<EntityUrl>> GetAllUrlEntityCached()
        {
            //cache
            string key = string.Format(CacheKey.URLEntity_ALL_KEY);
            return await _cacheBase.GetAsync(key, async () =>
            {
                return await Task.FromResult(_urlEntityRepository.Table.ToList());
            });
        }

        #region Methods

        /// <summary>
        /// Gets an URL Entity
        /// </summary>
        /// <param name="entityUrlId">URL Entity identifier</param>
        /// <returns>URL Entity</returns>
        public virtual Task<EntityUrl> GetEntityUrlById(string urlEntityId)
        {
            return _urlEntityRepository.GetByIdAsync(urlEntityId);
        }

        /// <summary>
        /// Inserts an URL Entity
        /// </summary>
        /// <param name="urlEntity">URL Entity</param>
        public virtual async Task InsertEntityUrl(EntityUrl urlEntity)
        {
            if (urlEntity == null)
                throw new ArgumentNullException(nameof(urlEntity));

            await _urlEntityRepository.InsertAsync(urlEntity);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.URLEntity_PATTERN_KEY);
        }

        /// <summary>
        /// Updates the URL Entity
        /// </summary>
        /// <param name="urlEntity">URL Entity</param>
        public virtual async Task UpdateEntityUrl(EntityUrl urlEntity)
        {
            if (urlEntity == null)
                throw new ArgumentNullException(nameof(urlEntity));

            await _urlEntityRepository.UpdateAsync(urlEntity);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.URLEntity_PATTERN_KEY);
        }
        /// <summary>
        /// Deletes an URL Entity
        /// </summary>
        /// <param name="urlEntity">URL Entity</param>
        public virtual async Task DeleteEntityUrl(EntityUrl urlEntity)
        {
            if (urlEntity == null)
                throw new ArgumentNullException(nameof(urlEntity));

            await _urlEntityRepository.DeleteAsync(urlEntity);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.URLEntity_PATTERN_KEY);
        }


        /// <summary>
        /// Find URL Entity
        /// </summary>
        /// <param name="slug">Slug</param>
        /// <returns>Found URL Entity</returns>
        public virtual async Task<EntityUrl> GetBySlug(string slug)
        {
            if (String.IsNullOrEmpty(slug))
                return null;

            slug = slug.ToLowerInvariant();

            var query = from ur in _urlEntityRepository.Table
                        where ur.Slug == slug
                        orderby ur.IsActive
                        select ur;
            return await Task.FromResult(query.FirstOrDefault());
        }

        /// <summary>
        /// Find URL Entity (cached version).
        /// </summary>
        /// <param name="slug">Slug</param>
        /// <returns>Found URL Entity</returns>
        public virtual async Task<EntityUrl> GetBySlugCached(string slug)
        {
            if (String.IsNullOrEmpty(slug))
                return null;

            slug = slug.ToLowerInvariant();

            if (_config.LoadAllUrlEntitiesOnStartup)
            {
                var source = await GetAllUrlEntityCached();
                var query = from ur in source
                            where ur.Slug != null && ur.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)
                            orderby ur.IsActive
                            select ur;
                var entityUrlForCaching = query.FirstOrDefault();
                return entityUrlForCaching;
            }

            string key = string.Format(CacheKey.URLEntity_BY_SLUG_KEY, slug);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var urlEntity = await GetBySlug(slug);
                if (urlEntity == null)
                    return null;

                return urlEntity;
            });
        }

        /// <summary>
        /// Gets all URL Entity
        /// </summary>
        /// <param name="slug">Slug</param>
        /// <param name="active">Active</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>URL Entity</returns>
        public virtual async Task<IPagedList<EntityUrl>> GetAllEntityUrl(string slug = "", bool? active = null, int pageIndex = 0, int pageSize = int.MaxValue)
        {

            var query = from p in _urlEntityRepository.Table
                        select p;

            if (!string.IsNullOrWhiteSpace(slug))
                query = query.Where(ur => ur.Slug.Contains(slug.ToLowerInvariant()));

            if (active.HasValue)
                query = query.Where(ur => ur.IsActive == active.Value);

            query = query.OrderBy(ur => ur.Slug);
            return await PagedList<EntityUrl>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Find slug
        /// </summary>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="entityName">Entity name</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Found slug</returns>
        public virtual async Task<string> GetActiveSlug(string entityId, string entityName, string languageId)
        {
            if (_config.LoadAllUrlEntitiesOnStartup)
            {
                string key = string.Format(CacheKey.URLEntity_ACTIVE_BY_ID_NAME_LANGUAGE_KEY, entityId, entityName, languageId);
                return await _cacheBase.GetAsync(key, async () =>
                {
                    var source = await GetAllUrlEntityCached();
                    var query = from ur in source
                                where ur.EntityId == entityId &&
                                ur.EntityName == entityName &&
                                ur.LanguageId == languageId &&
                                ur.IsActive
                                select ur.Slug;
                    var slug = query.FirstOrDefault();
                    if (slug == null)
                        slug = "";
                    return slug;
                });
            }
            else
            {
                string key = string.Format(CacheKey.URLEntity_ACTIVE_BY_ID_NAME_LANGUAGE_KEY, entityId, entityName, languageId);
                return await _cacheBase.GetAsync(key, async () =>
                {

                    var source = _urlEntityRepository.Table;
                    var query = from ur in source
                                where ur.EntityId == entityId &&
                                ur.EntityName == entityName &&
                                ur.LanguageId == languageId &&
                                ur.IsActive
                                select ur.Slug;
                    var slug = await Task.FromResult(query.FirstOrDefault());
                    if (slug == null)
                        slug = "";
                    return slug;
                });
            }
        }

        /// <summary>
        /// Save slug
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="slug">Slug</param>
        /// <param name="languageId">Language ID</param>
        public virtual async Task SaveSlug<T>(T entity, string slug, string languageId) where T : BaseEntity, ISlugEntity
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            string entityId = entity.Id;
            string entityName = typeof(T).Name;

            var query = from ur in _urlEntityRepository.Table
                        where ur.EntityId == entityId &&
                        ur.EntityName == entityName &&
                        ur.LanguageId == languageId
                        select ur;

            var allUrlEntity = query.ToList();
            var activeUrlEntity = allUrlEntity.FirstOrDefault(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(slug))
                slug = slug.ToLowerInvariant();

            if (activeUrlEntity == null && !string.IsNullOrWhiteSpace(slug))
            {
                //find in non-active records with the specified slug
                var nonActiveRecordWithSpecifiedSlug = allUrlEntity
                    .FirstOrDefault(x => x.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) && !x.IsActive);
                if (nonActiveRecordWithSpecifiedSlug != null)
                {
                    //mark non-active record as active
                    nonActiveRecordWithSpecifiedSlug.IsActive = true;
                    await UpdateEntityUrl(nonActiveRecordWithSpecifiedSlug);
                }
                else
                {
                    //new record
                    var entityUrl = new EntityUrl
                    {
                        EntityId = entityId,
                        EntityName = entityName,
                        Slug = slug,
                        LanguageId = languageId,
                        IsActive = true,
                    };
                    await InsertEntityUrl(entityUrl);
                }
            }

            if (activeUrlEntity != null && string.IsNullOrWhiteSpace(slug))
            {
                activeUrlEntity.IsActive = false;
                await UpdateEntityUrl(activeUrlEntity);
            }

            if (activeUrlEntity != null && !string.IsNullOrWhiteSpace(slug))
            {
                if (!activeUrlEntity.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
                {
                    //find in non-active records with the specified slug
                    var nonActiveRecordWithSpecifiedSlug = allUrlEntity
                        .FirstOrDefault(x => x.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) && !x.IsActive);
                    if (nonActiveRecordWithSpecifiedSlug != null)
                    {
                        nonActiveRecordWithSpecifiedSlug.IsActive = true;
                        await UpdateEntityUrl(nonActiveRecordWithSpecifiedSlug);

                        activeUrlEntity.IsActive = false;
                        await UpdateEntityUrl(activeUrlEntity);
                    }
                    else
                    {
                        //insert new record
                        var entityUrl = new EntityUrl
                        {
                            EntityId = entityId,
                            EntityName = entityName,
                            Slug = slug,
                            LanguageId = languageId,
                            IsActive = true,
                        };
                        await InsertEntityUrl(entityUrl);

                        activeUrlEntity.IsActive = false;
                        await UpdateEntityUrl(activeUrlEntity);
                    }

                }
            }
        }

        #endregion
    }
}