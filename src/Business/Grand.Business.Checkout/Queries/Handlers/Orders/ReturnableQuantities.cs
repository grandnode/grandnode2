using Grand.Business.Core.Extensions;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

/// <summary>
///     How much of each order item can still be returned, before the products that may not be returned are left out
/// </summary>
internal static class ReturnableQuantities
{
    /// <param name="order">The order</param>
    /// <param name="deliveredShipments">The order's shipments that have been delivered</param>
    /// <param name="merchandiseReturns">The order's merchandise returns</param>
    /// <param name="days">The return period in days; 0 for none</param>
    /// <param name="fromDelivery">Whether the period is counted from each shipment's delivery, not the order date</param>
    /// <param name="utcNow">Now</param>
    public static Dictionary<string, int> Compute(Order order, IReadOnlyCollection<Shipment> deliveredShipments,
        IEnumerable<MerchandiseReturn> merchandiseReturns, int days, bool fromDelivery, DateTime utcNow)
    {
        var quantities = order.OrderItems.ToDictionary(x => x.Id, _ => 0);
        if (days > 0 && !fromDelivery && (utcNow - order.CreatedOnUtc).TotalDays >= days)
            return quantities;

        //each shipment has its own days, counted from its delivery
        var withinPeriod = days > 0 && fromDelivery
            ? deliveredShipments.Where(x => (utcNow - x.DeliveryDateUtc!.Value).TotalDays < days).ToList()
            : deliveredShipments;

        foreach (var item in order.OrderItems)
        {
            //returns do not say which shipment they came from, so the quantity still within its period caps what is
            //left of everything delivered
            var returnable = Math.Min(DeliveredQuantity(withinPeriod, item.Id),
                DeliveredQuantity(deliveredShipments, item.Id) - merchandiseReturns.ReturnedQuantity(item.Id));
            if (returnable > 0)
                quantities[item.Id] = returnable;
        }

        return quantities;
    }

    private static int DeliveredQuantity(IEnumerable<Shipment> shipments, string orderItemId)
    {
        return shipments.SelectMany(x => x.ShipmentItems).Where(x => x.OrderItemId == orderItemId)
            .Sum(x => x.Quantity);
    }
}
