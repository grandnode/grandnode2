namespace Grand.Web.Models.Orders;

/// <summary>
///     A pickup point of the store the customer can return the items at
/// </summary>
public class MerchandiseReturnInStorePointModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
}
