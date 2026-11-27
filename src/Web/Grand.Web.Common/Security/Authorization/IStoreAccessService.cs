namespace Grand.Web.Common.Security.Authorization;

/// <summary>
///     Store access of the customer of the current request; the guest account screens pick their layout from it
/// </summary>
public interface IStoreAccessService
{
    /// <summary>
    ///     Closed when the store is closed to the customer, RegisteredOnly when the customer may not browse it, else Open
    /// </summary>
    Task<StoreAccess> GetStoreAccess();
}
