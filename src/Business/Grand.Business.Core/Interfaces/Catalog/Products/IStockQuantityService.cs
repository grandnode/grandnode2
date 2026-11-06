#nullable enable
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Business.Core.Interfaces.Catalog.Products;

public interface IStockQuantityService
{
    int GetTotalStockQuantity(Product product,
        bool useReservedQuantity = true,
        string warehouseId = "", bool total = false);

    int GetTotalStockQuantityForCombination(Product product, ProductAttributeCombination combination,
        bool useReservedQuantity = true, string warehouseId = "");

    /// <summary>
    ///     Whether the stock covers the quantity, by the rule the shopping cart and the order placement share; a
    ///     bundle is checked by its products
    /// </summary>
    StockCheck CheckStock(Product product, int quantity, IList<CustomAttribute>? attributes, string warehouseId);

    (string resource, object? arg0) FormatStockMessage(Product product, string warehouseId,
        IList<CustomAttribute> attributes);

    /// <summary>
    ///     The stock message together with the availability it describes - one answer, so the
    ///     text a customer reads and the state given to search engines cannot disagree
    /// </summary>
    StockStatus GetStockStatus(Product product, string warehouseId, IList<CustomAttribute> attributes);
}