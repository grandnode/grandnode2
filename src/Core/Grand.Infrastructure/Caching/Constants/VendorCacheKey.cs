namespace Grand.Infrastructure.Caching.Constants;

public static partial class CacheKey
{
    /// <summary>
    ///     Key for caching
    /// </summary>
    /// <remarks>
    ///     {0} : vendor ID
    /// </remarks>
    public static string VENDOR_BY_ID_KEY => "Grand.vendor.id-{0}";
}