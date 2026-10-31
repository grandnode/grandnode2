using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Company credit service
/// </summary>
public class CompanyCreditService : ICompanyCreditService
{
    #region Ctor

    public CompanyCreditService(IRepository<Order> orderRepository, ICustomerService customerService)
    {
        _orderRepository = orderRepository;
        _customerService = customerService;
    }

    #endregion

    #region Fields

    private readonly IRepository<Order> _orderRepository;
    private readonly ICustomerService _customerService;

    #endregion

    #region Utilities

    /// <summary>
    ///     Gets the company's orders bought on credit that are not paid yet
    /// </summary>
    private IQueryable<Order> UnpaidCreditOrders(string companyOwnerId)
    {
        return _orderRepository.Table.Where(o =>
            o.OwnerId == companyOwnerId &&
            o.PaymentDueDateUtc != null &&
            !o.Deleted &&
            o.OrderStatusId != (int)OrderStatusSystem.Cancelled &&
            (o.PaymentStatusId == PaymentStatus.Pending ||
             o.PaymentStatusId == PaymentStatus.Authorized ||
             o.PaymentStatusId == PaymentStatus.PartiallyPaid));
    }

    #endregion

    #region Methods

    public virtual async Task<CompanyCredit> GetCompanyCredit(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var companyOwnerId = OrderApprovalRules.CompanyId(customer);
        var owner = string.IsNullOrEmpty(customer.OwnerId)
            ? customer
            : await _customerService.GetCustomerById(customer.OwnerId);

        if (owner is not { Active: true, Deleted: false } ||
            !owner.CreditLimit.HasValue || !owner.PaymentTermDays.HasValue)
            return CompanyCredit.None(companyOwnerId);

        var outstanding = await GetOutstandingAmount(companyOwnerId);
        var hasOverdue = await HasOverdue(companyOwnerId, DateTime.UtcNow);

        return new CompanyCredit(companyOwnerId, owner.CreditLimit, owner.PaymentTermDays, outstanding,
            hasOverdue);
    }

    public virtual async Task<double> GetOutstandingAmount(string companyOwnerId)
    {
        if (string.IsNullOrEmpty(companyOwnerId))
            return 0;

        var query = UnpaidCreditOrders(companyOwnerId)
            .Select(o => new { o.OrderTotal, o.PaidAmount, o.CurrencyRate });
        var orders = await _orderRepository.ToListAsync(query);

        // the same conversion to the primary currency as the order reports use
        return orders.Sum(o => (o.OrderTotal - o.PaidAmount) / (o.CurrencyRate > 0 ? o.CurrencyRate : 1));
    }

    public virtual async Task<bool> HasOverdue(string companyOwnerId, DateTime utcNow)
    {
        if (string.IsNullOrEmpty(companyOwnerId))
            return false;

        //the due date is a calendar day: overdue once it has passed, as in PaymentExtensions.IsPaymentOverdue
        var today = utcNow.Date;
        return await _orderRepository.AnyAsync(
            UnpaidCreditOrders(companyOwnerId).Where(o => o.PaymentDueDateUtc < today));
    }

    #endregion
}
