using Grand.Domain.Tax;

namespace Grand.Business.Core.Interfaces.Catalog.Tax
{
    /// <summary>
    /// Tax category service interface
    /// </summary>
    public interface ITaxCategoryService
    {
        
        /// <summary>
        /// Gets all tax categories
        /// </summary>
        /// <returns>Tax categories</returns>
        Task<IList<TaxCategory>> GetAllTaxCategories();

        /// <summary>
        /// Gets a tax category
        /// </summary>
        /// <param name="taxCategoryId">Tax category identifier</param>
        /// <returns>Tax category</returns>
        Task<TaxCategory> GetTaxCategoryById(string taxCategoryId);

        /// <summary>
        /// Inserts a tax category
        /// </summary>
        /// <param name="taxCategory">Tax category</param>
        Task InsertTaxCategory(TaxCategory taxCategory);

        /// <summary>
        /// Updates the tax category
        /// </summary>
        /// <param name="taxCategory">Tax category</param>
        Task UpdateTaxCategory(TaxCategory taxCategory);
        /// <summary>
        /// Deletes a tax category
        /// </summary>
        /// <param name="taxCategory">Tax category</param>
        Task DeleteTaxCategory(TaxCategory taxCategory);

    }
}
