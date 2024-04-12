namespace Grand.Domain.Localization;

/// <summary>
///     Represents a locale string resource
/// </summary>
public class TranslationResource : BaseEntity
{
    /// <summary>
    ///     Gets or sets the language identifier
    /// </summary>
    public string LanguageId { get; set; }

    /// <summary>
    ///     Gets or sets the translation name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the translation value
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    ///     Gets or sets the translation area
    /// </summary>
    public TranslationResourceArea Area { get; set; }
}