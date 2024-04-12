using Grand.Domain.Payments;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Process payment result
/// </summary>
public class ProcessPaymentResult
{
    /// <summary>
    ///     Ctor
    /// </summary>
    public ProcessPaymentResult()
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
    ///     Gets or sets a payment transaction status after processing
    /// </summary>
    public TransactionStatus NewPaymentTransactionStatus { get; set; } = TransactionStatus.Pending;

    /// <summary>
    ///     Gets or sets value paid amount
    /// </summary>
    public double PaidAmount { get; set; }

    /// <summary>
    ///     Add error
    /// </summary>
    /// <param name="error">Error</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }
}