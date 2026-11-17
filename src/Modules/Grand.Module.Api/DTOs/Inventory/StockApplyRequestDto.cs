namespace Grand.Module.Api.DTOs.Inventory;

/// <summary>
///     A bulk stock write: up to 1000 lines, applied in order, each with its own result
/// </summary>
public class StockApplyRequestDto
{
    public IList<StockApplyLineDto> Lines { get; set; } = new List<StockApplyLineDto>();
}
