namespace Grand.Domain.Orders;

/// <summary>
///     Whether the checkout asks for the buyer's purchase order number
/// </summary>
public enum PurchaseOrderNumberMode
{
    /// <summary>
    ///     The checkout does not ask for it
    /// </summary>
    Disabled = 0,

    /// <summary>
    ///     The buyer may enter it
    /// </summary>
    Optional = 10,

    /// <summary>
    ///     The buyer has to enter it
    /// </summary>
    Required = 20
}
