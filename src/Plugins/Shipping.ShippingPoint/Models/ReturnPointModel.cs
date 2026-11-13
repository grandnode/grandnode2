namespace Shipping.ShippingPoint.Models;

public class ReturnPointModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string OpeningHours { get; set; }
    /// <summary>
    ///     The fee formatted in the order's currency; empty when the return is free whatever point is chosen
    /// </summary>
    public string ReturnFee { get; set; }
}
