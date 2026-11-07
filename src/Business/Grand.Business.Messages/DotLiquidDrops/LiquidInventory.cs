using DotLiquid;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using System.Globalization;

namespace Grand.Business.Messages.DotLiquidDrops;

/// <summary>
///     Tokens of the batches a store holds that are expiring or expired: {% for batch in Inventory.Batches %}
/// </summary>
public class LiquidInventory : Drop
{
    public LiquidInventory(IEnumerable<ExpiringBatch> batches)
    {
        ArgumentNullException.ThrowIfNull(batches);

        Batches = batches.Select(x => new LiquidBatch(x)).ToList();
    }

    public IList<LiquidBatch> Batches { get; }
}

/// <summary>
///     One batch: its product, warehouse, number, expiry date, quantity and expiry status
/// </summary>
public class LiquidBatch : Drop
{
    private readonly ExpiringBatch _batch;

    public LiquidBatch(ExpiringBatch batch)
    {
        _batch = batch;
    }

    public string ProductName => _batch.ProductName;
    public string Sku => _batch.Sku;
    public string Warehouse => _batch.WarehouseName;
    public string Number => _batch.Batch.Number;

    public string ExpiryDate =>
        _batch.Batch.ExpiryDateUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    public int Quantity => _batch.Batch.Quantity;

    /// <summary>
    ///     Expiring or Expired
    /// </summary>
    public string Status => _batch.Status.ToString();
}
