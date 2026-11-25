namespace Grand.Domain.Pages;

/// <summary>
///     Represents a page layout
/// </summary>
public class PageLayout : BaseEntity
{
    /// <summary>
    ///     Gets or sets the layout name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the view path
    /// </summary>
    public string ViewPath { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }
}