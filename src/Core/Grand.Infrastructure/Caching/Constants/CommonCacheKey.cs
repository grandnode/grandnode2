﻿namespace Grand.Infrastructure.Caching.Constants
{
    public static partial class CacheKey
    {
        #region Settings

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : key - settings name
        /// {1} : store ident
        /// </remarks>
        public static string SETTINGS_BY_KEY => "Grand.setting.{0}.{1}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string SETTINGS_PATTERN_KEY => "Grand.setting.";

        #endregion

        #region Discounts

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : discont ID
        /// </remarks>
        public static string DISCOUNTS_BY_ID_KEY => "Grand.discount.id-{0}";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : show hidden records?
        /// {1} : store ident
        /// {2} : currency code
        /// {3} : coupon code
        /// {4} : discount name
        /// </remarks>
        public static string DISCOUNTS_ALL_KEY => "Grand.discount.all-{0}-{1}-{2}-{3}-{4}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string DISCOUNTS_PATTERN_KEY => "Grand.discount.";

        #endregion

        #region Languages & localization

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// </remarks>
        public static string LANGUAGES_BY_ID_KEY => "Grand.language.id-{0}";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : language code
        /// </remarks>
        public static string LANGUAGES_BY_CODE => "Grand.language.code-{0}";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : show hidden records?
        /// </remarks>
        public static string LANGUAGES_ALL_KEY => "Grand.language.all-{0}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string LANGUAGES_PATTERN_KEY => "Grand.language.";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// </remarks>
        public static string TRANSLATERESOURCES_ALL_KEY => "Grand.translate.all-{0}";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// {1} : resource key
        /// </remarks>
        public static string TRANSLATERESOURCES_BY_RESOURCENAME_KEY => "Grand.translate.{0}-{1}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string TRANSLATERESOURCES_PATTERN_KEY => "Grand.translate.";

        #endregion

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : picture ID
        /// </remarks>
        public static string PICTURE_BY_ID => "Grand.picture-{0}";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : picture ID
        /// {1} : store ID
        /// {2} : target size
        /// {3} : showDefaultPicture
        /// {4} : storeLocation
        /// </remarks>
        public static string PICTURE_BY_KEY => "Grand.picture-{0}-{1}-{2}-{3}-{4}";

        #region Seo

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : entity ID
        /// {1} : entity name
        /// {2} : language ID
        /// </remarks>
        public static string URLEntity_ACTIVE_BY_ID_NAME_LANGUAGE_KEY => "Grand.urlEntity.active.id-name-language-{0}-{1}-{2}";

        /// <summary>
        /// Key for caching
        /// </summary>
        public static string URLEntity_ALL_KEY => "Grand.urlEntity.all";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : slug
        /// </remarks>
        public static string URLEntity_BY_SLUG_KEY => "Grand.urlEntity.active.slug-{0}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string URLEntity_PATTERN_KEY = "Grand.urlEntity.";

        #endregion

        #region Stores

        /// <summary>
        /// Key for caching
        /// </summary>
        public static string STORES_ALL_KEY => "Grand.stores.all";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : store ID
        /// </remarks>
        public static string STORES_BY_ID_KEY => "Grand.stores.id-{0}";

        #endregion

        #region Tax

        /// <summary>
        /// Key for caching
        /// </summary>
        public static string TAXCATEGORIES_ALL_KEY => "Grand.taxcategory.all";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : tax category ID
        /// </remarks>
        public static string TAXCATEGORIES_BY_ID_KEY => "Grand.taxcategory.id-{0}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string TAXCATEGORIES_PATTERN_KEY => "Grand.taxcategory.";

        #endregion

        #region Pages

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : store ID
        /// {1} : ignore ACL?
        /// </remarks>
        public static string PAGES_ALL_KEY => "Grand.pages.all-{0}-{1}";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : page ID
        /// </remarks>
        public static string PAGES_BY_ID_KEY => "Grand.pages.id-{0}";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : page systemname
        /// {1} : store id
        /// </remarks>
        public static string PAGES_BY_SYSTEMNAME => "Grand.pages.systemname-{0}-{1}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        public static string PAGES_PATTERN_KEY => "Grand.pages.";

        #endregion
    }
}
