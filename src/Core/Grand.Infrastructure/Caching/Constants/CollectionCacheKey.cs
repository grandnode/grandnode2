namespace Grand.Infrastructure.Caching.Constants
{
    public static partial class CacheKey
    {

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string COLLECTIONS_PATTERN_KEY => "Grand.collection.";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : collection ID
        /// </remarks>
        public static string COLLECTIONS_BY_ID_KEY => "Grand.collection.id-{0}";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : show hidden records?
        /// {1} : collection ID
        /// {2} : page index
        /// {3} : page size
        /// {4} : current customer ID
        /// {5} : store ID
        /// </remarks>
        public static string PRODUCTCOLLECTIONS_ALLBYCOLLECTIONID_KEY => "Grand.productcollection.allbycollectionid-{0}-{1}-{2}-{3}-{4}-{5}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string PRODUCTCOLLECTIONS_PATTERN_KEY => "Grand.productcollection.";

    }
}
