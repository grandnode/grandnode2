namespace Grand.Infrastructure.Caching.Constants
{
    public static partial class CacheKey
    {

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string BRANDS_PATTERN_KEY => "Grand.brand.";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : brand ID
        /// </remarks>
        public static string BRANDS_BY_ID_KEY => "Grand.brand.id-{0}";

    }
}
