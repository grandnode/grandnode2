using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Orders;

public class OrderApprovalModel : BaseEntityModel
{
    public DateTime CreatedOn { get; set; }
    public int StatusId { get; set; }
    public string Status { get; set; }
    public string Buyer { get; set; }

    /// <summary>
    ///     The placed order awaiting the decision
    /// </summary>
    public string OrderId { get; set; }

    public int OrderNumber { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the viewer may open the order's details page
    /// </summary>
    public bool CanViewOrder { get; set; }

    public DateTime? DecidedOn { get; set; }
    public string DecidedBy { get; set; }
    public string DecisionComment { get; set; }

    /// <summary>
    ///     The viewer is an approver of the company and the request is still pending
    /// </summary>
    public bool CanDecide { get; set; }

    /// <summary>
    ///     The viewer is the buyer and the request is pending or approved but not yet used
    /// </summary>
    public bool CanWithdraw { get; set; }

    public IList<OrderApprovalItemModel> Items { get; set; } = new List<OrderApprovalItemModel>();
    public OrderApprovalSummaryModel Summary { get; set; } = new();

    public class OrderApprovalItemModel : BaseModel
    {
        public string ProductName { get; set; }
        public string Sku { get; set; }
        public int Quantity { get; set; }
        public string AttributeDescription { get; set; }
        public string UnitPrice { get; set; }
        public string LineTotal { get; set; }
    }

    public class OrderApprovalSummaryModel : BaseModel
    {
        public string SubTotal { get; set; }
        public string Discount { get; set; }
        public string ShippingMethod { get; set; }
        public string Shipping { get; set; }
        public string Tax { get; set; }
        public string Total { get; set; }
    }
}
