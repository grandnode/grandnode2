using Grand.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Products
{
    /// <summary>
    /// Product layout interface
    /// </summary>
    public partial interface IProductLayoutService
    {
       
        /// <summary>
        /// Gets all product layout
        /// </summary>
        /// <returns>Product layouts</returns>
        Task<IList<ProductLayout>> GetAllProductLayouts();

        /// <summary>
        /// Gets a product layout
        /// </summary>
        /// <param name="productLayoutId">Product layout identifier</param>
        /// <returns>Product layout</returns>
        Task<ProductLayout> GetProductLayoutById(string productLayoutId);

        /// <summary>
        /// Inserts product layout
        /// </summary>
        /// <param name="productLayout">Product layout</param>
        Task InsertProductLayout(ProductLayout productLayout);

        /// <summary>
        /// Updates the product layout
        /// </summary>
        /// <param name="productLayout">Product layout</param>
        Task UpdateProductLayout(ProductLayout productLayout);

        /// <summary>
        /// Delete product layout
        /// </summary>
        /// <param name="productLayout">Product layout</param>
        Task DeleteProductLayout(ProductLayout productLayout);

    }
}
