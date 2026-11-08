namespace Grand.Domain.Shipping;

/// <summary>
///     The quantity a shipment item takes from one batch
/// </summary>
public class ShipmentItemBatch
{
    public string Number { get; set; }

    public int Quantity { get; set; }
}
