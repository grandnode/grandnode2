namespace Grand.Module.Api.DTOs.Inventory;

/// <summary>
///     One page of the stock feed
/// </summary>
public class StockJournalResultDto
{
    /// <summary>
    ///     The cursor to ask with next time: the id of the last entry returned, or the cursor asked with when the page is
    ///     empty. Keep it even when HasMore is false, to poll for new entries later
    /// </summary>
    public string? Next { get; set; }

    /// <summary>
    ///     Whether more entries follow this page now
    /// </summary>
    public bool HasMore { get; set; }

    public IList<StockJournalEntryDto> Entries { get; set; } = new List<StockJournalEntryDto>();
}
