namespace Grand.Module.Api.DTOs.Inventory;

/// <summary>
///     The outcome of one line of a bulk stock write
/// </summary>
public class StockApplyResultDto
{
    public string Key { get; set; }

    /// <summary>
    ///     "Applied", "AlreadyApplied" or "Rejected"
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    ///     Why the line was rejected; empty otherwise
    /// </summary>
    public string Error { get; set; }

    /// <summary>
    ///     The bucket's stock after the line; empty when rejected
    /// </summary>
    public int? Stock { get; set; }
}
