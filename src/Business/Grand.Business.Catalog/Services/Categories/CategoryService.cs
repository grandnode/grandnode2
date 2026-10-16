using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Catalog.Services.Categories;

/// <summary>
///     Category service
/// </summary>
public class CategoryService : ICategoryService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="categoryRepository">Category repository</param>
    /// <param name="mediator">Mediator</param>
    /// <param name="aclService">ACL service</param>
    /// <param name="accessControlConfig"></param>
    public CategoryService(ICacheBase cacheBase,
        IRepository<Category> categoryRepository,
        IMediator mediator,
        IAclService aclService,
        AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _categoryRepository = categoryRepository;
        _mediator = mediator;
        _aclService = aclService;
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    #region Fields

    private readonly IRepository<Category> _categoryRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;
    private readonly IAclService _aclService;
    private readonly AccessControlConfig _accessControlConfig;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all categories
    /// </summary>
    /// <param name="parentId">Parent Id</param>
    /// <param name="categoryName">Category name</param>
    /// <param name="storeId">Store ident</param>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Categories</returns>
    public virtual async Task<IPagedList<Category>> GetAllCategories(string parentId, string categoryName,
        string storeId, Customer customer,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        var query = from c in _categoryRepository.Table
            select c;

        if (!showHidden)
            query = query.Where(c => c.Published);
        if (!string.IsNullOrWhiteSpace(categoryName))
            query = query.Where(m => m.Name.ToLowerInvariant().Contains(categoryName.ToLowerInvariant()));

        if (parentId != null)
            query = query.Where(m => m.ParentCategoryId == parentId);

        if (!_accessControlConfig.IgnoreAcl ||
            (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations))
        {
            if (!showHidden && !_accessControlConfig.IgnoreAcl)
            {
                //Limited to customer group (access control list)
                var allowedCustomerGroupsIds = customer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations)
                //Limited to stores rule
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(storeId)
                    select p;
        }

        query = query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name);

        //pagination
        return await _categoryRepository.PagedAsync(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Gets categories for menu
    /// </summary>
    /// <param name="customer">Customer whose groups limit the result</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <returns>Categories</returns>
    public virtual async Task<IList<Category>> GetMenuCategories(Customer customer, string storeId)
    {
        var query = from c in _categoryRepository.Table
            select c;

        query = query.Where(c => c.Published && c.IncludeInMenu);

        switch (_accessControlConfig.IgnoreAcl)
        {
            case true when
                string.IsNullOrEmpty(storeId) || _accessControlConfig.IgnoreStoreLimitations:
                return await _categoryRepository.ToListAsync(query);
            case false:
            {
                //Limited to customer group (access control list)
                var allowedCustomerGroupsIds = customer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
                break;
            }
        }

        if (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations)
            //Limited to stores rule
            query = from p in query
                where !p.LimitedToStores || p.Stores.Contains(storeId)
                select p;
        return await _categoryRepository.ToListAsync(query);
    }

    /// <summary>
    ///     Gets all categories filtered by parent category identifier
    /// </summary>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="parentCategoryId">Parent category identifier</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <param name="includeAllLevels">A value that indicates if we should load all child levels</param>
    /// <returns>Categories</returns>
    public virtual async Task<IList<Category>> GetAllCategoriesByParentCategoryId(Customer customer, string storeId,
        string parentCategoryId = "",
        bool showHidden = false, bool includeAllLevels = false)
    {
        var categories = await GetChildCategories(parentCategoryId, showHidden, customer, storeId);
        if (!includeAllLevels) return categories;

        //walk the tree outside the cache: every level resolves through its own cache entry, so no
        //semaphore is held while the next level is fetched, and a subtree is stored once rather than
        //once per ancestor
        var allLevels = new List<Category>(categories);
        foreach (var category in categories)
            allLevels.AddRange(await GetAllCategoriesByParentCategoryId(customer, storeId, category.Id, showHidden, true));

        return allLevels;
    }

    /// <summary>
    ///     Gets the direct children of a category, filtered by the given customer's groups and store
    /// </summary>
    private async Task<IList<Category>> GetChildCategories(string parentCategoryId, bool showHidden, Customer customer,
        string storeId)
    {
        //hidden records are not filtered by customer or store, so the key does not depend on them
        var key = string.Format(CacheKey.CATEGORIES_BY_PARENT_CATEGORY_ID_KEY, parentCategoryId, showHidden,
            showHidden ? "" : string.Join(",", customer.GetCustomerGroupIds()), showHidden ? "" : storeId);
        return await _cacheBase.GetAsync(key, () =>
        {
            var query = _categoryRepository.Table.Where(c => c.ParentCategoryId == parentCategoryId);
            if (!showHidden)
                query = query.Where(c => c.Published);

            if (!showHidden && (!_accessControlConfig.IgnoreAcl || !_accessControlConfig.IgnoreStoreLimitations))
            {
                if (!_accessControlConfig.IgnoreAcl)
                {
                    //Limited to customer groups rules
                    var allowedCustomerGroupsIds = customer.GetCustomerGroupIds();
                    query = from p in query
                        where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                        select p;
                }

                if (!_accessControlConfig.IgnoreStoreLimitations)
                    //Limited to stores rules
                    query = from p in query
                        where !p.LimitedToStores || p.Stores.Contains(storeId)
                        select p;
            }

            return Task.FromResult<IList<Category>>(query.OrderBy(x => x.DisplayOrder).ToList());
        });
    }

    /// <summary>
    ///     Gets all categories that should be displayed on the home page
    /// </summary>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Categories</returns>
    public virtual async Task<IList<Category>> GetAllCategoriesDisplayedOnHomePage(Customer customer, string storeId,
        bool showHidden = false)
    {
        //without a customer the ACL check rejects every record, so fail instead of returning nothing
        if (!showHidden) ArgumentNullException.ThrowIfNull(customer);

        var query = _categoryRepository.Table
            .Where(x => x.Published && x.ShowOnHomePage)
            .OrderBy(x => x.DisplayOrder);
        var categories = await _categoryRepository.ToListAsync(query);
        if (!showHidden)
            categories = categories
                .Where(c => _aclService.Authorize(c, customer) &&
                            _aclService.Authorize(c, storeId))
                .ToList();

        return categories;
    }

    /// <summary>
    ///     Gets all featured products from categories that should be displayed on the home page
    /// </summary>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Categories</returns>
    public virtual async Task<IList<Category>> GetAllCategoriesFeaturedProductsOnHomePage(Customer customer, string storeId,
        bool showHidden = false)
    {
        //without a customer the ACL check rejects every record, so fail instead of returning nothing
        if (!showHidden) ArgumentNullException.ThrowIfNull(customer);

        var query = _categoryRepository.Table
            .Where(x => x.Published && x.FeaturedProductsOnHomePage)
            .OrderBy(x => x.DisplayOrder);

        var categories = await _categoryRepository.ToListAsync(query);
        if (!showHidden)
            categories = categories
                .Where(c => _aclService.Authorize(c, customer) &&
                            _aclService.Authorize(c, storeId))
                .ToList();
        return categories;
    }

    /// <summary>
    ///     Gets all categories displayed in the search box
    /// </summary>
    /// <param name="customer">Customer whose groups limit the result</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <returns>Categories</returns>
    public virtual async Task<IList<Category>> GetAllCategoriesSearchBox(Customer customer, string storeId)
    {
        //without a customer the ACL check rejects every record, so fail instead of returning nothing
        ArgumentNullException.ThrowIfNull(customer);

        var query = _categoryRepository.Table
            .Where(x => x.Published && x.ShowOnSearchBox)
            .OrderBy(x => x.SearchBoxDisplayOrder);

        var categories = (await _categoryRepository.ToListAsync(query))
            .Where(c => _aclService.Authorize(c, customer) &&
                        _aclService.Authorize(c, storeId))
            .ToList();

        return categories;
    }

    /// <summary>
    ///     Get category breadcrumb
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Category breadcrumb </returns>
    public virtual async Task<IList<Category>> GetCategoryBreadCrumb(Category category, Customer customer, string storeId,
        bool showHidden = false)
    {
        //without a customer the ACL check rejects every record, so fail instead of returning nothing
        if (!showHidden) ArgumentNullException.ThrowIfNull(customer);

        var result = new List<Category>();

        //used to avoid circular references
        var alreadyProcessedCategoryIds = new List<string>();

        while (category != null && //not null                
               (showHidden || category.Published) && //published
               (showHidden ||
                _aclService.Authorize(category, customer)) && //limited to customer groups
               (showHidden || _aclService.Authorize(category, storeId)) && //limited to store
               !alreadyProcessedCategoryIds.Contains(category.Id))
        {
            result.Add(category);

            alreadyProcessedCategoryIds.Add(category.Id);

            category = await GetCategoryById(category.ParentCategoryId);
        }

        result.Reverse();
        return result;
    }

    /// <summary>
    ///     Get category breadcrumb
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="allCategories">All categories</param>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Category breadcrumb </returns>
    public virtual IList<Category> GetCategoryBreadCrumb(Category category, IList<Category> allCategories,
        Customer customer, string storeId, bool showHidden = false)
    {
        //without a customer the ACL check rejects every record, so fail instead of returning nothing
        if (!showHidden) ArgumentNullException.ThrowIfNull(customer);

        var result = new List<Category>();

        //used to avoid circular references
        var alreadyProcessedCategoryIds = new List<string>();

        while (category != null && //not null                
               (showHidden || category.Published) && //published
               (showHidden ||
                _aclService.Authorize(category, customer)) && //limited to customer groups
               (showHidden || _aclService.Authorize(category, storeId)) && //limited to store
               !alreadyProcessedCategoryIds.Contains(category.Id)) //avoid circular references
        {
            result.Add(category);

            alreadyProcessedCategoryIds.Add(category.Id);

            category = (from c in allCategories
                where c.Id == category.ParentCategoryId
                select c).FirstOrDefault();
        }

        result.Reverse();
        return result;
    }

    /// <summary>
    ///     Get formatted category breadcrumb
    ///     Note: ACL and store acl are ignored
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="separator">Separator</param>
    /// <param name="languageId">Language ID</param>
    /// <returns>Formatted breadcrumb</returns>
    public virtual async Task<string> GetFormattedBreadCrumb(Category category, string separator = ">>",
        string languageId = "")
    {
        var result = string.Empty;

        var breadcrumb = await GetCategoryBreadCrumb(category, customer: null, storeId: "", showHidden: true);
        for (var i = 0; i <= breadcrumb.Count - 1; i++)
        {
            var categoryName = breadcrumb[i].GetTranslation(x => x.Name, languageId);
            result = string.IsNullOrEmpty(result)
                ? categoryName
                : $"{result} {separator} {categoryName}";
        }

        return result;
    }

    /// <summary>
    ///     Get formatted category breadcrumb
    ///     Note: ACL and store acl are ignored
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="allCategories">All categories</param>
    /// <param name="separator">Separator</param>
    /// <param name="languageId">Language ID</param>
    /// <returns>Formatted breadcrumb</returns>
    public virtual string GetFormattedBreadCrumb(Category category,
        IList<Category> allCategories, string separator = ">>", string languageId = "")
    {
        var result = string.Empty;

        var breadcrumb = GetCategoryBreadCrumb(category, allCategories, customer: null, storeId: "", showHidden: true);
        for (var i = 0; i <= breadcrumb.Count - 1; i++)
        {
            var categoryName = breadcrumb[i].GetTranslation(x => x.Name, languageId);
            result = string.IsNullOrEmpty(result)
                ? categoryName
                : $"{result} {separator} {categoryName}";
        }

        return result;
    }

    /// <summary>
    ///     Gets all categories by discount id
    /// </summary>
    /// <returns>Categories</returns>
    public virtual async Task<IList<Category>> GetAllCategoriesByDiscount(string discountId)
    {
        var query = from c in _categoryRepository.Table
            where c.AppliedDiscounts.Any(x => x == discountId)
            select c;

        return await _categoryRepository.ToListAsync(query);
    }

    /// <summary>
    ///     Gets a category
    /// </summary>
    /// <param name="categoryId">Category identifier</param>
    /// <returns>Category</returns>
    public virtual async Task<Category> GetCategoryById(string categoryId)
    {
        var key = string.Format(CacheKey.CATEGORIES_BY_ID_KEY, categoryId);
        return await _cacheBase.GetAsync(key, () => _categoryRepository.GetByIdAsync(categoryId));
    }

    /// <summary>
    ///     Inserts category
    /// </summary>
    /// <param name="category">Category</param>
    public virtual async Task InsertCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        if (string.IsNullOrEmpty(category.ParentCategoryId))
            category.ParentCategoryId = "";

        await _categoryRepository.InsertAsync(category);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.CATEGORIES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(category);
    }

    /// <summary>
    ///     Updates the category
    /// </summary>
    /// <param name="category">Category</param>
    public virtual async Task UpdateCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        if (string.IsNullOrEmpty(category.ParentCategoryId))
            category.ParentCategoryId = "";

        //validate category hierarchy
        var parentCategory = await GetCategoryById(category.ParentCategoryId);
        while (parentCategory != null)
        {
            if (category.Id == parentCategory.Id)
            {
                category.ParentCategoryId = "";
                break;
            }

            parentCategory = await GetCategoryById(parentCategory.ParentCategoryId);
        }

        await _categoryRepository.UpdateAsync(category);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.CATEGORIES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(category);
    }

    /// <summary>
    ///     Delete category
    /// </summary>
    /// <param name="category">Category</param>
    public virtual async Task DeleteCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        //reset a "Parent category" property of all child subcategories
        var subcategories = await _categoryRepository.ToListAsync(_categoryRepository.Table
            .Where(c => c.ParentCategoryId == category.Id)
            .OrderBy(c => c.DisplayOrder));
        foreach (var subcategory in subcategories)
        {
            subcategory.ParentCategoryId = "";
            await UpdateCategory(subcategory);
        }

        await _categoryRepository.DeleteAsync(category);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.CATEGORIES_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(category);
    }

    #endregion
}