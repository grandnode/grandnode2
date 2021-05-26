using Grand.Business.Marketing.Extensions;
using Grand.Business.Marketing.Interfaces.Knowledgebase;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Knowledgebase;
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

namespace Grand.Business.Marketing.Services.Knowledgebase
{
    public class KnowledgebaseService : IKnowledgebaseService
    {
        private readonly IRepository<KnowledgebaseCategory> _knowledgebaseCategoryRepository;
        private readonly IRepository<KnowledgebaseArticle> _knowledgebaseArticleRepository;
        private readonly IRepository<KnowledgebaseArticleComment> _articleCommentRepository;
        private readonly IMediator _mediator;
        private readonly IWorkContext _workContext;
        private readonly ICacheBase _cacheBase;

        /// <summary>
        /// Ctor
        /// </summary>
        public KnowledgebaseService
            (IRepository<KnowledgebaseCategory> knowledgebaseCategoryRepository,
            IRepository<KnowledgebaseArticle> knowledgebaseArticleRepository,
            IRepository<KnowledgebaseArticleComment> articleCommentRepository,
            IMediator mediator,
            IWorkContext workContext,
            ICacheBase cacheBase
            )
        {
            _knowledgebaseCategoryRepository = knowledgebaseCategoryRepository;
            _knowledgebaseArticleRepository = knowledgebaseArticleRepository;
            _articleCommentRepository = articleCommentRepository;
            _mediator = mediator;
            _workContext = workContext;
            _cacheBase = cacheBase;
        }

        /// <summary>
        /// Deletes knowledgebase category
        /// </summary>
        /// <param name="id"></param>
        public virtual async Task DeleteKnowledgebaseCategory(KnowledgebaseCategory kc)
        {
            var children = _knowledgebaseCategoryRepository.Table.Where(x => x.ParentCategoryId == kc.Id).ToList();
            await _knowledgebaseCategoryRepository.DeleteAsync(kc);
            foreach (var child in children)
            {
                child.ParentCategoryId = "";
                await UpdateKnowledgebaseCategory(child);
            }

            await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);

            await _mediator.EntityDeleted(kc);
        }

        /// <summary>
        /// Edits knowledgebase category
        /// </summary>
        /// <param name="kc"></param>
        public virtual async Task UpdateKnowledgebaseCategory(KnowledgebaseCategory kc)
        {
            kc.UpdatedOnUtc = DateTime.UtcNow;
            await _knowledgebaseCategoryRepository.UpdateAsync(kc);
            await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
            await _mediator.EntityUpdated(kc);
        }

        /// <summary>
        /// Gets knowledgebase category
        /// </summary>
        /// <param name="id"></param>
        /// <returns>knowledgebase category</returns>
        public virtual async Task<KnowledgebaseCategory> GetKnowledgebaseCategory(string id)
        {
            return await Task.FromResult(_knowledgebaseCategoryRepository.Table.Where(x => x.Id == id).FirstOrDefault());
        }

        /// <summary>
        /// Gets knowledgebase category
        /// </summary>
        /// <param name="id"></param>
        /// <returns>knowledgebase category</returns>
        public virtual async Task<KnowledgebaseCategory> GetPublicKnowledgebaseCategory(string id)
        {
            string key = string.Format(CacheKey.KNOWLEDGEBASE_CATEGORY_BY_ID, id, _workContext.CurrentCustomer.GetCustomerGroupIds(),
                _workContext.CurrentStore.Id);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _knowledgebaseCategoryRepository.Table
                            select p;

                query = query.Where(x => x.Published);
                query = query.Where(x => x.Id == id);

                if (!CommonHelper.IgnoreAcl)
                {
                    //Limited to customer groups rules
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }

                if (!CommonHelper.IgnoreStoreLimitations)
                {
                    //Store acl
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                            select p;
                }

                var toReturn = await Task.FromResult(query.FirstOrDefault());
                return toReturn;
            });
        }

        /// <summary>
        /// Inserts knowledgebase category
        /// </summary>
        /// <param name="kc"></param>
        public virtual async Task InsertKnowledgebaseCategory(KnowledgebaseCategory kc)
        {
            kc.CreatedOnUtc = DateTime.UtcNow;
            kc.UpdatedOnUtc = DateTime.UtcNow;

            await _knowledgebaseCategoryRepository.InsertAsync(kc);

            await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);

            await _mediator.EntityInserted(kc);
        }

        /// <summary>
        /// Gets knowledgebase categories
        /// </summary>
        /// <returns>List of knowledgebase categories</returns>
        public virtual async Task<List<KnowledgebaseCategory>> GetKnowledgebaseCategories()
        {
            var categories = _knowledgebaseCategoryRepository.Table.OrderBy(x => x.ParentCategoryId).ThenBy(x => x.DisplayOrder).ToList();
            return await Task.FromResult(categories.SortCategoriesForTree());
        }

        /// <summary>
        /// Gets knowledgebase article
        /// </summary>
        /// <param name="id"></param>
        /// <returns>knowledgebase article</returns>
        public virtual Task<KnowledgebaseArticle> GetKnowledgebaseArticle(string id)
        {
            return _knowledgebaseArticleRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Gets knowledgebase articles
        /// </summary>
        /// <returns>List of knowledgebase articles</returns>
        /// <param name="storeId">Store ident</param>
        public virtual async Task<List<KnowledgebaseArticle>> GetKnowledgebaseArticles(string storeId = "")
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                        select p;

            var customer = _workContext.CurrentCustomer;

            if (!CommonHelper.IgnoreAcl)
            {
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                        where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                        select p;
            }

            if (!CommonHelper.IgnoreStoreLimitations && !string.IsNullOrEmpty(storeId))
            {
                //Limited to stores rules
                query = from p in query
                        where !p.LimitedToStores || p.Stores.Contains(storeId)
                        select p;
            }
            query = query.OrderBy(x => x.DisplayOrder);
            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Inserts knowledgebase article
        /// </summary>
        /// <param name="ka"></param>
        public virtual async Task InsertKnowledgebaseArticle(KnowledgebaseArticle ka)
        {
            ka.CreatedOnUtc = DateTime.UtcNow;
            ka.UpdatedOnUtc = DateTime.UtcNow;
            await _knowledgebaseArticleRepository.InsertAsync(ka);
            await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
            await _mediator.EntityInserted(ka);
        }

        /// <summary>
        /// Edits knowledgebase article
        /// </summary>
        /// <param name="ka"></param>
        public virtual async Task UpdateKnowledgebaseArticle(KnowledgebaseArticle ka)
        {
            ka.UpdatedOnUtc = DateTime.UtcNow;
            await _knowledgebaseArticleRepository.UpdateAsync(ka);
            await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
            await _mediator.EntityUpdated(ka);
        }

        /// <summary>
        /// Deletes knowledgebase article
        /// </summary>
        /// <param name="id"></param>
        public virtual async Task DeleteKnowledgebaseArticle(KnowledgebaseArticle ka)
        {
            await _knowledgebaseArticleRepository.DeleteAsync(ka);
            await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
            await _mediator.EntityDeleted(ka);
        }

        /// <summary>
        /// Gets knowledgebase articles by category id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>IPagedList<KnowledgebaseArticle></returns>
        public virtual async Task<IPagedList<KnowledgebaseArticle>> GetKnowledgebaseArticlesByCategoryId(string id, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var articles = await Task.FromResult(_knowledgebaseArticleRepository.Table.Where(x => x.ParentCategoryId == id).OrderBy(x => x.DisplayOrder).ToList());
            return new PagedList<KnowledgebaseArticle>(articles, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets public(published etc) knowledgebase categories
        /// </summary>
        /// <returns>List of public knowledgebase categories</returns>
        public virtual async Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategories()
        {
            var key = string.Format(CacheKey.KNOWLEDGEBASE_CATEGORIES, string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                _workContext.CurrentStore.Id);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _knowledgebaseCategoryRepository.Table
                            select p;

                query = query.Where(x => x.Published);

                if (!CommonHelper.IgnoreAcl)
                {
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }

                if (!CommonHelper.IgnoreStoreLimitations)
                {
                    //Store acl
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                            select p;
                }
                query = query.OrderBy(x => x.DisplayOrder);
                return await Task.FromResult(query.ToList());

            });
        }

        /// <summary>
        /// Gets public(published etc) knowledgebase articles
        /// </summary>
        /// <returns>List of public knowledgebase articles</returns>
        public virtual async Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticles()
        {
            var key = string.Format(CacheKey.ARTICLES, string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                _workContext.CurrentStore.Id);

            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _knowledgebaseArticleRepository.Table
                            select p;

                query = query.Where(x => x.Published);

                if (!CommonHelper.IgnoreAcl)
                {
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }

                if (!CommonHelper.IgnoreStoreLimitations)
                {
                    //Store acl
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                            select p;
                }

                query = query.OrderBy(x => x.DisplayOrder);
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets knowledgebase article if it is published etc
        /// </summary>
        /// <returns>knowledgebase article</returns>
        public virtual async Task<KnowledgebaseArticle> GetPublicKnowledgebaseArticle(string id)
        {
            var key = string.Format(CacheKey.ARTICLE_BY_ID, id, string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                _workContext.CurrentStore.Id);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _knowledgebaseArticleRepository.Table
                            select p;

                query = query.Where(x => x.Published);
                query = query.Where(x => x.Id == id);

                if (!CommonHelper.IgnoreAcl)
                {
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }

                if (!CommonHelper.IgnoreStoreLimitations)
                {
                    //Store acl
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                            select p; 
                }

                return await Task.FromResult(query.FirstOrDefault());
            });
        }

        /// <summary>
        /// Gets public(published etc) knowledgebase articles for category id
        /// </summary>
        /// <returns>List of public knowledgebase articles</returns>
        public virtual async Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByCategory(string categoryId)
        {
            var key = string.Format(CacheKey.ARTICLES_BY_CATEGORY_ID, categoryId, string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                _workContext.CurrentStore.Id);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _knowledgebaseArticleRepository.Table
                            select p;

                query = query.Where(x => x.Published);
                query = query.Where(x => x.ParentCategoryId == categoryId);

                if (!CommonHelper.IgnoreAcl)
                {
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }

                if (!CommonHelper.IgnoreStoreLimitations)
                {
                    //Store acl
                    var currentStoreId = new List<string> { _workContext.CurrentStore.Id };
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                            select p;
                }
                query = query.OrderBy(x => x.DisplayOrder);
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets public(published etc) knowledgebase articles for keyword
        /// </summary>
        /// <returns>List of public knowledgebase articles</returns>
        public virtual async Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByKeyword(string keyword)
        {
            var key = string.Format(CacheKey.ARTICLES_BY_KEYWORD, keyword, string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                _workContext.CurrentStore.Id);

            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _knowledgebaseArticleRepository.Table
                            select p;

                query = query.Where(x => x.Published);

                query = query.Where(p => p.Locales.Any(x => x.LocaleValue != null && x.LocaleValue.ToLower().Contains(keyword.ToLower()))
                    || p.Name.ToLower().Contains(keyword.ToLower()) || p.Content.ToLower().Contains(keyword.ToLower()));

                if (!CommonHelper.IgnoreAcl)
                {
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }

                if (!CommonHelper.IgnoreStoreLimitations)
                {
                    //Store acl
                    var currentStoreId = new List<string> { _workContext.CurrentStore.Id };
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                            select p;
                }
                query = query.OrderBy(x => x.DisplayOrder);
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets public(published etc) knowledgebase categories for keyword
        /// </summary>
        /// <returns>List of public knowledgebase categories</returns>
        public virtual async Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategoriesByKeyword(string keyword)
        {
            var key = string.Format(CacheKey.KNOWLEDGEBASE_CATEGORIES_BY_KEYWORD, keyword, string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                _workContext.CurrentStore.Id);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _knowledgebaseCategoryRepository.Table
                            select p;

                query = query.Where(x => x.Published);

                query = query.Where(p => p.Locales.Any(x => x.LocaleValue != null && x.LocaleValue.ToLower().Contains(keyword.ToLower()))
                    || p.Name.ToLower().Contains(keyword.ToLower()) || p.Description.ToLower().Contains(keyword.ToLower()));

                if (!CommonHelper.IgnoreAcl)
                {
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }

                if (!CommonHelper.IgnoreStoreLimitations)
                {
                    //Store acl
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                            select p; ;
                }
                query = query.OrderBy(x => x.DisplayOrder);
                return await Task.FromResult(query.ToList());

            });
        }

        /// <summary>
        /// Gets homepage knowledgebase articles
        /// </summary>
        /// <returns>List of homepage knowledgebase articles</returns>
        public virtual async Task<List<KnowledgebaseArticle>> GetHomepageKnowledgebaseArticles()
        {
            var key = string.Format(CacheKey.HOMEPAGE_ARTICLES, string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                _workContext.CurrentStore.Id);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _knowledgebaseArticleRepository.Table
                            select p;

                query = query.Where(x => x.Published);
                query = query.Where(x => x.ShowOnHomepage);

                if (!CommonHelper.IgnoreAcl)
                {
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }

                if (!CommonHelper.IgnoreStoreLimitations)
                {
                    //Store acl
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                            select p;
                }
                query = query.OrderBy(x => x.DisplayOrder);
                return await Task.FromResult(query.ToList());

            });
        }

        /// <summary>
        /// Gets knowledgebase articles by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>IPagedList<KnowledgebaseArticle></returns>
        public virtual async Task<IPagedList<KnowledgebaseArticle>> GetKnowledgebaseArticlesByName(string name, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                        select p;


            query = query.Where(x => x.Published);

            if (!string.IsNullOrEmpty(name))
            {

                query = query.Where(p => p.Locales.Any(x => x.LocaleKey == "Name" && x.LocaleValue != null && x.LocaleValue.ToLower().Contains(name.ToLower()))
                    || p.Name.ToLower().Contains(name.ToLower()));
                
            }

            query = query.OrderBy(x => x.DisplayOrder);
            var toReturn = await Task.FromResult(query.ToList());

            return new PagedList<KnowledgebaseArticle>(toReturn, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets related knowledgebase articles
        /// </summary>
        /// <param name="name"></param>
        /// <returns>IPagedList<KnowledgebaseArticle></returns>
        public virtual async Task<IPagedList<KnowledgebaseArticle>> GetRelatedKnowledgebaseArticles(string articleId, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var article = await GetKnowledgebaseArticle(articleId);
            List<KnowledgebaseArticle> toReturn = new List<KnowledgebaseArticle>();

            foreach (var id in article.RelatedArticles)
            {
                var relatedArticle = await GetKnowledgebaseArticle(id);
                if (relatedArticle != null)
                    toReturn.Add(relatedArticle);
            }

            return new PagedList<KnowledgebaseArticle>(toReturn.ToList(), pageIndex, pageSize);
        }

        /// <summary>
        /// Inserts an article comment
        /// </summary>
        /// <param name="articleComment">Article comment</param>
        public virtual async Task InsertArticleComment(KnowledgebaseArticleComment articleComment)
        {
            if (articleComment == null)
                throw new ArgumentNullException(nameof(articleComment));

            await _articleCommentRepository.InsertAsync(articleComment);

            //event notification
            await _mediator.EntityInserted(articleComment);
        }

        /// <summary>
        /// Gets all comments
        /// </summary>
        /// <param name="customerId">Customer identifier; "" to load all records</param>
        /// <returns>Comments</returns>
        public virtual async Task<IList<KnowledgebaseArticleComment>> GetAllComments(string customerId)
        {
            var query = from c in _articleCommentRepository.Table
                        orderby c.CreatedOnUtc
                        where (customerId == "" || c.CustomerId == customerId)
                        select c;
            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Gets an article comment
        /// </summary>
        /// <param name="articleId">Article identifier</param>
        /// <returns>Article comment</returns>
        public virtual Task<KnowledgebaseArticleComment> GetArticleCommentById(string articleId)
        {
            return _articleCommentRepository.GetByIdAsync(articleId);
        }

        /// <summary>
        /// Get article comments by identifiers
        /// </summary>
        /// <param name="commentIds"Article comment identifiers</param>
        /// <returns>Article comments</returns>
        public virtual async Task<IList<KnowledgebaseArticleComment>> GetArticleCommentsByIds(string[] commentIds)
        {
            if (commentIds == null || commentIds.Length == 0)
                return new List<KnowledgebaseArticleComment>();

            var query = from bc in _articleCommentRepository.Table
                        where commentIds.Contains(bc.Id)
                        select bc;
            var comments = query.ToList();
            //sort by passed identifiers
            var sortedComments = new List<KnowledgebaseArticleComment>();
            foreach (string id in commentIds)
            {
                var comment = comments.Find(x => x.Id == id);
                if (comment != null)
                    sortedComments.Add(comment);
            }
            return await Task.FromResult(sortedComments);
        }

        public virtual async Task<IList<KnowledgebaseArticleComment>> GetArticleCommentsByArticleId(string articleId)
        {
            var query = from c in _articleCommentRepository.Table
                        where c.ArticleId == articleId
                        orderby c.CreatedOnUtc
                        select c;
            return await Task.FromResult(query.ToList());
        }

        public virtual async Task DeleteArticleComment(KnowledgebaseArticleComment articleComment)
        {
            if (articleComment == null)
                throw new ArgumentNullException(nameof(articleComment));

            await _articleCommentRepository.DeleteAsync(articleComment);
        }
    }
}
