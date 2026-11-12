using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     The label of a merchandise return and the rest of its return shipping. Each method holds the lock of the
///     return's label, never the lock of its order, so a provider is called while the order's returns can still be
///     changed, and it writes only the return shipping of the return, so it overwrites none of those changes
/// </summary>
public interface IMerchandiseReturnLabelService
{
    /// <summary>
    ///     Creates the label, or only the instructions when the method creates no label; called right after the return
    ///     is saved and again to retry a failed label. A label the provider failed to create is saved as failed, with a
    ///     note for the staff, and is not an error of the call
    /// </summary>
    /// <returns>null when done or there is nothing to do, else a key of MerchandiseReturnErrors</returns>
    Task<string> CreateLabel(string merchandiseReturnId);

    /// <summary>
    ///     Cancels a created label once the return is rejected or cancelled; best effort: a label the provider did
    ///     not cancel stays created, with a note for the staff
    /// </summary>
    Task CancelLabel(string merchandiseReturnId);

    /// <summary>
    ///     Saves the changes apply makes to the return shipping: the staff's edits, the tracking number the customer
    ///     types in, a carrier's updates. The status of the return is never changed here
    /// </summary>
    /// <param name="merchandiseReturnId">The return</param>
    /// <param name="apply">The changes</param>
    /// <param name="allowed">
    ///     Checked on the return as stored, under the locks: when it says no, nothing is changed, e.g. a tracking
    ///     number typed in while the staff marked the return received
    /// </param>
    /// <returns>null when saved or not allowed, else a key of MerchandiseReturnErrors</returns>
    Task<string> UpdateShipping(string merchandiseReturnId, Action<MerchandiseReturnShipping> apply,
        Func<MerchandiseReturn, bool> allowed = null);
}
