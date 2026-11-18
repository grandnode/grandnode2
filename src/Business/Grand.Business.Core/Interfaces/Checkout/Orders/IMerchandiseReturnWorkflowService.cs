using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Every change of a merchandise return: each one holds the lock of the return's order, so it is checked against
///     the quantity left to return and the return as stored, not as a page loaded earlier saw it. Each method returns
///     the reason it changed nothing, as a key of MerchandiseReturnErrors, or null
/// </summary>
public interface IMerchandiseReturnWorkflowService
{
    /// <summary>
    ///     Creates what the customer asked to return, one merchandise return for each vendor of the items, or none
    ///     when more than is left to return was asked for
    /// </summary>
    Task<MerchandiseReturnSubmitResult> Submit(MerchandiseReturnSubmitRequest request);

    /// <summary>
    ///     The returns Submit would save for the request, one for each vendor with its return shipping, or the reason
    ///     it would save none; nothing is saved, no lock is held and no label is created. The quantity left is read as
    ///     it is now: Submit checks it again under the lock
    /// </summary>
    Task<MerchandiseReturnSubmitResult> Preview(MerchandiseReturnSubmitRequest request);

    /// <summary>
    ///     Changes the status along the allowed flow, notes for the staff who changed it, and sends the customer the
    ///     status changed message. Received only where CanMarkReceived says yes to the return as stored
    /// </summary>
    /// <param name="merchandiseReturnId">Merchandise return identifier</param>
    /// <param name="status">The new status</param>
    /// <param name="customerNote">A note shown to the customer with the change, e.g. why the return was rejected</param>
    /// <param name="changedBy">Who changed it, as the staff note names them (e.g. the e-mail); null for the system</param>
    Task<string> ChangeStatus(string merchandiseReturnId, MerchandiseReturnStatus status, string customerNote = null,
        string changedBy = null);

    /// <summary>
    ///     Changes the status as the other overload does, when allowed says yes to the return as stored
    /// </summary>
    /// <param name="merchandiseReturnId">Merchandise return identifier</param>
    /// <param name="status">The new status</param>
    /// <param name="customerNote">A note shown to the customer with the change</param>
    /// <param name="changedBy">Who changed it, as the staff note names them; null for the system</param>
    /// <param name="allowed">
    ///     Checked on the return as stored, under the lock: when it says no, nothing is saved, e.g. a customer's cancel
    ///     of a return the staff accepted since the page was loaded (StatusChangeNotAllowed)
    /// </param>
    Task<string> ChangeStatus(string merchandiseReturnId, MerchandiseReturnStatus status, string customerNote,
        string changedBy, Func<MerchandiseReturn, bool> allowed);

    /// <summary>
    ///     Changes the status as the overload with allowed does
    /// </summary>
    /// <param name="merchandiseReturnId">Merchandise return identifier</param>
    /// <param name="status">The new status</param>
    /// <param name="customerNote">A note shown to the customer with the change</param>
    /// <param name="changedBy">Who changed it, as the staff note names them; null for the system</param>
    /// <param name="allowed">Checked on the return as stored, under the lock; null allows any return</param>
    /// <param name="withoutReturnReceipts">
    ///     The caller can post no return receipt (the vendor panel has no inventory documents): Received is allowed
    ///     where a receipt would take the goods in
    /// </param>
    Task<string> ChangeStatus(string merchandiseReturnId, MerchandiseReturnStatus status, string customerNote,
        string changedBy, Func<MerchandiseReturn, bool> allowed, bool withoutReturnReceipts);

    /// <summary>
    ///     Saves the fields set by apply; the status stays as stored, it changes only through ChangeStatus
    /// </summary>
    Task<string> Update(string merchandiseReturnId, Action<MerchandiseReturn> apply);

    /// <summary>
    ///     Deletes the merchandise return
    /// </summary>
    Task<string> Delete(string merchandiseReturnId);

    /// <summary>
    ///     A return receipt of the return was posted: a pending or accepted return becomes received, any other one
    ///     stays as it is
    /// </summary>
    /// <param name="merchandiseReturnId">Merchandise return identifier</param>
    /// <param name="changedBy">Who posted the receipt, as the staff note names them; null for the system</param>
    Task MarkReceived(string merchandiseReturnId, string changedBy = null);

    /// <summary>
    ///     Whether staff may mark the return received by hand: a posted return receipt took the goods in already (it
    ///     marks the return received itself unless that failed), or no receipt can take them in, as a product that does
    ///     not track stock, nor a bundle none of whose products does, is not booked. Otherwise only a return receipt makes
    ///     it received, so the goods reach the stock and their inspection. ChangeStatus refuses Received otherwise, on
    ///     the return as stored; a staff surface asks it before offering Received
    /// </summary>
    /// <param name="merchandiseReturn">The merchandise return</param>
    Task<bool> CanMarkReceived(MerchandiseReturn merchandiseReturn);

    /// <summary>
    ///     As the other overload, with the return's order the caller has loaded already
    /// </summary>
    /// <param name="merchandiseReturn">The merchandise return</param>
    /// <param name="order">The return's order; null when it was not found</param>
    Task<bool> CanMarkReceived(MerchandiseReturn merchandiseReturn, Order order);
}
