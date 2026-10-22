using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductAttributeCombinationViewModelService
{
    Task OutOfStockNotifications(Product product, ProductAttributeCombination combination,
        ProductAttributeCombination prevcombination);

    Task PrepareAddProductAttributeCombinationModel(ProductAttributeCombinationModel model, Product product);

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
