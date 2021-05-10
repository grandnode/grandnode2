using Grand.Infrastructure.ModelBinding;

namespace Grand.Web.Admin.Models.Settings
{
    public class AdminSearchSettingsModel
    {
        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.SearchInProducts")]
        public bool SearchInProducts { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.ProductsDisplayOrder")]
        public int ProductsDisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.SearchInCategories")]
        public bool SearchInCategories { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.CategoriesDisplayOrder")]
        public int CategoriesDisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.SearchInCollections")]
        public bool SearchInCollections { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.CollectionsDisplayOrder")]
        public int CollectionsDisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.SearchInPages")]
        public bool SearchInPages { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.PagesDisplayOrder")]
        public int PagesDisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.SearchInNews")]
        public bool SearchInNews { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.NewsDisplayOrder")]
        public int NewsDisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.SearchInBlogs")]
        public bool SearchInBlogs { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.BlogsDisplayOrder")]
        public int BlogsDisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.SearchInCustomers")]
        public bool SearchInCustomers { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.CustomersDisplayOrder")]
        public int CustomersDisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.SearchInOrders")]
        public bool SearchInOrders { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.OrdersDisplayOrder")]
        public int OrdersDisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.MinSearchTermLength")]
        public int MinSearchTermLength { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.MaxSearchResultsCount")]
        public int MaxSearchResultsCount { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.SearchInMenu")]
        public bool SearchInMenu { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.MenuDisplayOrder")]
        public int MenuDisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.CategorySizeLimit")]
        public int CategorySizeLimit { get; set; }
        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.BrandSizeLimit")]
        public int BrandSizeLimit { get; set; }
        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.CollectionSizeLimit")]
        public int CollectionSizeLimit { get; set; }
        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.VendorSizeLimit")]
        public int VendorSizeLimit { get; set; }
        [GrandResourceDisplayName("Admin.Settings.AdminSearch.Fields.CustomerGroupSizeLimit")]
        public int CustomerGroupSizeLimit { get; set; }

    }
}
