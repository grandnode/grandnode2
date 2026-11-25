using Grand.Domain;
using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class GetPaymentTransactionQuery : IRequest<IPagedList<PaymentTransaction>>
{
    public string StoreId { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public Guid? OrderGuid { get; set; }
    public TransactionStatus? Ts { get; set; } = null;
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = int.MaxValue;
    public DateTime? CreatedFromUtc { get; set; } = null;
    public DateTime? CreatedToUtc { get; set; } = null;

    /// <summary>False leaves out transactions of a checkout that never got a payment through: still in progress,
    /// failed or abandoned.</summary>
    public bool IncludeTemp { get; set; } = true;

    public string? PaymentMethodSystemName { get; set; }

    /// <summary>Only the payments of orders carrying this purchase order number, matched exactly.</summary>
    public string? PurchaseOrderNumber { get; set; }

    /// <summary>Only the payments of orders bought on credit and past their due date.</summary>
    public bool OverdueOnly { get; set; }

    /// <summary>Only the payments of this sales employee's orders.</summary>
    public string? SalesEmployeeId { get; set; }
}