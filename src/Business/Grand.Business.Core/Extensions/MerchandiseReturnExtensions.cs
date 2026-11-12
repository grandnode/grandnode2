using Grand.Domain.Customers;
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

    //the status flow: a rejected or cancelled return may be opened again, a completed one is final
    private static readonly Dictionary<MerchandiseReturnStatus, MerchandiseReturnStatus[]> Transitions = new() {
        [MerchandiseReturnStatus.Pending] = [
            MerchandiseReturnStatus.Accepted, MerchandiseReturnStatus.Received, MerchandiseReturnStatus.Rejected,
            MerchandiseReturnStatus.Cancelled
        ],
        [MerchandiseReturnStatus.Accepted] = [
            MerchandiseReturnStatus.Received, MerchandiseReturnStatus.Completed, MerchandiseReturnStatus.Rejected,
            MerchandiseReturnStatus.Cancelled
        ],
        [MerchandiseReturnStatus.Received] = [MerchandiseReturnStatus.Completed],
        [MerchandiseReturnStatus.Rejected] = [MerchandiseReturnStatus.Pending],
        [MerchandiseReturnStatus.Cancelled] = [MerchandiseReturnStatus.Pending],
        [MerchandiseReturnStatus.Completed] = []
    };

    /// <summary>
    ///     The statuses a merchandise return in this status may change to
    /// </summary>
    public static IReadOnlyList<MerchandiseReturnStatus> NextStatuses(this MerchandiseReturnStatus status)
    {
        return Transitions.GetValueOrDefault(status, []);
    }

    /// <summary>
    ///     Whether a merchandise return in this status may change to the other one
    /// </summary>
    public static bool CanChangeTo(this MerchandiseReturnStatus from, MerchandiseReturnStatus to)
    {
        return from.NextStatuses().Contains(to);
    }

    /// <summary>
    ///     How the staff note of a status change names the staff member who made it: the e-mail, else the full name,
    ///     else the customer id; null when no one did, and the note says the system
    /// </summary>
    public static string StatusChangedBy(this Customer staff)
    {
        if (staff == null)
            return null;
        if (!string.IsNullOrWhiteSpace(staff.Email))
            return staff.Email;

        var fullName = staff.GetFullName();
        return !string.IsNullOrWhiteSpace(fullName) ? fullName : $"staff ({staff.Id})";
    }

    /// <summary>
    ///     The IResourceLock key held while a merchandise return of the order is checked against the quantity left to
    ///     return and saved
    /// </summary>
    public static string QuantityLockKey(string orderId)
    {
        return $"{nameof(MerchandiseReturn)}:{orderId}";
    }

    /// <summary>
    ///     The IResourceLock key held while the return label of a merchandise return is requested from the provider
    /// </summary>
    public static string LabelLockKey(string merchandiseReturnId)
    {
        return $"{nameof(MerchandiseReturn)}:label:{merchandiseReturnId}";
    }
}
