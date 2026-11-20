namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Represents a CancelRecurringPaymentResult
/// </summary>
public class CancelRecurringPaymentResult
{
    /// <summary>
    ///     Gets a value indicating whether request has been completed successfully
    /// </summary>
    public bool Success => Errors.Count == 0;

    /// <summary>
    ///     Errors
    /// </summary>
    public IList<string> Errors { get; set; } = new List<string>();

    /// <summary>
    ///     Add error
    /// </summary>
    /// <param name="error">Error</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }
}
