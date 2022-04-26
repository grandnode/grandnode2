using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Business.Core.Interfaces.Catalog.Products
{
    public interface IStockQuantityService
    {
        int GetTotalStockQuantity(Product product,
            bool useReservedQuantity = true,
            string warehouseId = "", bool total = false);

        int GetTotalStockQuantityForCombination(Product product, ProductAttributeCombination combination,
            bool useReservedQuantity = true, string warehouseId = "");

        string FormatStockMessage(Product product, string warehouseId, IList<CustomAttribute> attributes);
    }
}
