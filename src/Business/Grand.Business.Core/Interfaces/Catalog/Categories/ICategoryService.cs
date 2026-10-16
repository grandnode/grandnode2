using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Catalog.Categories;

public interface ICategoryService
{
    /// <summary>
    ///     Gets all categories
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="categoryName">Category name</param>
    /// <param name="storeId">Store id</param>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Categories</returns>
    Task<IPagedList<Category>> GetAllCategories(string parentId, string categoryName, string storeId, Customer customer,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

    /// <summary>
    ///     Gets all categories
    /// </summary>
    /// <param name="customer">Customer whose groups limit the result</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <returns>Categories</returns>
    Task<IList<Category>> GetMenuCategories(Customer customer, string storeId);

    /// <summary>
    ///     Gets all categories by parent category
    /// </summary>
    /// <param name="customer">Customer whose groups limit the result and key the cache</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="parentCategoryId">Parent category id</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <param name="includeAllLevels">A value indicates if we should load all child levels</param>
    /// <returns>Categories</returns>
    Task<IList<Category>> GetAllCategoriesByParentCategoryId(Customer customer, string storeId, string parentCategoryId = "",
        bool showHidden = false, bool includeAllLevels = false);

    /// <summary>
    ///     Gets all categories displayed on the home page
    /// </summary>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Categories</returns>
    Task<IList<Category>> GetAllCategoriesDisplayedOnHomePage(Customer customer, string storeId, bool showHidden = false);

    /// <summary>
    ///     Gets all categories displayed that should be visible on the home page, in featured products section
    /// </summary>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    Task<IList<Category>> GetAllCategoriesFeaturedProductsOnHomePage(Customer customer, string storeId,
        bool showHidden = false);

    /// <summary>
    ///     Gets all categories that should be displayed in search box
    /// </summary>
    /// <param name="customer">Customer whose groups limit the result</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <returns>Categories</returns>
    Task<IList<Category>> GetAllCategoriesSearchBox(Customer customer, string storeId);

    /// <summary>
    ///     Get category breadcrumb
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Category breadcrumb </returns>
    Task<IList<Category>> GetCategoryBreadCrumb(Category category, Customer customer, string storeId,
        bool showHidden = false);

    /// <summary>
    ///     Get category breadcrumb
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="allCategories">All categories</param>
    /// <param name="customer">Customer whose groups limit the result (not read when showHidden is true)</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Category breadcrumb </returns>
    IList<Category> GetCategoryBreadCrumb(Category category, IList<Category> allCategories, Customer customer,
        string storeId, bool showHidden = false);

    /// <summary>
    ///     Get formatted category breadcrumb
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="separator">Separator</param>
    /// <param name="languageId">Language id for localization</param>
    /// <returns>Formatted breadcrumb</returns>
    Task<string> GetFormattedBreadCrumb(Category category, string separator = ">>", string languageId = "");

    /// <summary>
    ///     Get formatted category breadcrumb
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="allCategories">All categories</param>
    /// <param name="separator">Separator</param>
    /// <param name="languageId">Language id for localization</param>
    /// <returns>Formatted breadcrumb</returns>
    string GetFormattedBreadCrumb(Category category,
        IList<Category> allCategories, string separator = ">>", string languageId = "");

    /// <summary>
    ///     Gets all categories filtered by discount id
    /// </summary>
    /// <returns>Categories</returns>
    Task<IList<Category>> GetAllCategoriesByDiscount(string discountId);

    /// <summary>
    ///     Gets a category
    /// </summary>
    /// <param name="categoryId">Category identifier</param>
    /// <returns>Category</returns>
    Task<Category> GetCategoryById(string categoryId);

    /// <summary>
    ///     Inserts the new category
    /// </summary>
    /// <param name="category">Category</param>
    Task InsertCategory(Category category);

    /// <summary>
    ///     Updates the existing category
    /// </summary>
    /// <param name="category">Category</param>
    Task UpdateCategory(Category category);

    /// <summary>
    ///     Deletes the existing category
    /// </summary>
    /// <param name="category">Category</param>
    Task DeleteCategory(Category category);
}