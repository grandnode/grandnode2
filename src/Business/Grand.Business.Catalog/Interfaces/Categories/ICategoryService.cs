using Grand.Domain;
using Grand.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Categories
{
    public partial interface ICategoryService
    {
        /// <summary>
        /// Gets all categories
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="categoryName">Category name</param>
        /// <param name="storeId">Store id</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Categories</returns>
        Task<IPagedList<Category>> GetAllCategories(string parentId = null, string categoryName = "", string storeId = "",
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

        /// <summary>
        /// Gets all categories
        /// </summary>
        /// <returns>Categories</returns>
        Task<IList<Category>> GetMenuCategories();

        /// <summary>
        /// Gets all categories by parent category
        /// </summary>
        /// <param name="parentCategoryId">Parent category id</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <param name="includeAllLevels">A value indicates if we should load all child levels</param>
        /// <returns>Categories</returns>
        Task<IList<Category>> GetAllCategoriesByParentCategoryId(string parentCategoryId = "",
            bool showHidden = false, bool includeAllLevels = false);

        /// <summary>
        /// Gets all categories displayed on the home page
        /// </summary>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Categories</returns>
        Task<IList<Category>> GetAllCategoriesDisplayedOnHomePage(bool showHidden = false);

        /// <summary>
        /// Gets all categories displayed that should be visible on the home page, in featured products section
        /// </summary>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        Task<IList<Category>> GetAllCategoriesFeaturedProductsOnHomePage(bool showHidden = false);

        /// <summary>
        /// Gets all categories that should be displayed in search box
        /// </summary>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Categories</returns>
        Task<IList<Category>> GetAllCategoriesSearchBox();

        /// <summary>
        /// Get category breadcrumb 
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Category breadcrumb </returns>
        Task<IList<Category>> GetCategoryBreadCrumb(Category category, bool showHidden = false);

        /// <summary>
        /// Get category breadcrumb
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="allCategories">All categories</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Category breadcrumb </returns>
        IList<Category> GetCategoryBreadCrumb(Category category, IList<Category> allCategories, bool showHidden = false);

        /// <summary>
        /// Get formatted category breadcrumb
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="separator">Separator</param>
        /// <param name="languageId">Language id for localization</param>
        /// <returns>Formatted breadcrumb</returns>
        Task<string> GetFormattedBreadCrumb(Category category, string separator = ">>", string languageId = "");

        /// <summary>
        /// Get formatted category breadcrumb
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="allCategories">All categories</param>
        /// <param name="separator">Separator</param>
        /// <param name="languageId">Language id for localization</param>
        /// <returns>Formatted breadcrumb</returns>
        string GetFormattedBreadCrumb(Category category,
            IList<Category> allCategories, string separator = ">>", string languageId = "");

        /// <summary>
        /// Gets all categories filtered by discount id
        /// </summary>
        /// <returns>Categories</returns>
        Task<IList<Category>> GetAllCategoriesByDiscount(string discountId);

        /// <summary>
        /// Gets a category
        /// </summary>
        /// <param name="categoryId">Category identifier</param>
        /// <returns>Category</returns>
        Task<Category> GetCategoryById(string categoryId);

        /// <summary>
        /// Inserts the new category
        /// </summary>
        /// <param name="category">Category</param>
        Task InsertCategory(Category category);

        /// <summary>
        /// Updates the existing category
        /// </summary>
        /// <param name="category">Category</param>
        Task UpdateCategory(Category category);

        /// <summary>
        /// Deletes the existing category
        /// </summary>
        /// <param name="category">Category</param>
        Task DeleteCategory(Category category);

    }
}
