using Grand.Domain.Configuration;

namespace Grand.Domain.Common;

public class CommonSettings : ISettings
{
    public bool StoreInDatabaseContactUsForm { get; set; }
    public bool SubjectFieldOnContactUsForm { get; set; }
    public bool UseSystemEmailForContactUsForm { get; set; }

    public bool SitemapEnabled { get; set; }
    public bool SitemapIncludeCategories { get; set; }
    public bool SitemapIncludeBrands { get; set; }
    public bool SitemapIncludeProducts { get; set; }
    public bool SitemapIncludeImage { get; set; }

    /// <summary>
    ///     A list of custom URLs to be added to sitemap.xml (include page names only)
    /// </summary>
    public List<string> SitemapCustomUrls { get; set; } = new();

    /// <summary>
    ///     Gets or sets a ignore words (phrases) to be ignored when logging errors/messages
    /// </summary>
    public List<string> IgnoreLogWordlist { get; set; } = new();

    /// <summary>
    ///     Gets or sets a value indicating whether "accept terms of service" links should be open in popup window. If
    ///     disabled, then they'll be open on a new page.
    /// </summary>
    public bool PopupForTermsOfServiceLinks { get; set; }

    /// <summary>
    ///     Gets or sets to allow user to select store
    /// </summary>
    public bool AllowToSelectStore { get; set; }

    /// <summary>
    ///     Gets or sets to edit product where auction ended
    /// </summary>
    public bool AllowEditProductEndedAuction { get; set; }

    /// <summary>
    ///     Gets or sets - allow user to read "let's encrypted file"
    /// </summary>
    public bool AllowToReadLetsEncryptFile { get; set; }
}