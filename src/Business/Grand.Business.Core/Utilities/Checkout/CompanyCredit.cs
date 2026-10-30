namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Trade credit of a company: its limit and payment term, and what it still owes on orders bought on credit.
///     Amounts are in the primary store currency.
/// </summary>
/// <param name="CompanyOwnerId">Identifier of the company owner the credit belongs to</param>
/// <param name="CreditLimit">Credit limit; null means no trade credit</param>
/// <param name="PaymentTermDays">Payment term in days; null means no trade credit</param>
/// <param name="OutstandingAmount">Unpaid amount of the company's orders bought on credit</param>
/// <param name="HasOverdue">Whether any of those orders is past its due date</param>
public sealed record CompanyCredit(
    string CompanyOwnerId,
    double? CreditLimit,
    int? PaymentTermDays,
    double OutstandingAmount,
    bool HasOverdue)
{
    /// <summary>
    ///     Gets a value indicating whether the company may buy on credit: both the limit and the term are set
    /// </summary>
    public bool Enabled => CreditLimit.HasValue && PaymentTermDays.HasValue;

    /// <summary>
    ///     Gets the credit still available
    /// </summary>
    public double Available => Enabled ? Math.Max(0, CreditLimit!.Value - OutstandingAmount) : 0;

    /// <summary>
    ///     Gets a value indicating whether an order of the given amount may be bought on credit
    /// </summary>
    /// <param name="amountInPrimaryCurrency">Order amount in the primary store currency</param>
    /// <param name="blockWhenOverdue">Whether an overdue payment blocks further buying on credit</param>
    public bool Covers(double amountInPrimaryCurrency, bool blockWhenOverdue)
    {
        return Enabled && !(blockWhenOverdue && HasOverdue) && amountInPrimaryCurrency <= Available;
    }

    /// <summary>
    ///     Gets the credit of a company that cannot buy on credit
    /// </summary>
    /// <param name="companyOwnerId">Identifier of the company owner</param>
    public static CompanyCredit None(string companyOwnerId)
    {
        return new CompanyCredit(companyOwnerId, null, null, 0, false);
    }
}
