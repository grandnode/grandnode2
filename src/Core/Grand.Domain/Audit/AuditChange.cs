namespace Grand.Domain.Audit;

/// <summary>
///     One changed field of an audited entity
/// </summary>
public class AuditChange
{
    /// <summary>
    ///     Gets or sets the field path, e.g. Price or TierPrices[id=…].Quantity
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    ///     Gets or sets the value before the change (null when masked or when the field was added)
    /// </summary>
    public string OldValue { get; set; }

    /// <summary>
    ///     Gets or sets the value after the change (null when masked or when the field was removed)
    /// </summary>
    public string NewValue { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the values are hidden because the field is sensitive
    /// </summary>
    public bool Masked { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether a value was cut to the stored length
    /// </summary>
    public bool Truncated { get; set; }
}
