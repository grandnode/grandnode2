namespace Grand.Domain.Orders;

/// <summary>
///     Represents the state of the return label of a merchandise return
/// </summary>
public enum ReturnLabelStatus
{
    /// <summary>
    ///     No label was requested
    /// </summary>
    None = 0,

    /// <summary>
    ///     The label is being requested from the provider
    /// </summary>
    Pending = 10,

    /// <summary>
    ///     The label was created
    /// </summary>
    Created = 20,

    /// <summary>
    ///     The provider failed to create the label
    /// </summary>
    Failed = 30
}
