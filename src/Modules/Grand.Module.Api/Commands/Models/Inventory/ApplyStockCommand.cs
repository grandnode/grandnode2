using Grand.Module.Api.DTOs.Inventory;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Inventory;

/// <summary>
///     A bulk stock write: each line applied on its own, in order, with its own result
/// </summary>
public class ApplyStockCommand : IRequest<IList<StockApplyResultDto>>
{
    public IList<StockApplyLineDto> Lines { get; set; } = new List<StockApplyLineDto>();

    /// <summary>
    ///     The API customer the entries are booked for
    /// </summary>
    public required string CustomerId { get; set; }
}
