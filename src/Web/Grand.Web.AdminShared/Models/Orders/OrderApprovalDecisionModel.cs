namespace Grand.Web.AdminShared.Models.Orders;

/// <summary>
///     The popup approving or rejecting an order
/// </summary>
public class OrderApprovalDecisionModel
{
    public string OrderId { get; set; }
    public int OrderNumber { get; set; }
    public IList<string> Reasons { get; set; } = new List<string>();
}
