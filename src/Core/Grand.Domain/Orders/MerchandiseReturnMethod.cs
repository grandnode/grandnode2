namespace Grand.Domain.Orders;

/// <summary>
///     Represents how the customer sends a merchandise return back, as the store declares it
/// </summary>
public enum MerchandiseReturnMethod
{
    /// <summary>
    ///     Not declared - nothing is published about the return method
    /// </summary>
    NotSpecified = 0,

    /// <summary>
    ///     Returned by mail or courier
    /// </summary>
    ByMail = 10,

    /// <summary>
    ///     Returned in a physical store
    /// </summary>
    InStore = 20
}
