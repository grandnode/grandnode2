using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Cms.Services;

public class KnowledgebaseService : IKnowledgebaseService
{
    private readonly AccessControlConfig _accessControlConfig;
    private readonly IRepository<KnowledgebaseArticleComment> _articleCommentRepository;
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<KnowledgebaseArticle> _knowledgebaseArticleRepository;
    private readonly IRepository<KnowledgebaseCategory> _knowledgebaseCategoryRepository;
    private readonly IMediator _mediator;

    /// <summary>
    ///     Ctor
    /// </summary>
    public KnowledgebaseService
    (IRepository<KnowledgebaseCategory> knowledgebaseCategoryRepository,
        IRepository<KnowledgebaseArticle> knowledgebaseArticleRepository,
        IRepository<KnowledgebaseArticleComment> articleCommentRepository,
        IMediator mediator,
        ICacheBase cacheBase, AccessControlConfig accessControlConfig)
    {
        _knowledgebaseCategoryRepository = knowledgebaseCategoryRepository;
        _knowledgebaseArticleRepository = knowledgebaseArticleRepository;
        _articleCommentRepository = articleCommentRepository;
        _mediator = mediator;
        _cacheBase = cacheBase;
        _accessControlConfig = accessControlConfig;
    }

    private IQueryable<T> ApplyStandardFilter<T>(IQueryable<T> query, CatalogAccess access)
        where T : BaseEntity, IGroupLinkEntity, IStoreLinkEntity
    {
        ArgumentNullException.ThrowIfNull(access);
        return ApplyStandardFilter(query, access.ShowHidden ? null : access.Customer.GetCustomerGroupIds(),
            access.StoreId);
    }

    //null group ids skip the group filter, an empty store id the store filter
    private IQueryable<T> ApplyStandardFilter<T>(IQueryable<T> query, string[] allowedCustomerGroupsIds,
        string storeId) where T : BaseEntity, IGroupLinkEntity, IStoreLinkEntity
    {
        if (allowedCustomerGroupsIds != null && !_accessControlConfig.IgnoreAcl)
        {
            query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
        }

        if (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations)
        {
            query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(storeId)
                    select p;
        }

        return query;
    }

    private string GetCacheKey(string format, CatalogAccess access, params object[] additionalParams)
    {
        ArgumentNullException.ThrowIfNull(access);
        //groups only matter with the ACL on; "*" keeps reads including hidden records apart from a customer with no groups
        var groupIds = _accessControlConfig.IgnoreAcl ? ""
            : access.ShowHidden ? "*"
            : string.Join(",", access.Customer.GetCustomerGroupIds());
        var standardParams = new[] { groupIds, access.StoreId };

        var allParams = additionalParams != null
            ? standardParams.Concat(additionalParams).ToArray()
            : standardParams;

        return string.Format(format, allParams);
    }

    /// <summary>
    ///     Deletes knowledge base category
    /// </summary>
    /// <param name="kc"></param>
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
    ///     Edits knowledge base category
    /// </summary>
    /// <param name="kc"></param>
    public virtual async Task UpdateKnowledgebaseCategory(KnowledgebaseCategory kc)
    {
        await _knowledgebaseCategoryRepository.UpdateAsync(kc);
        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
        await _mediator.EntityUpdated(kc);
    }

    /// <summary>
    ///     Gets knowledge base category
    /// </summary>
    /// <param name="id"></param>
    /// <returns>knowledge base category</returns>
    public virtual async Task<KnowledgebaseCategory> GetKnowledgebaseCategory(string id)
    {
        return await _knowledgebaseCategoryRepository.GetOneAsync(x => x.Id == id);
    }

    /// <summary>
    ///     Gets knowledge base category
    /// </summary>
    /// <param name="id"></param>
    /// <returns>knowledge base category</returns>
    public virtual async Task<KnowledgebaseCategory> GetPublicKnowledgebaseCategory(string id, CatalogAccess access)
    {
        var key = GetCacheKey(CacheKey.KNOWLEDGEBASE_CATEGORY_BY_ID, access, id);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseCategoryRepository.Table
                        select p;

            query = query.Where(x => x.Published);
            query = query.Where(x => x.Id == id);
            query = ApplyStandardFilter(query, access);
            var toReturn = await _knowledgebaseCategoryRepository.FirstOrDefaultAsync(query);
            return toReturn;
        });
    }

    /// <summary>
    ///     Inserts knowledge base category
    /// </summary>
    /// <param name="kc"></param>
    public virtual async Task InsertKnowledgebaseCategory(KnowledgebaseCategory kc)
    {
        await _knowledgebaseCategoryRepository.InsertAsync(kc);

        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);

        await _mediator.EntityInserted(kc);
    }

    /// <summary>
    ///     Gets knowledge base categories
    /// </summary>
    /// <returns>List of knowledge base categories</returns>
    public virtual async Task<List<KnowledgebaseCategory>> GetKnowledgebaseCategories()
    {
        var categories = _knowledgebaseCategoryRepository.Table.OrderBy(x => x.ParentCategoryId)
            .ThenBy(x => x.DisplayOrder).ToList();
        return await Task.FromResult(categories);
    }

    /// <summary>
    ///     Gets knowledge base article
    /// </summary>
    /// <param name="id"></param>
    /// <returns>knowledge base article</returns>
    public virtual Task<KnowledgebaseArticle> GetKnowledgebaseArticle(string id)
    {
        return _knowledgebaseArticleRepository.GetByIdAsync(id);
    }

    /// <summary>
    ///     Gets every knowledge base article, unpublished and group- or store-limited ones included
    /// </summary>
    /// <returns>List of knowledge base articles</returns>
    public virtual async Task<List<KnowledgebaseArticle>> GetKnowledgebaseArticles()
    {
        var query = from p in _knowledgebaseArticleRepository.Table
                    select p;
        query = query.OrderBy(x => x.DisplayOrder);
        return (await _knowledgebaseArticleRepository.ToListAsync(query)).ToList();
    }

    /// <summary>
    ///     Inserts knowledge base article
    /// </summary>
    /// <param name="ka"></param>
    public virtual async Task InsertKnowledgebaseArticle(KnowledgebaseArticle ka)
    {
        await _knowledgebaseArticleRepository.InsertAsync(ka);
        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
        await _mediator.EntityInserted(ka);
    }

    /// <summary>
    ///     Edits knowledge base article
    /// </summary>
    /// <param name="ka"></param>
    public virtual async Task UpdateKnowledgebaseArticle(KnowledgebaseArticle ka)
    {
        await _knowledgebaseArticleRepository.UpdateAsync(ka);
        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
        await _mediator.EntityUpdated(ka);
    }

    /// <summary>
    ///     Deletes knowledge base article
    /// </summary>
    /// <param name="ka"></param>
    public virtual async Task DeleteKnowledgebaseArticle(KnowledgebaseArticle ka)
    {
        await _knowledgebaseArticleRepository.DeleteAsync(ka);
        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
        await _mediator.EntityDeleted(ka);
    }

    /// <summary>
    ///     Gets knowledge base articles by category id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    public virtual async Task<IPagedList<KnowledgebaseArticle>> GetKnowledgebaseArticlesByCategoryId(string id,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _knowledgebaseArticleRepository.Table.Where(x => x.ParentCategoryId == id)
            .OrderBy(x => x.DisplayOrder);
        return await _knowledgebaseArticleRepository.PagedAsync(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base categories
    /// </summary>
    /// <returns>List of public knowledgebase categories</returns>
    public virtual async Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategories(CatalogAccess access)
    {
        var key = GetCacheKey(CacheKey.KNOWLEDGEBASE_CATEGORIES, access);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseCategoryRepository.Table
                        select p;

            query = query.Where(x => x.Published);

            query = ApplyStandardFilter(query, access);
            query = query.OrderBy(x => x.DisplayOrder);
            return (await _knowledgebaseCategoryRepository.ToListAsync(query)).ToList();
        });
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base articles
    /// </summary>
    /// <returns>List of public knowledge base articles</returns>
    public virtual async Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticles(CatalogAccess access)
    {
        var key = GetCacheKey(CacheKey.ARTICLES, access);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                        select p;

            query = query.Where(x => x.Published);
            query = ApplyStandardFilter(query, access);

            query = query.OrderBy(x => x.DisplayOrder);
            return (await _knowledgebaseArticleRepository.ToListAsync(query)).ToList();
        });
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base articles for an explicit store and customer groups
    /// </summary>
    /// <param name="storeId">Store ident</param>
    /// <param name="customerGroupIds">Customer groups the articles must be visible to</param>
    /// <returns>List of public knowledge base articles</returns>
    public virtual async Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticles(string storeId,
        IList<string> customerGroupIds)
    {
        var query = from p in _knowledgebaseArticleRepository.Table
                    select p;

        query = query.Where(x => x.Published);
        query = ApplyStandardFilter(query, customerGroupIds?.ToArray() ?? [], storeId);

        query = query.OrderBy(x => x.DisplayOrder);
        return (await _knowledgebaseArticleRepository.ToListAsync(query)).ToList();
    }

    /// <summary>
    ///     Gets knowledge base article if it is published etc
    /// </summary>
    /// <returns>knowledge base article</returns>
    public virtual async Task<KnowledgebaseArticle> GetPublicKnowledgebaseArticle(string id, CatalogAccess access)
    {
        var key = GetCacheKey(CacheKey.ARTICLE_BY_ID, access, id);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                        select p;

            query = query.Where(x => x.Published);
            query = query.Where(x => x.Id == id);
            query = ApplyStandardFilter(query, access);
            return await _knowledgebaseArticleRepository.FirstOrDefaultAsync(query);
        });
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base articles for category id
    /// </summary>
    /// <returns>List of public knowledge base articles</returns>
    public virtual async Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByCategory(string categoryId,
        CatalogAccess access)
    {
        var key = GetCacheKey(CacheKey.ARTICLES_BY_CATEGORY_ID, access, categoryId);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                        select p;

            query = query.Where(x => x.Published);
            query = query.Where(x => x.ParentCategoryId == categoryId);

            query = ApplyStandardFilter(query, access);
            query = query.OrderBy(x => x.DisplayOrder);
            return (await _knowledgebaseArticleRepository.ToListAsync(query)).ToList();
        });
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base articles for keyword
    /// </summary>
    /// <returns>List of public knowledge base articles</returns>
    public virtual async Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByKeyword(string keyword,
        CatalogAccess access)
    {
        var key = GetCacheKey(CacheKey.ARTICLES_BY_KEYWORD, access, keyword);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                        select p;

            query = query.Where(x => x.Published);

            query = query.Where(p =>
                p.Locales.Any(x => x.LocaleValue != null && x.LocaleValue.ToLower().Contains(keyword.ToLower()))
                || p.Name.ToLower().Contains(keyword.ToLower()) || p.Content.ToLower().Contains(keyword.ToLower()));

            query = ApplyStandardFilter(query, access);
            query = query.OrderBy(x => x.DisplayOrder);
            return (await _knowledgebaseArticleRepository.ToListAsync(query)).ToList();
        });
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base categories for keyword
    /// </summary>
    /// <returns>List of public knowledge base categories</returns>
    public virtual async Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategoriesByKeyword(string keyword,
        CatalogAccess access)
    {
        var key = GetCacheKey(CacheKey.KNOWLEDGEBASE_CATEGORIES_BY_KEYWORD, access, keyword);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseCategoryRepository.Table
                        select p;

            query = query.Where(x => x.Published);

            query = query.Where(p =>
                p.Locales.Any(x => x.LocaleValue != null && x.LocaleValue.ToLower().Contains(keyword.ToLower()))
                || p.Name.ToLower().Contains(keyword.ToLower()) || p.Description.ToLower().Contains(keyword.ToLower()));

            query = ApplyStandardFilter(query, access);
            query = query.OrderBy(x => x.DisplayOrder);
            return (await _knowledgebaseCategoryRepository.ToListAsync(query)).ToList();
        });
    }

    /// <summary>
    ///     Gets homepage knowledge base articles
    /// </summary>
    /// <returns>List of homepage knowledge base articles</returns>
    public virtual async Task<List<KnowledgebaseArticle>> GetHomepageKnowledgebaseArticles(CatalogAccess access)
    {
        var key = GetCacheKey(CacheKey.HOMEPAGE_ARTICLES, access);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                        select p;

            query = query.Where(x => x.Published);
            query = query.Where(x => x.ShowOnHomepage);
            query = ApplyStandardFilter(query, access);
            query = query.OrderBy(x => x.DisplayOrder);
            return (await _knowledgebaseArticleRepository.ToListAsync(query)).ToList();
        });
    }

    /// <summary>
    ///     Gets knowledge base articles by name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    public virtual async Task<IPagedList<KnowledgebaseArticle>> GetKnowledgebaseArticlesByName(string name,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = from p in _knowledgebaseArticleRepository.Table
                    select p;

        query = query.Where(x => x.Published);

        if (!string.IsNullOrEmpty(name))
            query = query.Where(p =>
                p.Locales.Any(x =>
                    x.LocaleKey == "Name" && x.LocaleValue != null && x.LocaleValue.ToLower().Contains(name.ToLower()))
                || p.Name.ToLower().Contains(name.ToLower()));

        query = query.OrderBy(x => x.DisplayOrder);
        return await _knowledgebaseArticleRepository.PagedAsync(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Gets related knowledge base articles
    /// </summary>
    public virtual async Task<IPagedList<KnowledgebaseArticle>> GetRelatedKnowledgebaseArticles(string articleId,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var article = await GetKnowledgebaseArticle(articleId);
        if (article == null)
            return new PagedList<KnowledgebaseArticle>([], pageIndex, pageSize);

        var relatedIds = article.RelatedArticles.Distinct().ToList();
        var related = relatedIds.Count == 0
            ? []
            : (await _knowledgebaseArticleRepository.ToListAsync(
                _knowledgebaseArticleRepository.Table.Where(x => relatedIds.Contains(x.Id))))
            .ToDictionary(x => x.Id);

        //keep the order the articles were related in; ids of deleted articles are skipped
        var toReturn = article.RelatedArticles.Select(related.GetValueOrDefault).Where(x => x != null);
        return new PagedList<KnowledgebaseArticle>(toReturn, pageIndex, pageSize);
    }

    /// <summary>
    ///     Inserts an article comment
    /// </summary>
    /// <param name="articleComment">Article comment</param>
    public virtual async Task InsertArticleComment(KnowledgebaseArticleComment articleComment)
    {
        ArgumentNullException.ThrowIfNull(articleComment);

        await _articleCommentRepository.InsertAsync(articleComment);

        //event notification
        await _mediator.EntityInserted(articleComment);
    }

    public virtual async Task<IList<KnowledgebaseArticleComment>> GetArticleCommentsByArticleId(string articleId)
    {
        var query = from c in _articleCommentRepository.Table
                    where c.ArticleId == articleId
                    orderby c.CreatedOnUtc
                    select c;
        return await _articleCommentRepository.ToListAsync(query);
    }

    public virtual async Task DeleteArticleComment(KnowledgebaseArticleComment articleComment)
    {
        ArgumentNullException.ThrowIfNull(articleComment);

        await _articleCommentRepository.DeleteAsync(articleComment);
    }

    /// <summary>
    ///     Gets all comments
    /// </summary>
    /// <param name="customerId">Customer identifier; "" to load all records</param>
    /// <returns>Comments</returns>
    public virtual async Task<IList<KnowledgebaseArticleComment>> GetAllComments(string customerId)
    {
        var query = from c in _articleCommentRepository.Table
                    orderby c.CreatedOnUtc
                    where customerId == "" || c.CustomerId == customerId
                    select c;
        return await _articleCommentRepository.ToListAsync(query);
    }

    /// <summary>
    ///     Gets an article comment
    /// </summary>
    /// <param name="articleId">Article identifier</param>
    /// <returns>Article comment</returns>
    public virtual Task<KnowledgebaseArticleComment> GetArticleCommentById(string articleId)
    {
        return _articleCommentRepository.GetByIdAsync(articleId);
    }

    /// <summary>
    ///     Get article comments by identifiers
    /// </summary>
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
        var sortedComments = commentIds.Select(id => comments.FirstOrDefault(x => x.Id == id))
            .Where(comment => comment != null)
            .ToList();

        return await Task.FromResult(sortedComments);
    }
}