using Grand.Domain;
using Grand.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Products
{
    /// <summary>
    /// Product attribute service interface
    /// </summary>
    public partial interface IProductAttributeService
    {
        #region Product attributes

        /// <summary>
        /// Deletes a product attribute
        /// </summary>
        /// <param name="productAttribute">Product attribute</param>
        Task DeleteProductAttribute(ProductAttribute productAttribute);

        /// <summary>
        /// Gets all product attributes
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Product attributes</returns>
        Task<IPagedList<ProductAttribute>> GetAllProductAttributes(int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets a product attribute 
        /// </summary>
        /// <param name="productAttributeId">Product attribute identifier</param>
        /// <returns>Product attribute </returns>
        Task<ProductAttribute> GetProductAttributeById(string productAttributeId);

        /// <summary>
        /// Inserts a product attribute
        /// </summary>
        /// <param name="productAttribute">Product attribute</param>
        Task InsertProductAttribute(ProductAttribute productAttribute);

        /// <summary>
        /// Updates the product attribute
        /// </summary>
        /// <param name="productAttribute">Product attribute</param>
        Task UpdateProductAttribute(ProductAttribute productAttribute);

        #endregion

        #region Product attributes mappings

        
        /// <summary>
        /// Inserts a product attribute mapping
        /// </summary>
        /// <param name="productAttributeMapping">The product attribute mapping</param>
        /// <param name="productId">Product ident</param>
        Task InsertProductAttributeMapping(ProductAttributeMapping productAttributeMapping, string productId);

        /// <summary>
        /// Updates the product attribute mapping
        /// </summary>
        /// <param name="productAttributeMapping">The product attribute mapping</param>
        /// <param name="productId">Product ident</param>
        /// <param name="values">Update values</param>
        Task UpdateProductAttributeMapping(ProductAttributeMapping productAttributeMapping, string productId, bool values = false);

        /// <summary>
        /// Deletes a product attribute mapping
        /// </summary>
        /// <param name="productAttributeMapping">Product attribute mapping</param>
        /// <param name="productId">Product ident</param>
        Task DeleteProductAttributeMapping(ProductAttributeMapping productAttributeMapping, string productId);

        #endregion

        #region Product attribute values

        /// <summary>
        /// Deletes a product attribute value
        /// </summary>
        /// <param name="productAttributeValue">Product attribute value</param>
        /// <param name="productId">Product ident</param>
        /// <param name="productAttributeMappingId">Product attr mapping ident</param>
        Task DeleteProductAttributeValue(ProductAttributeValue productAttributeValue, string productId, string productAttributeMappingId);

        /// <summary>
        /// Inserts a product attribute value
        /// </summary>
        /// <param name="productAttributeValue">The product attribute value</param>
        /// <param name="productId">Product ident</param>
        /// <param name="productAttributeMappingId">Product attr mapping ident</param>
        Task InsertProductAttributeValue(ProductAttributeValue productAttributeValue, string productId, string productAttributeMappingId);

        /// <summary>
        /// Updates the product attribute value
        /// </summary>
        /// <param name="productAttributeValue">The product attribute value</param>
        /// <param name="productId">Product ident</param>
        /// <param name="productAttributeMappingId">Product attr mapping ident</param>
        Task UpdateProductAttributeValue(ProductAttributeValue productAttributeValue, string productId, string productAttributeMappingId);

        #endregion

        #region Predefined product attribute values

        /// <summary>
        /// Gets predefined product attribute values by product attribute identifier
        /// </summary>
        /// <param name="productAttributeId">The product attribute identifier</param>
        /// <returns>Product attribute mapping collection</returns>
        Task<IList<PredefinedProductAttributeValue>> GetPredefinedProductAttributeValues(string productAttributeId);


        #endregion

        #region Product attribute combinations

        /// <summary>
        /// Deletes a product attribute combination
        /// </summary>
        /// <param name="combination">Product attribute combination</param>
        /// <param name="productId">Product ident</param>
        Task DeleteProductAttributeCombination(ProductAttributeCombination combination, string productId);

        /// <summary>
        /// Inserts a product attribute combination
        /// </summary>
        /// <param name="combination">Product attribute combination</param>
        /// <param name="productId">Product ident</param>
        Task InsertProductAttributeCombination(ProductAttributeCombination combination, string productId);

        /// <summary>
        /// Updates a product attribute combination
        /// </summary>
        /// <param name="combination">Product attribute combination</param>
        /// <param name="productId">Product ident</param>
        Task UpdateProductAttributeCombination(ProductAttributeCombination combination, string productId);

        #endregion
    }
}
