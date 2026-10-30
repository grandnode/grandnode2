using Grand.Domain.Localization;
using Grand.Domain.Audit;

namespace Grand.Domain.Seo;

/// <summary>
///     Represents an URL
/// </summary>
public class EntityUrl : BaseEntity
{
    /// <summary>
    ///     Gets or sets the entity identifier
    /// </summary>
    public string EntityId { get; set; }

    /// <summary>
    ///     Gets or sets the entity name
    /// </summary>
    public string EntityName { get; set; }

    /// <summary>
    ///     Gets or sets the slug
    /// </summary>
    [AuditName]
    public string Slug { get; set; }

    /// <summary>
    ///     Gets or sets the value indicating whether the item is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    ///     Gets or sets the language identifier
    /// </summary>
    [AuditReference(typeof(Language))]
    public string LanguageId { get; set; }

    /// <summary>
    ///     Gets or sets the controller name
    /// </summary>
    public string Controller { get; set; }

    /// <summary>
    ///     Gets or sets the action name
    /// </summary>
    public string Action { get; set; }
}