namespace Grand.Module.Api.DTOs.Inventory;

/// <summary>
///     One page of the stock feed; Next is the cursor of the page after this one, empty when there are no more entries
/// </summary>
public class StockJournalResultDto
{
    public string Next { get; set; }
    public IList<StockJournalEntryDto> Entries { get; set; } = new List<StockJournalEntryDto>();
}
