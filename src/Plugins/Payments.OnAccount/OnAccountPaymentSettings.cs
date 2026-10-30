using Grand.Domain.Configuration;
using Grand.Domain.Orders;

namespace Payments.OnAccount;

public class OnAccountPaymentSettings : ISettings
{
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether a company with an overdue payment can no longer buy on credit
    /// </summary>
    public bool BlockWhenOverdue { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether the checkout asks for the buyer's purchase order number with this method
    /// </summary>
    public PurchaseOrderNumberMode PurchaseOrderNumberMode { get; set; }
}
