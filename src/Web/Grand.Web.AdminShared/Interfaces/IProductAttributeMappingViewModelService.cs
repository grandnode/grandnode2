using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductAttributeMappingViewModelService
{
    Task PrepareProductAttributeValueModel(Product product, ProductModel.ProductAttributeValueModel model);

    Task<ProductModel.ProductAttributeMappingModel> PrepareProductAttributeMappingModel(Product product);

    Task<ProductModel.ProductAttributeMappingModel> PrepareProductAttributeMappingModel(
        ProductModel.ProductAttributeMappingModel model);

    Task<ProductModel.ProductAttributeMappingModel> PrepareProductAttributeMappingModel(Product product,
        ProductAttributeMapping productAttributeMapping);

    Task<IList<ProductModel.ProductAttributeMappingModel>> PrepareProductAttributeMappingModels(Product product);

    Task InsertProductAttributeMappingModel(ProductModel.ProductAttributeMappingModel model);

    Task UpdateProductAttributeMappingModel(ProductModel.ProductAttributeMappingModel model);

    Task<ProductModel.ProductAttributeMappingModel> PrepareProductAttributeMappingModel(
        ProductAttributeMapping productAttributeMapping);

    Task UpdateProductAttributeValidationRulesModel(ProductAttributeMapping productAttributeMapping,
        ProductModel.ProductAttributeMappingModel model);

    Task<ProductAttributeConditionModel> PrepareProductAttributeConditionModel(Product product,
        ProductAttributeMapping productAttributeMapping);

    Task UpdateProductAttributeConditionModel(Product product, ProductAttributeMapping productAttributeMapping,
        ProductAttributeConditionModel model);

    Task<ProductModel.ProductAttributeValueModel> PrepareProductAttributeValueModel(Product product,
        ProductAttributeMapping productAttributeMapping);

    Task<IList<ProductModel.ProductAttributeValueModel>> PrepareProductAttributeValueModels(Product product,
        ProductAttributeMapping productAttributeMapping);

    Task<ProductModel.ProductAttributeValueModel> PrepareProductAttributeValueModel(ProductAttributeMapping pa,
        ProductAttributeValue pav);

    Task InsertProductAttributeValueModel(ProductModel.ProductAttributeValueModel model);

    Task UpdateProductAttributeValueModel(ProductAttributeValue pav, ProductModel.ProductAttributeValueModel model);
}
