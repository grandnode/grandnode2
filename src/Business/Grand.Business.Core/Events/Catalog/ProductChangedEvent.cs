using Grand.Mediator;

namespace Grand.Business.Core.Events.Catalog;

/// <summary>
///     A stored product changed: one of its own fields, or a record embedded in it (tier price, picture, category
///     mapping, attribute, ...) whose own event does not carry the product id
/// </summary>
public class ProductChangedEvent : INotification
{
    public ProductChangedEvent(string productId, bool listsChanged = false)
    {
        ProductId = productId;
        ListsChanged = listsChanged;
    }

    public string ProductId { get; }

    /// <summary>
    ///     Gets a value indicating whether lists of products may show the change too: listings, the home page and
    ///     the customer group, customer tag and personal product lists
    /// </summary>
    public bool ListsChanged { get; }
}
