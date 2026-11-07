using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     The stock change a back-office form asks for: only the values the user changed since the form was shown, so a
///     form left open never undoes what an order did meanwhile
/// </summary>
public static class StockFormChange
{
    /// <returns>The change, or null when neither the stock nor the reserved quantity changed</returns>
    public static StockChange Of(IList<CustomAttribute> attributes, string warehouseId, int stock, int originalStock,
        int reserved, int originalReserved, StockSource source)
    {
        var stockChanged = stock != originalStock;
        var reservedChanged = reserved != originalReserved;
        if (!stockChanged && !reservedChanged)
            return null;

        return new StockChange(attributes, warehouseId, stockChanged ? stock : 0, stockChanged,
            InventoryMovementType.ManualEdit, source) { ReservedQuantity = reservedChanged ? reserved : null };
    }
}
