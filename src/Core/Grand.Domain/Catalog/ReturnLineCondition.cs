namespace Grand.Domain.Catalog;

/// <summary>
///     What happens to a returned item: back on the shelf, or written off without touching stock
/// </summary>
public enum ReturnLineCondition
{
    Restock = 0,
    Scrap = 10
}
