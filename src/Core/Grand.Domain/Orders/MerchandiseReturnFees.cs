namespace Grand.Domain.Orders;

/// <summary>
///     Represents who pays for a merchandise return, as the store declares it
/// </summary>
public enum MerchandiseReturnFees
{
    /// <summary>
    ///     Not declared - nothing is published about return fees
    /// </summary>
    NotSpecified = 0,

    /// <summary>
    ///     Returns cost the customer nothing
    /// </summary>
    Free = 10,

    /// <summary>
    ///     The customer pays for the return
    /// </summary>
    CustomerPays = 20
}
