using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductViewModelService
{
    Task PrepareProductModel(ProductModel model, Product product, bool setPredefinedValues, bool excludeProperties);

    Task PrepareProductReviewModel(ProductReviewModel model, ProductReview productReview, bool excludeProperties,
        bool formatReviewText);

    Task OutOfStockNotifications(Product product, int prevStockQuantity,
        List<ProductWarehouseInventory> prevMultiWarehouseStock);

    Task OutOfStockNotifications(Product product, ProductAttributeCombination combination,
        ProductAttributeCombination prevcombination);

    Task PrepareAddProductAttributeCombinationModel(ProductAttributeCombinationModel model, Product product);
    Task SaveProductWarehouseInventory(Product product, IList<ProductModel.ProductWarehouseInventoryModel> model);
    Task<ProductListModel> PrepareProductListModel();

    Task<(IEnumerable<ProductModel> productModels, int totalCount)> PrepareProductsModel(ProductListModel model,
        int pageIndex, int pageSize);

    Task<IList<Product>> PrepareProducts(ProductListModel model);
    Task<Product> InsertProductModel(ProductModel model);
    Task<Product> UpdateProductModel(Product product, ProductModel model);
    Task DeleteProduct(Product product);
    Task DeleteSelected(IEnumerable<string> selectedIds);
    Task<ProductModel.AddRequiredProductModel> PrepareAddRequiredProductModel();

    Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(ProductModel.AddProductModel model,
        int pageIndex, int pageSize);

    Task<ProductModel.AddRelatedProductModel> PrepareRelatedProductModel();
    Task<ProductModel.AddSimilarProductModel> PrepareSimilarProductModel();
    Task<ProductModel.AddBundleProductModel> PrepareBundleProductModel();
    Task<ProductModel.AddCrossSellProductModel> PrepareCrossSellProductModel();
    Task<ProductModel.AddRecommendedProductModel> PrepareRecommendedProductModel();
    Task<ProductModel.AddAssociatedProductModel> PrepareAssociatedProductModel();
    Task<BulkEditListModel> PrepareBulkEditListModel();

    Task<(IEnumerable<BulkEditProductModel> bulkEditProductModels, int totalCount)> PrepareBulkEditProductModel(
        BulkEditListModel model, int pageIndex, int pageSize);

    Task UpdateBulkEdit(IEnumerable<BulkEditProductModel> products);

    Task InsertBulkEdit(IEnumerable<BulkEditProductModel> products);

    Task DeleteBulkEdit(IEnumerable<BulkEditProductModel> products);

    Task<ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel>
        PrepareAssociateProductToAttributeValueModel();

    Task<IList<ProductModel.ProductAttributeCombinationModel>> PrepareProductAttributeCombinationModel(Product product);

    Task<ProductAttributeCombinationModel> PrepareProductAttributeCombinationModel(Product product,
        string combinationId);

    Task<IList<string>> InsertOrUpdateProductAttributeCombinationPopup(Product product,
        ProductAttributeCombinationModel model);

    Task GenerateAllAttributeCombinations(Product product);

    Task ClearAllAttributeCombinations(Product product);

    Task<IList<ProductModel.ProductAttributeCombinationTierPricesModel>>
        PrepareProductAttributeCombinationTierPricesModel(Product product, string productAttributeCombinationId);

    Task InsertProductAttributeCombinationTierPricesModel(Product product,
        ProductAttributeCombination productAttributeCombination,
        ProductModel.ProductAttributeCombinationTierPricesModel model);

    Task UpdateProductAttributeCombinationTierPricesModel(Product product,
        ProductAttributeCombination productAttributeCombination,
        ProductModel.ProductAttributeCombinationTierPricesModel model);

    Task DeleteProductAttributeCombinationTierPrices(Product product,
        ProductAttributeCombination productAttributeCombination, ProductCombinationTierPrices tierPrice);

}