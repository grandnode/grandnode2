using Grand.Domain;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Products
{
    /// <summary>
    /// Specification attribute service interface
    /// </summary>
    public interface ISpecificationAttributeService
    {
        #region Specification attribute

        /// <summary>
        /// Gets a specification attribute
        /// </summary>
        /// <param name="specificationAttributeId">The specification attribute identifier</param>
        /// <returns>Specification attribute</returns>
        Task<SpecificationAttribute> GetSpecificationAttributeById(string specificationAttributeId);

        /// <summary>
        /// Gets a specification attribute by se-name
        /// </summary>
        /// <param name="sename">Se-name</param>
        /// <returns>Specification attribute</returns>
        Task<SpecificationAttribute> GetSpecificationAttributeBySeName(string sename);

        /// <summary>
        /// Gets specification attributes
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Specification attributes</returns>
        Task<IPagedList<SpecificationAttribute>> GetSpecificationAttributes(int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Inserts a specification attribute
        /// </summary>
        /// <param name="specificationAttribute">The specification attribute</param>
        Task InsertSpecificationAttribute(SpecificationAttribute specificationAttribute);

        /// <summary>
        /// Updates the specification attribute
        /// </summary>
        /// <param name="specificationAttribute">The specification attribute</param>
        Task UpdateSpecificationAttribute(SpecificationAttribute specificationAttribute);

        /// <summary>
        /// Deletes a specification attribute
        /// </summary>
        /// <param name="specificationAttribute">The specification attribute</param>
        Task DeleteSpecificationAttribute(SpecificationAttribute specificationAttribute);

        #endregion

        #region Specification attribute option

        /// <summary>
        /// Gets a specification attribute option
        /// </summary>
        /// <param name="specificationAttributeOption">The specification attribute option</param>
        /// <returns>Specification attribute option</returns>
        Task<SpecificationAttribute> GetSpecificationAttributeByOptionId(string specificationAttributeOption);

        /// <summary>
        /// Deletes a specification attribute option
        /// </summary>
        /// <param name="specificationAttributeOption">The specification attribute option</param>
        Task DeleteSpecificationAttributeOption(SpecificationAttributeOption specificationAttributeOption);

        #endregion

        #region Product specification attribute

        /// <summary>
        /// Inserts a product specification attribute mapping
        /// </summary>
        /// <param name="productSpecificationAttribute">Product specification attribute mapping</param>
        /// <param name="productId">Product ident</param>
        Task InsertProductSpecificationAttribute(ProductSpecificationAttribute productSpecificationAttribute, string productId);

        /// <summary>
        /// Updates the product specification attribute mapping
        /// </summary>
        /// <param name="productSpecificationAttribute">Product specification attribute mapping</param>
        /// <param name="productId">Product ident</param>
        Task UpdateProductSpecificationAttribute(ProductSpecificationAttribute productSpecificationAttribute, string productId);

        /// <summary>
        /// Deletes a product specification attribute mapping
        /// </summary>
        /// <param name="productSpecificationAttribute">Product specification attribute</param>
        /// <param name="productId">Product ident</param>
        Task DeleteProductSpecificationAttribute(ProductSpecificationAttribute productSpecificationAttribute, string productId);

        /// <summary>
        /// Gets a count of product specification attribute mapping records
        /// </summary>
        /// <param name="productId">Product identifier; "" to load all records</param>
        /// <param name="specificationAttributeOptionId">The specification attribute option identifier; "" to load all records</param>
        /// <returns>Count</returns>
        int GetProductSpecificationAttributeCount(string productId = "", string specificationAttributeOptionId = "");

        #endregion
    }
}
