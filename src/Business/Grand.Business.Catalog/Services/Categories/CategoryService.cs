using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Security;
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

namespace Grand.Business.Catalog.Services.Categories
{
    /// <summary>
    /// Category service
    /// </summary>
    public partial class CategoryService : ICategoryService
    {
        #region Fields

        private readonly IRepository<Category> _categoryRepository;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;
        private readonly IAclService _aclService;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheBase">Cache manager</param>
        /// <param name="categoryRepository">Category repository</param>
        /// <param name="workContext">Work context</param>
        /// <param name="aclService">ACL service</param>
        public CategoryService(ICacheBase cacheBase,
            IRepository<Category> categoryRepository,
            IWorkContext workContext,
            IMediator mediator,
            IAclService aclService)
        {
            _cacheBase = cacheBase;
            _categoryRepository = categoryRepository;
            _workContext = workContext;
            _mediator = mediator;
            _aclService = aclService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all categories
        /// </summary>
        /// <param name="parentId">Parent Id</param>
        /// <param name="categoryName">Category name</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Categories</returns>
        public virtual async Task<IPagedList<Category>> GetAllCategories(string parentId = null, string categoryName = "", string storeId = "",
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        {
            var query = from c in _categoryRepository.Table
                        select c;

            if (!showHidden)
                query = query.Where(c => c.Published);
            if (!String.IsNullOrWhiteSpace(categoryName))
                query = query.Where(m => m.Name.ToLowerInvariant().Contains(categoryName.ToLowerInvariant()));

            if (parentId != null)
                query = query.Where(m => m.ParentCategoryId == parentId);

            if ((!CommonHelper.IgnoreAcl || (!String.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)))
            {
                if (!showHidden && !CommonHelper.IgnoreAcl)
                {
                    //Limited to customer group (access control list)
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }
                if (!String.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)
                {
                    //Limited to stores rule
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(storeId)
                            select p;
                }
            }

            query = query.OrderBy(c => c.ParentCategoryId).ThenBy(c => c.DisplayOrder).ThenBy(c => c.Name);

            //pagination
            return await Task.FromResult(new PagedList<Category>(query, pageIndex, pageSize));
        }

        /// <summary>
        /// Gets categories for menu
        /// </summary>
        /// <returns>Categories</returns>
        public virtual async Task<IList<Category>> GetMenuCategories()
        {

            var query = from c in _categoryRepository.Table
                        select c;

            query = query.Where(c => c.Published && c.IncludeInMenu);

            if ((!CommonHelper.IgnoreAcl || (!String.IsNullOrEmpty(_workContext.CurrentStore.Id) && !CommonHelper.IgnoreStoreLimitations)))
            {
                if (!CommonHelper.IgnoreAcl)
                {
                    //Limited to customer group (access control list)
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                            where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                            select p;
                }
                if (!String.IsNullOrEmpty(_workContext.CurrentStore.Id) && !CommonHelper.IgnoreStoreLimitations)
                {
                    //Limited to stores rule
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(_workContext.CurrentStore.Id)
                            select p;
                }
            }
            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Gets all categories filtered by parent category identifier
        /// </summary>
        /// <param name="parentCategoryId">Parent category identifier</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <param name="includeAllLevels">A value that indicates if we should load all child levels</param>
        /// <returns>Categories</returns>
        public virtual async Task<IList<Category>> GetAllCategoriesByParentCategoryId(string parentCategoryId = "",
            bool showHidden = false, bool includeAllLevels = false)
        {
            var storeId = _workContext.CurrentStore.Id;
            var customer = _workContext.CurrentCustomer;
            string key = string.Format(CacheKey.CATEGORIES_BY_PARENT_CATEGORY_ID_KEY, parentCategoryId, showHidden, customer.Id, storeId, includeAllLevels);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = _categoryRepository.Table.Where(c => c.ParentCategoryId == parentCategoryId);
                if (!showHidden)
                    query = query.Where(c => c.Published);

                if (!showHidden && (!CommonHelper.IgnoreAcl || !CommonHelper.IgnoreStoreLimitations))
                {
                    if (!showHidden && !CommonHelper.IgnoreAcl)
                    {
                        //Limited to customer groups rules
                        var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                        query = from p in query
                                where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                                select p;
                    }
                    if (!CommonHelper.IgnoreStoreLimitations)
                    {
                        //Limited to stores rules
                        query = from p in query
                                where !p.LimitedToStores || p.Stores.Contains(storeId)
                                select p;
                    }

                }
                var categories = query.OrderBy(x => x.DisplayOrder).ToList();
                if (includeAllLevels)
                {
                    var childCategories = new List<Category>();
                    //add child levels
                    foreach (var category in categories)
                    {
                        childCategories.AddRange(await GetAllCategoriesByParentCategoryId(category.Id, showHidden, includeAllLevels));
                    }
                    categories.AddRange(childCategories);
                }
                return categories;
            });
        }

        /// <summary>
        /// Gets all categories that should be displayed on the home page
        /// </summary>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Categories</returns>
        public virtual async Task<IList<Category>> GetAllCategoriesDisplayedOnHomePage(bool showHidden = false)
        {
            var query = _categoryRepository.Table
                .Where(x => x.Published && x.ShowOnHomePage)
                .OrderBy(x => x.DisplayOrder);
            var categories = await Task.FromResult(query.ToList());
            if (!showHidden)
            {
                categories = categories
                    .Where(c => _aclService.Authorize(c, _workContext.CurrentCustomer) && _aclService.Authorize(c, _workContext.CurrentStore.Id))
                    .ToList();
            }

            return categories;
        }

        /// <summary>
        /// Gets all featured products from categories that should be displayed on the home page
        /// </summary>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Categories</returns>
        public virtual async Task<IList<Category>> GetAllCategoriesFeaturedProductsOnHomePage(bool showHidden = false)
        {
            var query = _categoryRepository.Table
                    .Where(x => x.Published && x.FeaturedProductsOnHomePage)
                    .OrderBy(x => x.DisplayOrder);

            var categories = await Task.FromResult(query.ToList());
            if (!showHidden)
            {
                categories = categories
                    .Where(c => _aclService.Authorize(c, _workContext.CurrentCustomer) && _aclService.Authorize(c, _workContext.CurrentStore.Id))
                    .ToList();
            }
            return categories;
        }

        /// <summary>
        /// Gets all categories displayed in the search box
        /// </summary>
        /// <param name="showHidden">A value that indicates if it should shows hidden records<</param>
        /// <returns>Categories</returns>
        public virtual async Task<IList<Category>> GetAllCategoriesSearchBox()
        {
            var query = _categoryRepository.Table
                .Where(x => x.Published && x.ShowOnSearchBox)
                .OrderBy(x => x.SearchBoxDisplayOrder);

            var categories = (await Task.FromResult(query.ToList()))
                .Where(c => _aclService.Authorize(c, _workContext.CurrentCustomer) && _aclService.Authorize(c, _workContext.CurrentStore.Id))
                .ToList();

            return categories;
        }

        /// <summary>
        /// Get category breadcrumb 
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="categoryService">Category service</param>
        /// <param name="aclService">ACL service</param>
        /// <param name="storeLinkService">Store link service</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Category breadcrumb </returns>
        public virtual async Task<IList<Category>> GetCategoryBreadCrumb(Category category, bool showHidden = false)
        {
            var result = new List<Category>();

            //used to avoid circular references
            var alreadyProcessedCategoryIds = new List<string>();

            while (category != null && //not null                
                (showHidden || category.Published) && //published
                (showHidden || _aclService.Authorize(category, _workContext.CurrentCustomer)) && //limited to customer groups
                (showHidden || _aclService.Authorize(category, _workContext.CurrentStore.Id)) && //limited to store
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
        /// Get category breadcrumb 
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="allCategories">All categories</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Category breadcrumb </returns>
        public virtual IList<Category> GetCategoryBreadCrumb(Category category, IList<Category> allCategories, bool showHidden = false)
        {
            var result = new List<Category>();

            //used to avoid circular references
            var alreadyProcessedCategoryIds = new List<string>();

            while (category != null && //not null                
                (showHidden || category.Published) && //published
                (showHidden || _aclService.Authorize(category, _workContext.CurrentCustomer)) && //limited to customer groups
                (showHidden || _aclService.Authorize(category, _workContext.CurrentStore.Id)) && //limited to store
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
        /// Get formatted category breadcrumb 
        /// Note: ACL and store acl are ignored
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="separator">Separator</param>
        /// <param name="languageId">Language ID</param>
        /// <returns>Formatted breadcrumb</returns>
        public virtual async Task<string> GetFormattedBreadCrumb(Category category, string separator = ">>", string languageId = "")
        {
            string result = string.Empty;

            var breadcrumb = await GetCategoryBreadCrumb(category, true);
            for (int i = 0; i <= breadcrumb.Count - 1; i++)
            {
                var categoryName = breadcrumb[i].GetTranslation(x => x.Name, languageId);
                result = String.IsNullOrEmpty(result)
                    ? categoryName
                    : string.Format("{0} {1} {2}", result, separator, categoryName);
            }

            return result;
        }
        /// <summary>
        /// Get formatted category breadcrumb 
        /// Note: ACL and store acl are ignored
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="allCategories">All categories</param>
        /// <param name="separator">Separator</param>
        /// <param name="languageId">Language ID</param>
        /// <returns>Formatted breadcrumb</returns>
        public virtual string GetFormattedBreadCrumb(Category category,
            IList<Category> allCategories, string separator = ">>", string languageId = "")
        {
            string result = string.Empty;

            var breadcrumb = GetCategoryBreadCrumb(category, allCategories, true);
            for (int i = 0; i <= breadcrumb.Count - 1; i++)
            {
                var categoryName = breadcrumb[i].GetTranslation(x => x.Name, languageId);
                result = String.IsNullOrEmpty(result)
                    ? categoryName
                    : string.Format("{0} {1} {2}", result, separator, categoryName);
            }

            return result;
        }

        /// <summary>
        /// Gets all categories by discount id
        /// </summary>
        /// <returns>Categories</returns>
        public virtual async Task<IList<Category>> GetAllCategoriesByDiscount(string discountId)
        {
            var query = from c in _categoryRepository.Table
                        where c.AppliedDiscounts.Any(x => x == discountId)
                        select c;

            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Gets a category
        /// </summary>
        /// <param name="categoryId">Category identifier</param>
        /// <returns>Category</returns>
        public virtual async Task<Category> GetCategoryById(string categoryId)
        {
            string key = string.Format(CacheKey.CATEGORIES_BY_ID_KEY, categoryId);
            return await _cacheBase.GetAsync(key, () => _categoryRepository.GetByIdAsync(categoryId));
        }

        /// <summary>
        /// Inserts category
        /// </summary>
        /// <param name="category">Category</param>
        public virtual async Task InsertCategory(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            await _categoryRepository.InsertAsync(category);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.CATEGORIES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(category);
        }

        /// <summary>
        /// Updates the category
        /// </summary>
        /// <param name="category">Category</param>
        public virtual async Task UpdateCategory(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (String.IsNullOrEmpty(category.ParentCategoryId))
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
        /// Delete category
        /// </summary>
        /// <param name="category">Category</param>
        public virtual async Task DeleteCategory(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            //reset a "Parent category" property of all child subcategories
            var subcategories = await GetAllCategoriesByParentCategoryId(category.Id, true);
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
}
