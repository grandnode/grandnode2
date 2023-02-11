using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Products
{
    /// <summary>
    /// Copy product service
    /// </summary>
    public interface ICopyProductService
    {
        /// <summary>
        /// Create a copy of product with all depended data
        /// </summary>
        /// <param name="product">The product to copy</param>
        /// <param name="newName">The name of product duplicate</param>
        /// <param name="isPublished">A value indicating whether the product duplicate should be published</param>
        /// <param name="copyAssociatedProducts">A value indicating whether the copy associated products</param>
        /// <returns>Product copy</returns>
        Task<Product> CopyProduct(Product product, string newName,
            bool isPublished = true, bool copyAssociatedProducts = true);
    }
}
