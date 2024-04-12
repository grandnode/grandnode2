using Grand.Domain.History;

namespace Grand.Domain.Messages;

/// <summary>
///     Represents NewsLetterSubscription entity
/// </summary>
public class NewsLetterSubscription : BaseEntity, IHistory
{
    private ICollection<string> _categories;

    /// <summary>
    ///     Gets or sets the newsletter subscription GUID
    /// </summary>
    public Guid NewsLetterSubscriptionGuid { get; set; }

    /// <summary>
    ///     Gets or sets the Customer identifier
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the subcriber email
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether subscription is active
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    ///     Gets or sets the store identifier in which a customer has subscribed to newsletter
    /// </summary>
    public string StoreId { get; set; }

    /// <summary>
    ///     Gets or sets the categories
    /// </summary>
    public virtual ICollection<string> Categories {
        get => _categories ??= new List<string>();
        protected set => _categories = value;
    }
}