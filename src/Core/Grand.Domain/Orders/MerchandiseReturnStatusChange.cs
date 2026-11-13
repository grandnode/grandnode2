namespace Grand.Domain.Orders;

/// <summary>
///     A status a merchandise return reached, and when
/// </summary>
public class MerchandiseReturnStatusChange
{
    /// <summary>
    ///     Gets or sets the status identifier
    /// </summary>
    public int StatusId { get; set; }

    /// <summary>
    ///     Gets or sets the status
    /// </summary>
    public MerchandiseReturnStatus Status {
        get => (MerchandiseReturnStatus)StatusId;
        set => StatusId = (int)value;
    }

    /// <summary>
    ///     Gets or sets when the return reached the status
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }
}
