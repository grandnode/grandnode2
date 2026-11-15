using Grand.Module.Api.DTOs.Inventory;
using Grand.Mediator;

namespace Grand.Module.Api.Queries.Models.Inventory;

/// <summary>
///     One page of the stock feed: journal entries older than the settle window, ordered by id, from the cursor
/// </summary>
public class GetStockJournalQuery : IRequest<StockJournalResultDto>
{
    /// <summary>
    ///     The cursor: the id of the last entry of the previous page; empty for the beginning
    /// </summary>
    public string After { get; set; }

    public int Take { get; set; } = 100;

    public string WarehouseId { get; set; }

    public string WarehouseCode { get; set; }

    /// <summary>
    ///     Comma-separated inventory movement types to include; empty for all
    /// </summary>
    public string MovementTypes { get; set; }

    /// <summary>
    ///     Skip the entries the calling API customer wrote, so one connector does not hide another's writes
    /// </summary>
    public bool ExcludeOwn { get; set; }

    public string CustomerId { get; set; }
}
