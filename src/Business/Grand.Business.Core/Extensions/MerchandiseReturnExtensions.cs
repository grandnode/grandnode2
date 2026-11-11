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
            .Where(x => x.MerchandiseReturnStatus is not (MerchandiseReturnStatus.Rejected
                or MerchandiseReturnStatus.Cancelled))
            .SelectMany(x => x.MerchandiseReturnItems)
            .Where(x => x.OrderItemId == orderItemId)
            .Sum(x => x.Quantity);
    }
}
