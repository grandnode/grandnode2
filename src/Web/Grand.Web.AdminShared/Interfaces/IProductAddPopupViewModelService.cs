using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductAddPopupViewModelService
{
    Task<ProductModel.AddRequiredProductModel> PrepareAddRequiredProductModel();
    Task<ProductModel.AddRelatedProductModel> PrepareRelatedProductModel();
    Task<ProductModel.AddSimilarProductModel> PrepareSimilarProductModel();
    Task<ProductModel.AddBundleProductModel> PrepareBundleProductModel();
    Task<ProductModel.AddCrossSellProductModel> PrepareCrossSellProductModel();
    Task<ProductModel.AddRecommendedProductModel> PrepareRecommendedProductModel();
    Task<ProductModel.AddAssociatedProductModel> PrepareAssociatedProductModel();

    Task<ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel>
        PrepareAssociateProductToAttributeValueModel();

    Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(ProductModel.AddProductModel model,
        int pageIndex, int pageSize);
}
