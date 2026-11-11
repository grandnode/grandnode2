using Grand.Domain.Orders;

namespace Grand.Business.Core.Extensions;

public static class MerchandiseReturnExtensions
{
    /// <summary>
    ///     The quantity of an order item the merchandise returns take; a rejected or cancelled return takes nothing
    /// </summary>
    public static int ReturnedQuantity(this IEnumerable<MerchandiseReturn> merchandiseReturns, string orderItemId)
    {
        return merchandiseReturns
            .Where(x => x.MerchandiseReturnStatus.TakesQuantity())
            .SelectMany(x => x.MerchandiseReturnItems)
            .Where(x => x.OrderItemId == orderItemId)
            .Sum(x => x.Quantity);
    }

    /// <summary>
    ///     Whether a merchandise return in this status takes its quantity; a rejected or cancelled one takes nothing
    /// </summary>
    public static bool TakesQuantity(this MerchandiseReturnStatus status)
    {
        return status is not (MerchandiseReturnStatus.Rejected or MerchandiseReturnStatus.Cancelled);
    }

    /// <summary>
    ///     The IResourceLock key held while a merchandise return of the order is checked against the quantity left to
    ///     return and saved
    /// </summary>
    public static string QuantityLockKey(string orderId)
    {
        return $"{nameof(MerchandiseReturn)}:{orderId}";
    }
}
