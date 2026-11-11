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
    ///     Changes the status along the allowed flow and sends the customer the status changed message
    /// </summary>
    /// <param name="merchandiseReturnId">Merchandise return identifier</param>
    /// <param name="status">The new status</param>
    /// <param name="customerNote">A note shown to the customer with the change, e.g. why the return was rejected</param>
    Task<string> ChangeStatus(string merchandiseReturnId, MerchandiseReturnStatus status, string customerNote = null);

    /// <summary>
    ///     Saves the fields set by apply; the status stays as stored, it changes only through ChangeStatus
    /// </summary>
    Task<string> Update(string merchandiseReturnId, Action<MerchandiseReturn> apply);

    /// <summary>
    ///     Deletes the merchandise return
    /// </summary>
    Task<string> Delete(string merchandiseReturnId);
}
