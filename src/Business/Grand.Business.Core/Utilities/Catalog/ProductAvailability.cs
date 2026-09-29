namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     Whether a product can be bought right now. The member names are the schema.org
///     ItemAvailability values, so https://schema.org/{value} is the structured-data form.
/// </summary>
public enum ProductAvailability
{
    InStock,
    OutOfStock,
    BackOrder,
    PreOrder
}
