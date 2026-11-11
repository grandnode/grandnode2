namespace Grand.Web.Models.Common;

/// <summary>
///     Facts for the site-wide JSON-LD (Organization and WebSite). Urls that depend on the
///     request are built in the view; everything here is store data.
/// </summary>
public class SiteStructuredDataModel
{
    public string StoreName { get; set; }

    /// <summary>
    ///     Logo url as the picture service returns it (may be relative)
    /// </summary>
    public string LogoUrl { get; set; }

    public IList<string> SameAs { get; set; } = new List<string>();

    /// <summary>
    ///     Null when merchandise returns are off - then no return policy is declared at all
    /// </summary>
    public ReturnPolicyModel ReturnPolicy { get; set; }

    public class ReturnPolicyModel
    {
        /// <summary>
        ///     No day limit is configured.
        /// </summary>
        public bool UnlimitedWindow { get; set; }

        /// <summary>
        ///     merchantReturnDays, only when the store counts the days from delivery as schema.org does. Counted from
        ///     the order date, the number would promise a longer window than the store gives, so it is left out.
        /// </summary>
        public int? ReturnDays { get; set; }

        /// <summary>
        ///     schema.org ReturnFeesEnumeration member, or null when the store has not declared it
        /// </summary>
        public string ReturnFees { get; set; }

        /// <summary>
        ///     schema.org ReturnMethodEnumeration member, or null when the store has not declared it
        /// </summary>
        public string ReturnMethod { get; set; }

        /// <summary>
        ///     ISO 3166-1 alpha-2 codes of the countries the store ships to
        /// </summary>
        public IList<string> CountryCodes { get; set; } = new List<string>();
    }
}
