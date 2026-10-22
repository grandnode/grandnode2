using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductSpecificationViewModelService
{
    //Product specification
    Task<IList<ProductSpecificationAttributeModel>> PrepareProductSpecificationAttributeModel(Product product);

    Task InsertProductSpecificationAttributeModel(ProductModel.AddProductSpecificationAttributeModel model,
        Product product);

    Task UpdateProductSpecificationAttributeModel(ProductSpecificationAttribute psa,
        ProductModel.AddProductSpecificationAttributeModel model);

    Task DeleteProductSpecificationAttribute(Product product, ProductSpecificationAttribute psa);
}
