namespace Grand.Web.Models.Orders;

/// <summary>
///     The refund a return form comes to before it is submitted, worked out as the refund itself is; the amounts are
///     formatted in the order's currency, and null when the form cannot be submitted as it is
/// </summary>
public class MerchandiseReturnEstimateModel
{
    public string ItemsAmount { get; set; }
    public string ReturnShippingCost { get; set; }
    public string Amount { get; set; }

    /// <summary>
    ///     Why the form would be refused, e.g. a quantity another return took in the meantime
    /// </summary>
    public IList<string> Errors { get; set; } = new List<string>();
}
