using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Categories
{
    public interface ICategoryLayoutService
    {
        
        /// <summary>
        /// Gets all available category layouts
        /// </summary>
        /// <returns>Category layouts</returns>
        Task<IList<CategoryLayout>> GetAllCategoryLayouts();

        /// <summary>
        /// Gets a category layout
        /// </summary>
        /// <param name="categoryLayoutId">Category layout id</param>
        /// <returns>Category layout</returns>
        Task<CategoryLayout> GetCategoryLayoutById(string categoryLayoutId);

        /// <summary>
        /// Inserts new category layout
        /// </summary>
        /// <param name="categoryLayout">Category layout</param>
        Task InsertCategoryLayout(CategoryLayout categoryLayout);

        /// <summary>
        /// Updates the existing category layout
        /// </summary>
        /// <param name="categoryLayout">Category layout</param>
        Task UpdateCategoryLayout(CategoryLayout categoryLayout);

        /// <summary>
        /// Deletes existing category layouts
        /// </summary>
        /// <param name="categoryLayout">Category layout</param>
        Task DeleteCategoryLayout(CategoryLayout categoryLayout);

    }
}
