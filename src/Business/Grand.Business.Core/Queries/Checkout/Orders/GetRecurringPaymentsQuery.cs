using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

/// <summary>
///     Recurring payments for the panels, newest first
/// </summary>
public class GetRecurringPaymentsQuery : IRequest<IPagedList<RecurringPayment>>
{
    public string? StoreId { get; set; }

    /// <summary>Only the recurring payments of the customers with this e-mail, matched exactly.</summary>
    public string? CustomerEmail { get; set; }

    public RecurringPaymentStatus? Status { get; set; }

    /// <summary>Only the recurring payments whose initial order belongs to this sales employee.</summary>
    public string? SalesEmployeeId { get; set; }

    public int PageIndex { get; set; }
    public int PageSize { get; set; } = int.MaxValue;
}
