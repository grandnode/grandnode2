using Grand.Business.Core.Interfaces.Cms;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Cms.Services;

public class KnowledgebaseService : IKnowledgebaseService
{
    private readonly AccessControlConfig _accessControlConfig;
    private readonly IRepository<KnowledgebaseArticleComment> _articleCommentRepository;
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<KnowledgebaseArticle> _knowledgebaseArticleRepository;
    private readonly IRepository<KnowledgebaseCategory> _knowledgebaseCategoryRepository;
    private readonly IMediator _mediator;
    private readonly IWorkContext _workContext;

    /// <summary>
    ///     Ctor
    /// </summary>
    public KnowledgebaseService
    (IRepository<KnowledgebaseCategory> knowledgebaseCategoryRepository,
        IRepository<KnowledgebaseArticle> knowledgebaseArticleRepository,
        IRepository<KnowledgebaseArticleComment> articleCommentRepository,
        IMediator mediator,
        IWorkContext workContext,
        ICacheBase cacheBase, AccessControlConfig accessControlConfig)
    {
        _knowledgebaseCategoryRepository = knowledgebaseCategoryRepository;
        _knowledgebaseArticleRepository = knowledgebaseArticleRepository;
        _articleCommentRepository = articleCommentRepository;
        _mediator = mediator;
        _workContext = workContext;
        _cacheBase = cacheBase;
        _accessControlConfig = accessControlConfig;
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
    public virtual async Task<KnowledgebaseCategory> GetPublicKnowledgebaseCategory(string id)
    {
        var key = string.Format(CacheKey.KNOWLEDGEBASE_CATEGORY_BY_ID, id,
            _workContext.CurrentCustomer.GetCustomerGroupIds(),
            _workContext.CurrentStore.Id);
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseCategoryRepository.Table
                select p;

            query = query.Where(x => x.Published);
            query = query.Where(x => x.Id == id);

            if (!_accessControlConfig.IgnoreAcl)
            {
                //Limited to customer groups rules
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!_accessControlConfig.IgnoreStoreLimitations)
                //Store acl
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                    select p;

            var toReturn = await Task.FromResult(query.FirstOrDefault());
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
    ///     Gets knowledge base articles
    /// </summary>
    /// <returns>List of knowledge base articles</returns>
    /// <param name="storeId">Store ident</param>
    public virtual async Task<List<KnowledgebaseArticle>> GetKnowledgebaseArticles(string storeId = "")
    {
        var query = from p in _knowledgebaseArticleRepository.Table
            select p;
        if (!_accessControlConfig.IgnoreAcl)
        {
            var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
            query = from p in query
                where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                select p;
        }

        if (!_accessControlConfig.IgnoreStoreLimitations && !string.IsNullOrEmpty(storeId))
            //Limited to stores rules
            query = from p in query
                where !p.LimitedToStores || p.Stores.Contains(storeId)
                select p;
        query = query.OrderBy(x => x.DisplayOrder);
        return await Task.FromResult(query.ToList());
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
        var articles = await Task.FromResult(_knowledgebaseArticleRepository.Table.Where(x => x.ParentCategoryId == id)
            .OrderBy(x => x.DisplayOrder).ToList());
        return new PagedList<KnowledgebaseArticle>(articles, pageIndex, pageSize);
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base categories
    /// </summary>
    /// <returns>List of public knowledgebase categories</returns>
    public virtual async Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategories()
    {
        var key = string.Format(CacheKey.KNOWLEDGEBASE_CATEGORIES,
            string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
            _workContext.CurrentStore.Id);
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseCategoryRepository.Table
                select p;

            query = query.Where(x => x.Published);

            if (!_accessControlConfig.IgnoreAcl)
            {
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!_accessControlConfig.IgnoreStoreLimitations)
                //Store acl
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                    select p;
            query = query.OrderBy(x => x.DisplayOrder);
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base articles
    /// </summary>
    /// <returns>List of public knowledge base articles</returns>
    public virtual async Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticles()
    {
        var key = string.Format(CacheKey.ARTICLES, string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
            _workContext.CurrentStore.Id);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                select p;

            query = query.Where(x => x.Published);

            if (!_accessControlConfig.IgnoreAcl)
            {
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!_accessControlConfig.IgnoreStoreLimitations)
                //Store acl
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                    select p;

            query = query.OrderBy(x => x.DisplayOrder);
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Gets knowledge base article if it is published etc
    /// </summary>
    /// <returns>knowledge base article</returns>
    public virtual async Task<KnowledgebaseArticle> GetPublicKnowledgebaseArticle(string id)
    {
        var key = string.Format(CacheKey.ARTICLE_BY_ID, id,
            string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
            _workContext.CurrentStore.Id);
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                select p;

            query = query.Where(x => x.Published);
            query = query.Where(x => x.Id == id);

            if (!_accessControlConfig.IgnoreAcl)
            {
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!_accessControlConfig.IgnoreStoreLimitations)
                //Store acl
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                    select p;

            return await Task.FromResult(query.FirstOrDefault());
        });
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base articles for category id
    /// </summary>
    /// <returns>List of public knowledge base articles</returns>
    public virtual async Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByCategory(string categoryId)
    {
        var key = string.Format(CacheKey.ARTICLES_BY_CATEGORY_ID, categoryId,
            string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
            _workContext.CurrentStore.Id);
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                select p;

            query = query.Where(x => x.Published);
            query = query.Where(x => x.ParentCategoryId == categoryId);

            if (!_accessControlConfig.IgnoreAcl)
            {
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!_accessControlConfig.IgnoreStoreLimitations)
                //Store acl
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                    select p;
            query = query.OrderBy(x => x.DisplayOrder);
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base articles for keyword
    /// </summary>
    /// <returns>List of public knowledge base articles</returns>
    public virtual async Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByKeyword(string keyword)
    {
        var key = string.Format(CacheKey.ARTICLES_BY_KEYWORD, keyword,
            string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
            _workContext.CurrentStore.Id);

        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                select p;

            query = query.Where(x => x.Published);

            query = query.Where(p =>
                p.Locales.Any(x => x.LocaleValue != null && x.LocaleValue.ToLower().Contains(keyword.ToLower()))
                || p.Name.ToLower().Contains(keyword.ToLower()) || p.Content.ToLower().Contains(keyword.ToLower()));

            if (!_accessControlConfig.IgnoreAcl)
            {
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!_accessControlConfig.IgnoreStoreLimitations)
                //Store acl
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                    select p;
            query = query.OrderBy(x => x.DisplayOrder);
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Gets public(published etc) knowledge base categories for keyword
    /// </summary>
    /// <returns>List of public knowledge base categories</returns>
    public virtual async Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategoriesByKeyword(string keyword)
    {
        var key = string.Format(CacheKey.KNOWLEDGEBASE_CATEGORIES_BY_KEYWORD, keyword,
            string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
            _workContext.CurrentStore.Id);
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseCategoryRepository.Table
                select p;

            query = query.Where(x => x.Published);

            query = query.Where(p =>
                p.Locales.Any(x => x.LocaleValue != null && x.LocaleValue.ToLower().Contains(keyword.ToLower()))
                || p.Name.ToLower().Contains(keyword.ToLower()) || p.Description.ToLower().Contains(keyword.ToLower()));

            if (!_accessControlConfig.IgnoreAcl)
            {
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!_accessControlConfig.IgnoreStoreLimitations)
                //Store acl
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                    select p;
            query = query.OrderBy(x => x.DisplayOrder);
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Gets homepage knowledge base articles
    /// </summary>
    /// <returns>List of homepage knowledge base articles</returns>
    public virtual async Task<List<KnowledgebaseArticle>> GetHomepageKnowledgebaseArticles()
    {
        var key = string.Format(CacheKey.HOMEPAGE_ARTICLES,
            string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
            _workContext.CurrentStore.Id);
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _knowledgebaseArticleRepository.Table
                select p;

            query = query.Where(x => x.Published);
            query = query.Where(x => x.ShowOnHomepage);

            if (!_accessControlConfig.IgnoreAcl)
            {
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!_accessControlConfig.IgnoreStoreLimitations)
                //Store acl
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                    select p;
            query = query.OrderBy(x => x.DisplayOrder);
            return await Task.FromResult(query.ToList());
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
        var toReturn = await Task.FromResult(query.ToList());

        return new PagedList<KnowledgebaseArticle>(toReturn, pageIndex, pageSize);
    }

    /// <summary>
    ///     Gets related knowledge base articles
    /// </summary>
    public virtual async Task<IPagedList<KnowledgebaseArticle>> GetRelatedKnowledgebaseArticles(string articleId,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var article = await GetKnowledgebaseArticle(articleId);
        var toReturn = new List<KnowledgebaseArticle>();

        foreach (var id in article.RelatedArticles)
        {
            var relatedArticle = await GetKnowledgebaseArticle(id);
            if (relatedArticle != null)
                toReturn.Add(relatedArticle);
        }

        return new PagedList<KnowledgebaseArticle>(toReturn.ToList(), pageIndex, pageSize);
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
        return await Task.FromResult(query.ToList());
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
        return await Task.FromResult(query.ToList());
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
        var sortedComments = new List<KnowledgebaseArticleComment>();
        foreach (var id in commentIds)
        {
            var comment = comments.Find(x => x.Id == id);
            if (comment != null)
                sortedComments.Add(comment);
        }

        return await Task.FromResult(sortedComments);
    }
}