namespace Grand.Domain.Catalog;

/// <summary>
///     A draft can be edited and deleted; a posted document has booked its stock and never changes
/// </summary>
public enum InventoryDocumentStatus
{
    Draft = 0,
    Posted = 10
}
