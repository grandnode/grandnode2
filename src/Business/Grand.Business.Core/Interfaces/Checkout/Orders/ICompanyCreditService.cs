using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Trade credit of company accounts: the limit and term set on the company owner, and what the company owes
///     on orders bought on credit
/// </summary>
public interface ICompanyCreditService
{
    /// <summary>
    ///     Gets the trade credit the customer buys with: the customer's own for a company owner, the owner's for
    ///     a sub-account. The credit is disabled when the owner is missing, deleted or inactive.
    /// </summary>
    /// <param name="customer">Customer</param>
    Task<CompanyCredit> GetCompanyCredit(Customer customer);

    /// <summary>
    ///     Gets the unpaid amount of the company's orders bought on credit, in the primary store currency
    /// </summary>
    /// <param name="companyOwnerId">Identifier of the company owner</param>
    Task<double> GetOutstandingAmount(string companyOwnerId);

    /// <summary>
    ///     Gets a value indicating whether any of the company's orders bought on credit is past its due date
    ///     (the due date is a calendar day: an order due today is not overdue yet)
    /// </summary>
    /// <param name="companyOwnerId">Identifier of the company owner</param>
    /// <param name="utcNow">Current date and time in UTC</param>
    Task<bool> HasOverdue(string companyOwnerId, DateTime utcNow);
}
