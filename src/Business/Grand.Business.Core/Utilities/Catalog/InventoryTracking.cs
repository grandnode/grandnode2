using Grand.Domain.Catalog;

namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     Whether the inventory knows the units of a product: its batches or serial numbers
/// </summary>
public static class InventoryTracking
{
    /// <summary>
    ///     Why a stock write from outside the back office is refused for a product tracking its units
    /// </summary>
    public const string StockDocumentsOnlyMessage =
        "stock of a batch- or serial-tracked product changes through inventory documents";

    /// <summary>
    ///     The product tracks batches or serial numbers; only a product whose stock is tracked can
    /// </summary>
    public static bool Tracked(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return product.InventoryTrackingModeId != InventoryTrackingMode.None &&
               product.ManageInventoryMethodId is ManageInventoryMethod.ManageStock
                   or ManageInventoryMethod.ManageStockByAttributes;
    }

    /// <summary>
    ///     The product's stock changes only through inventory documents and shipments: by the setting, or always for a
    ///     product tracking its units, whose changes must name them
    /// </summary>
    public static bool StockDocumentOnly(Product product, InventorySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.StockChangesOnlyThroughDocuments || Tracked(product);
    }
}
