
namespace Grand.Web.Events.Cache
{
    public static class CacheKeyConst
    {
        /// <summary>
        /// Key for categories on the search page
        /// </summary>
        /// <remarks>
        /// {0} : language id
        /// {1} : groups of the current user
        /// {2} : current store ID
        /// </remarks>
        public const string SEARCH_CATEGORIES_MODEL_KEY = "Grand.category-{0}-{1}-{2}.pres.search";
        
        /// <summary>
        /// Key for List of BrandModel caching
        /// </summary>
        /// <remarks>
        /// {0} : language id
        /// {1} : comma separated list of customer groups
        /// {2} : current store ID
        /// </remarks>
        public const string BRAND_ALL_MODEL_KEY = "Grand.brand.navigation.all-{0}-{1}-{2}.pres";
        /// <summary>
        /// Key for caching of brand displayed on home page
        /// </summary>
        /// <remarks>
        /// {0} : current store ID
        /// {1} : language ID
        /// </remarks>
        public const string BRAND_HOMEPAGE_KEY = "Grand.brand.navigation.homepage-{0}-{1}.pres";
        /// <summary>
        /// Key for CollectionNavigationModel caching
        /// </summary>
        /// <remarks>
        /// {0} : language id
        /// {1} : current store ID
        /// </remarks>
        public const string BRAND_NAVIGATION_MENU = "Grand.brand.navigation.menu-{0}-{1}.pres";

        /// <summary>
        /// Key for CollectionNavigationModel caching
        /// </summary>
        /// <remarks>
        /// {0} : current collection id
        /// {1} : language id
        /// {2} : groups of the current user
        /// {3} : current store ID
        /// </remarks>
        public const string COLLECTION_NAVIGATION_MODEL_KEY = "Grand.collection.navigation-{0}-{1}-{2}-{3}";
        
        /// <summary>
        /// Key for CollectionNavigationModel caching
        /// </summary>
        /// <remarks>
        /// {0} : language id
        /// {1} : current store ID
        /// </remarks>
        public const string COLLECTION_NAVIGATION_MENU = "Grand.collection.navigation.menu-{0}-{1}.pres";

        /// <summary>
        /// Key for caching of collection displayed on home page
        /// </summary>
        /// <remarks>
        /// {0} : current store ID
        /// {1} : language ID
        /// </remarks>
        public const string COLLECTION_HOMEPAGE_KEY = "Grand.collection.navigation.homepage-{0}-{1}.pres";

        
        /// <summary>
        /// Key for caching of collection displayed on home page
        /// </summary>
        /// <remarks>
        /// {0} : customer group
        /// {1} : store ID
        /// {2} : language ID
        /// </remarks>
        public const string COLLECTION_FEATURED_PRODUCT_HOMEPAGE_KEY = "Grand.collection.navigation.homepage-fp-{0}-{1}-{2}.pres";

        /// <summary>
        /// Key for caching of a value indicating whether a collection has featured products
        /// </summary>
        /// <remarks>
        /// {0} : collection id
        /// {1} : groups of the current user
        /// {2} : current store ID
        /// </remarks>
        public const string COLLECTION_HAS_FEATURED_PRODUCTS_KEY = "Grand.productcollection.hasfeaturedproducts-{0}-{1}-{2}";

        /// <summary>
        /// Key for List of CollectionModel caching
        /// </summary>
        /// <remarks>
        /// {0} : language id
        /// {1} : comma separated list of customer groups
        /// {2} : current store ID
        /// </remarks>
        public const string COLLECTION_ALL_MODEL_KEY = "Grand.collection.navigation.all-{0}-{1}-{2}.pres";

        /// <summary>
        /// Key for VendorNavigationModel caching
        /// </summary>
        public const string VENDOR_NAVIGATION_MODEL_KEY = "Grand.pres.vendor.navigation";
        public const string VENDOR_NAVIGATION_PATTERN_KEY = "Grand.pres.vendor.navigation";

        /// <summary>
        /// Key for List of VendorModel caching
        /// </summary>
        public const string VENDOR_ALL_MODEL_KEY = "Grand.pres.vendor.navigation.all";

        
        /// <summary>
        /// Key for CategoryNavigationModel caching
        /// </summary>
        /// <remarks>
        /// {0} : language id
        /// {1} : comma separated list of customer groups
        /// {2} : current store ID
        /// {3} : parent category Id
        /// </remarks>
        public const string CATEGORY_ALL_MODEL_KEY = "Grand.category.all-{0}-{1}-{2}-{3}.pres";

        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : store ID
        /// {1} : customer groups
        /// </remarks>
        public static string CATEGORIES_BY_MENU => "Grand.category.menu-{0}-{1}.pres";

        /// <summary>
        /// Key for CategorySearchBoxModel caching
        /// </summary>
        /// <remarks>
        /// {1} : comma separated list of customer groups
        /// {2} : current store ID
        /// </remarks>
        public const string CATEGORY_ALL_SEARCHBOX = "Grand.category.all.searchbox-{0}-{1}";

        
        /// <summary>
        /// Key for caching of a value indicating whether a category has featured products
        /// </summary>
        /// <remarks>
        /// {0} : category id
        /// {1} : groups of the current user
        /// {2} : current store ID
        /// </remarks>
        public const string CATEGORY_HAS_FEATURED_PRODUCTS_KEY = "Grand.productcategory.hasfeaturedproducts-{0}-{1}-{2}.pres";

        /// <summary>
        /// Key for caching of category breadcrumb
        /// </summary>
        /// <remarks>
        /// {0} : category id
        /// {1} : groups of the current user
        /// {2} : current store ID
        /// {3} : language ID
        /// </remarks>
        public const string CATEGORY_BREADCRUMB_KEY = "Grand.category.breadcrumb-{0}-{1}-{2}-{3}.pres";

        /// <summary>
        /// Key for caching of knowledgebase category breadcrumb
        /// </summary>
        /// <remarks>
        /// {0} : category id
        /// {1} : groups of the current user
        /// {2} : current store ID
        /// {3} : language ID
        /// </remarks>
        public const string KNOWLEDGEBASE_CATEGORY_BREADCRUMB_KEY = "Grand.knowledgebase.category.breadcrumb-{0}-{1}-{2}-{3}";

        /// <summary>
        /// Key for caching of categories displayed on home page
        /// </summary>
        /// <remarks>
        /// {0} : groups of the current user
        /// {1} : current store ID
        /// {2} : language ID
        /// </remarks>
        public const string CATEGORY_HOMEPAGE_KEY = "Grand.category.homepage-{0}-{1}-{2}";
        public const string CATEGORY_FEATURED_PRODUCTS_HOMEPAGE_KEY = "Grand.category.homepage-fp-{0}-{1}-{2}";

        /// <summary>
        /// Key for GetChildCategoryIds method results caching
        /// </summary>
        /// <remarks>
        /// {0} : parent category id
        /// {1} : comma separated list of customer groups
        /// {2} : current store ID
        /// </remarks>
        public const string CATEGORY_CHILD_IDENTIFIERS_MODEL_KEY = "Grand.category.childidentifiers-{0}-{1}-{2}.pres";

        /// <summary>
        /// Key for ProductBreadcrumbModel caching
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {1} : language id
        /// {2} : comma separated list of customer groups
        /// {3} : current store ID
        /// </remarks>
        public const string PRODUCT_BREADCRUMB_MODEL_KEY = "Grand.product.id-{0}-{1}-{2}-{3}.breadcrumb.pres";

        /// <summary>
        /// Key for ProductTagModel caching
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {1} : language id
        /// {2} : current store ID
        /// </remarks>
        public const string PRODUCTTAG_BY_PRODUCT_MODEL_KEY = "Grand.producttag.byproduct-{0}-{1}-{2}.pres";

        /// <summary>
        /// Key for PopularProductTagsModel caching
        /// </summary>
        /// <remarks>
        /// {0} : language id
        /// {1} : current store ID
        /// </remarks>
        public const string PRODUCTTAG_POPULAR_MODEL_KEY = "Grand.producttag.popular-{0}-{1}.pres";

        /// <summary>
        /// Key for ProductCollections model caching
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {1} : language id
        /// {2} : groups of the current user
        /// {3} : current store ID
        /// </remarks>
        public const string PRODUCT_COLLECTIONS_MODEL_KEY = "Grand.product.id-{0}-{1}-{2}-{3}.collections";
               
        /// <summary>
        /// Key for bestsellers identifiers displayed on the home page - best seller
        /// </summary>
        /// <remarks>
        /// {0} : current store ID
        /// </remarks>
        public const string HOMEPAGE_BESTSELLERS_IDS_KEY = "Grand.pres.bestsellers.homepage-{0}";


        /// <summary>
        /// Key for Show on home page product model caching
        /// </summary>
        /// <remarks>
        /// </remarks>
        public const string HOMEPAGE_PRODUCTS_MODEL_KEY = "Grand.product.homepage";

        /// <summary>
        /// Key for Show best seller product model caching
        /// </summary>
        /// <remarks>
        /// </remarks>
        public const string BESTSELLER_PRODUCTS_MODEL_KEY = "Grand.product.homepage.bestseller";

        /// <summary>
        /// Key for "also purchased" product identifiers displayed on the product details page
        /// </summary>
        /// <remarks>
        /// {0} : current product id
        /// {1} : current store ID
        /// </remarks>
        public const string PRODUCTS_ALSO_PURCHASED_IDS_KEY = "Grand.product.id-{0}-{1}.press.alsopurchased";

        /// <summary>
        /// Key for "related" product identifiers displayed on the product details page
        /// </summary>
        /// <remarks>
        /// {0} : current product id
        /// {1} : current store ID
        /// </remarks>
        public const string PRODUCTS_RELATED_IDS_KEY = "Grand.product.id-{0}-{1}.pres.related";        

        /// <summary>
        /// Key for blog tag list model
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// {1} : current store ID
        /// </remarks>
        public const string BLOG_TAGS_MODEL_KEY = "Grand.pres.blog.tags-{0}-{1}";
        /// <summary>
        /// Key for blog archive (years, months) block model
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// {1} : current store ID
        /// </remarks>
        public const string BLOG_MONTHS_MODEL_KEY = "Grand.pres.blog.months-{0}-{1}";
        public const string BLOG_HOMEPAGE_MODEL_KEY = "Grand.pres.blog.homepage-{0}-{1}";
        public const string BLOG_CATEGORY_MODEL_KEY = "Grand.pres.blog.category-{0}-{1}";
        public const string BLOG_PATTERN_KEY = "Grand.pres.blog";

        /// <summary>
        /// Key for home page news
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// {1} : current store ID
        /// </remarks>
        public const string HOMEPAGE_NEWSMODEL_KEY = "Grand.pres.news.homepage-{0}-{1}";
        public const string NEWS_PATTERN_KEY = "Grand.pres.news";

        /// <summary>
        /// Key for sitemap on the sitemap page
        /// </summary>
        /// <remarks>
        /// {0} : language id
        /// {1} : groups of the current user
        /// {2} : current store ID
        /// </remarks>
        public const string SITEMAP_PAGE_MODEL_KEY = "Grand.pres.sitemap.page-{0}-{1}-{2}";

        /// <summary>
        /// Key for widget info
        /// </summary>
        /// <remarks>
        /// {0} : current store ID
        /// {1} : customer groups
        /// {2} : widget zone
        /// </remarks>
        public const string WIDGET_MODEL_KEY = "Grand.pres.widget-{0}-{1}-{2}";
       
    }
}
