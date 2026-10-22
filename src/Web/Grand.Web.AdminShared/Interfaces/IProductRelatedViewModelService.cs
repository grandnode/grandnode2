using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductRelatedViewModelService
{
    Task InsertRelatedProductModel(ProductModel.AddRelatedProductModel model);

    Task UpdateRelatedProductModel(ProductModel.RelatedProductModel model);

    Task DeleteRelatedProductModel(ProductModel.RelatedProductModel model);

    Task InsertSimilarProductModel(ProductModel.AddSimilarProductModel model);

    Task UpdateSimilarProductModel(ProductModel.SimilarProductModel model);

    Task DeleteSimilarProductModel(ProductModel.SimilarProductModel model);

    Task InsertCrossSellProductModel(ProductModel.AddCrossSellProductModel model);

    Task DeleteCrossSellProduct(string productId, string crossSellProductId);

    Task InsertRecommendedProductModel(ProductModel.AddRecommendedProductModel model);

    Task DeleteRecommendedProduct(string productId, string recommendedProductId);
}
