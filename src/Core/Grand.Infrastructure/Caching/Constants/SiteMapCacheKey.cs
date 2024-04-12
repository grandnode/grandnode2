namespace Grand.Infrastructure.Caching.Constants;

public static partial class CacheKey
{
    /// <summary>
    ///     Key for caching
    /// </summary>
    public static string ADMIN_SITEMAP_KEY => "Grand.admin-sitemap.all";

    /// <summary>
    ///     Key pattern to clear cache
    /// </summary>
    public static string ADMIN_SITEMAP_PATTERN_KEY => "Grand.admin-sitemap.";
}