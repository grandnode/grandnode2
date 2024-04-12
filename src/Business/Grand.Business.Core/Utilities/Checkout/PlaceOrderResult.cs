using Grand.Domain.Orders;
using Grand.Domain.Payments;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Place order result
/// </summary>
public class PlaceOrderResult
{
    /// <summary>
    ///     Ctor
    /// </summary>
    public PlaceOrderResult()
    {
        Errors = new List<string>();
    }

    /// <summary>
    ///     Gets a value indicating whether request has been completed successfully
    /// </summary>
    public bool Success => Errors.Count == 0;

    /// <summary>
    ///     Errors
    /// </summary>
    public IList<string> Errors { get; set; }

    /// <summary>
    ///     Gets or sets the placed order
    /// </summary>
    public Order PlacedOrder { get; set; }

    /// <summary>
    ///     Gets or sets the payment transaction
    /// </summary>
    public PaymentTransaction PaymentTransaction { get; set; }

    /// <summary>
    ///     Add error
    /// </summary>
    /// <param name="error">Error</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }
}