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

    /// <summary>
    ///     The search of a product popup outside the product screens, with the stores and vendors of the panel
    /// </summary>
    Task<T> PrepareAddProductModel<T>() where T : ProductModel.AddProductModel, new();
}
