using Grand.Domain.Orders;

namespace Grand.Business.Core.Extensions;

public static class MerchandiseReturnShippingExtensions
{
    /// <summary>
    ///     What the customer pays to ship a merchandise return: nothing when the store's fees are free, when any
    ///     reason ships free, or when every action gives no refund; otherwise the cost of the chosen option
    /// </summary>
    public static double CustomerCost(double optionCost, MerchandiseReturnFees fees, bool anyFreeReason,
        bool allWithoutRefund)
    {
        if (fees == MerchandiseReturnFees.Free) return 0;
        if (anyFreeReason) return 0;
        if (allWithoutRefund) return 0;
        return optionCost;
    }
}
