using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductViewModelService
{
    Task PrepareProductModel(ProductModel model, Product product, bool setPredefinedValues, bool excludeProperties);

    Task PrepareProductReviewModel(ProductReviewModel model, ProductReview productReview, bool excludeProperties,
        bool formatReviewText);

    /// <summary>
    ///     The error to show when the form changes stock while stock changes only through inventory documents; null
    ///     when the change is allowed
    /// </summary>
    Task<string> StockChangeRefused(ProductModel model);

    Task SaveProductWarehouseInventory(Product product, IList<ProductModel.ProductWarehouseInventoryModel> model);
    Task<ProductListModel> PrepareProductListModel();

    Task<(IEnumerable<ProductModel> productModels, int totalCount)> PrepareProductsModel(ProductListModel model,
        int pageIndex, int pageSize);

    Task<IList<Product>> PrepareProducts(ProductListModel model);
    Task<Product> InsertProductModel(ProductModel model);
    Task<Product> UpdateProductModel(Product product, ProductModel model);
    Task DeleteProduct(Product product);
    Task DeleteSelected(IEnumerable<string> selectedIds);
    Task<BulkEditListModel> PrepareBulkEditListModel();

    Task<(IEnumerable<BulkEditProductModel> bulkEditProductModels, int totalCount)> PrepareBulkEditProductModel(
        BulkEditListModel model, int pageIndex, int pageSize);

    Task UpdateBulkEdit(IEnumerable<BulkEditProductModel> products);

    Task InsertBulkEdit(IEnumerable<BulkEditProductModel> products);

    Task DeleteBulkEdit(IEnumerable<BulkEditProductModel> products);
}