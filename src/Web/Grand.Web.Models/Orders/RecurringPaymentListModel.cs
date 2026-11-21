using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Orders;

public class RecurringPaymentListModel : BaseModel
{
    public OrderPagingModel PagingContext { get; set; } = new();

    public IList<RecurringPaymentRowModel> RecurringPayments { get; set; } = new List<RecurringPaymentRowModel>();

    public class RecurringPaymentRowModel : BaseEntityModel
    {
        public DateTime CreatedOn { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the customer may cancel it: Pending, Active or PastDue
        /// </summary>
        public bool CanCancel { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the last cycle failed; the provider's reason is not shown
        /// </summary>
        public bool IsPastDue { get; set; }

        /// <summary>
        ///     Gets or sets the cycle, e.g. "Every month"
        /// </summary>
        public string Cycle { get; set; }

        /// <summary>
        ///     Gets or sets the paid cycles, e.g. "3 of 12"
        /// </summary>
        public string CyclesPaid { get; set; }

        /// <summary>
        ///     Gets or sets the next payment date reported by the provider, while it still charges
        /// </summary>
        public DateTime? NextPaymentDate { get; set; }

        public string InitialOrderId { get; set; }
        public int InitialOrderNumber { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the initial order exists and can be opened
        /// </summary>
        public bool CanViewInitialOrder { get; set; }

        public IList<string> Products { get; set; } = new List<string>();
    }
}
