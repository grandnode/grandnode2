using Grand.Domain.Catalog;
using Grand.Domain.Common;
using System.Collections.Generic;

namespace Grand.Business.Catalog.Interfaces.Products
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
