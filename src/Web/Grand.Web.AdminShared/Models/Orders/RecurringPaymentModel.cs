using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Orders;

/// <summary>
///     A recurring payment: a row of the list, and with its cycles the details screen
/// </summary>
public class RecurringPaymentModel : BaseEntityModel
{
    public string CustomerId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.Customer")]
    public string CustomerEmail { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.Store")]
    public string StoreName { get; set; }

    public int StatusId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.Status")]
    public string Status { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.Cycle")]
    public string Cycle { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.CyclesPaid")]
    public string CyclesPaid { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.NextPaymentDate")]
    public DateTime? NextPaymentDate { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.PaymentMethod")]
    public string PaymentMethodName { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.ProviderReference")]
    public string ProviderReference { get; set; }

    public string InitialOrderId { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.InitialOrder")]
    public int InitialOrderNumber { get; set; }

    /// <summary>
    ///     False for a deleted initial order, which has no screen to open
    /// </summary>
    public bool CanViewInitialOrder { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.CreatedOn")]
    public DateTime CreatedOn { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.LastError")]
    public string LastError { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.LastErrorDate")]
    public DateTime? LastErrorDate { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.CancelledOn")]
    public DateTime? CancelledOn { get; set; }

    [GrandResourceDisplayName("Admin.Orders.RecurringPayments.Fields.CancelledBy")]
    public string CancelledBy { get; set; }

    public bool CanCancel { get; set; }

    public IList<CycleModel> Cycles { get; set; } = new List<CycleModel>();

    public class CycleModel
    {
        public int CycleNumber { get; set; }
        public string OrderId { get; set; }
        public int OrderNumber { get; set; }

        /// <summary>
        ///     False when the cycle's order was deleted, or was never created
        /// </summary>
        public bool CanViewOrder { get; set; }

        public string Amount { get; set; }
        public DateTime PaidOn { get; set; }
        public string ProviderReference { get; set; }
    }
}
