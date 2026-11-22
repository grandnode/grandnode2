namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     What the recurring payment service did with a gateway report
/// </summary>
public enum RecurringReportOutcome
{
    /// <summary>
    ///     The report was recorded on the recurring payment
    /// </summary>
    Recorded = 0,

    /// <summary>
    ///     Nothing was recorded: the recurring payment is unknown
    /// </summary>
    NotFound = 10,

    /// <summary>
    ///     Nothing was recorded: the same report was recorded already, typically a resend
    /// </summary>
    Duplicate = 20,

    /// <summary>
    ///     Nothing was recorded: the report names another reference than the one the recurring payment runs on
    /// </summary>
    OtherReference = 30,

    /// <summary>
    ///     Nothing was recorded: the recurring payment is cancelled or completed
    /// </summary>
    Ended = 40
}
