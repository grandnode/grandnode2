using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Settings
{
    public partial class CatalogSettingsModel : BaseModel
    {
        public CatalogSettingsModel()
        {
            DefaultViewModes = new List<SelectListItem>();
            DefaultViewModes.Add(new SelectListItem() { Text = "grid", Value = "grid" });
            DefaultViewModes.Add(new SelectListItem() { Text = "list", Value = "list" });
        }

        
        public string ActiveStore { get; set; }


        [GrandResourceDisplayName("Admin.Settings.Catalog.AllowViewUnpublishedProductPage")]
        public bool AllowViewUnpublishedProductPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.DisplayDiscontinuedMessageForUnpublishedProducts")]
        public bool DisplayDiscontinuedMessageForUnpublishedProducts { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowSkuOnProductDetailsPage")]
        public bool ShowSkuOnProductDetailsPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowSkuOnCatalogPages")]
        public bool ShowSkuOnCatalogPages { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowMpn")]
        public bool ShowMpn { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowGtin")]
        public bool ShowGtin { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowFreeShippingNotification")]
        public bool ShowFreeShippingNotification { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.AllowProductSorting")]
        public bool AllowProductSorting { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.AllowProductViewModeChanging")]
        public bool AllowProductViewModeChanging { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowProductsFromSubcategories")]
        public bool ShowProductsFromSubcategories { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowCategoryProductNumber")]
        public bool ShowCategoryProductNumber { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowCategoryProductNumberIncludingSubcategories")]
        public bool ShowCategoryProductNumberIncludingSubcategories { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.CategoryBreadcrumbEnabled")]
        public bool CategoryBreadcrumbEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowShareButton")]
        public bool ShowShareButton { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.PageShareCode")]
        public string PageShareCode { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ProductReviewsMustBeApproved")]
        public bool ProductReviewsMustBeApproved { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.AllowAnonymousUsersToReviewProduct")]
        public bool AllowAnonymousUsersToReviewProduct { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ProductReviewPossibleOnlyAfterPurchasing")]
        public bool ProductReviewPossibleOnlyAfterPurchasing { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.NotifyStoreOwnerAboutNewProductReviews")]
        public bool NotifyStoreOwnerAboutNewProductReviews { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.EmailAFriendEnabled")]
        public bool EmailAFriendEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.AskQuestionEnabled")]
        public bool AskQuestionEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.AskQuestionOnProduct")]
        public bool AskQuestionOnProduct { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.AllowAnonymousUsersToEmailAFriend")]
        public bool AllowAnonymousUsersToEmailAFriend { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.RecentlyViewedProductsNumber")]
        public int RecentlyViewedProductsNumber { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.RecentlyViewedProductsEnabled")]
        public bool RecentlyViewedProductsEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.RecommendedProductsEnabled")]
        public bool RecommendedProductsEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.SuggestedProductsEnabled")]
        public bool SuggestedProductsEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.SuggestedProductsNumber")]
        public int SuggestedProductsNumber { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.PersonalizedProductsEnabled")]
        public bool PersonalizedProductsEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.PersonalizedProductsNumber")]
        public int PersonalizedProductsNumber { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.NewProductsNumber")]
        public int NewProductsNumber { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.NewProductsEnabled")]
        public bool NewProductsEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.NewProductsNumberOnHomePage")]
        public int NewProductsNumberOnHomePage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.NewProductsOnHomePage")]
        public bool NewProductsOnHomePage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.CompareProductsEnabled")]
        public bool CompareProductsEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowBestsellersOnHomepage")]
        public bool ShowBestsellersOnHomepage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.NumberOfBestsellersOnHomepage")]
        public int NumberOfBestsellersOnHomepage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.BestsellersFromReports")]
        public bool BestsellersFromReports { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.SearchPageProductsPerPage")]
        public int SearchPageProductsPerPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.SearchPageAllowCustomersToSelectPageSize")]
        public bool SearchPageAllowCustomersToSelectPageSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.SearchPagePageSizeOptions")]
        public string SearchPagePageSizeOptions { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ProductSearchAutoCompleteEnabled")]
        public bool ProductSearchAutoCompleteEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ProductSearchAutoCompleteNumberOfProducts")]
        public int ProductSearchAutoCompleteNumberOfProducts { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowProductImagesInSearchAutoComplete")]
        public bool ShowProductImagesInSearchAutoComplete { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ProductSearchTermMinimumLength")]
        public int ProductSearchTermMinimumLength { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ProductsAlsoPurchasedEnabled")]
        public bool ProductsAlsoPurchasedEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ProductsAlsoPurchasedNumber")]
        public int ProductsAlsoPurchasedNumber { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.NumberOfProductTags")]
        public int NumberOfProductTags { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ProductsByTagPageSize")]
        public int ProductsByTagPageSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ProductsByTagAllowCustomersToSelectPageSize")]
        public bool ProductsByTagAllowCustomersToSelectPageSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ProductsByTagPageSizeOptions")]
        public string ProductsByTagPageSizeOptions { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.IncludeShortDescriptionInCompareProducts")]
        public bool IncludeShortDescriptionInCompareProducts { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.IncludeFullDescriptionInCompareProducts")]
        public bool IncludeFullDescriptionInCompareProducts { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.IgnoreDiscounts")]
        public bool IgnoreDiscounts { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.IgnoreFeaturedProducts")]
        public bool IgnoreFeaturedProducts { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.IgnoreFilterableSpecAttributeOption")]
        public bool IgnoreFilterableSpecAttributeOption { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.IgnoreFilterableAvailableStartEndDateTime")]
        public bool IgnoreFilterableAvailableStartEndDateTime { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.CustomerProductPrice")]
        public bool CustomerProductPrice { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.CollectionsBlockItemsToDisplay")]
        public int CollectionsBlockItemsToDisplay { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowSpecAttributeOnCatalogPages")]
        public bool ShowSpecAttributeOnCatalogPages { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.SecondPictureOnCatalogPages")]
        public bool SecondPictureOnCatalogPages { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.DefaultProductRatingValue")]
        public int DefaultProductRatingValue { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.SearchBySku")]
        public bool SearchBySku { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.SearchByDescription")]
        public bool SearchByDescription { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.SaveSearchAutoComplete")]
        public bool SaveSearchAutoComplete { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.PeriodBestsellers")]
        public int PeriodBestsellers { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.IncludeFeaturedProductsInNormalLists")]
        public bool IncludeFeaturedProductsInNormalLists { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.DisplayTierPricesWithDiscounts")]
        public bool DisplayTierPricesWithDiscounts { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.DisplayQuantityOnCatalogPages")]
        public bool DisplayQuantityOnCatalogPages { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.SortingByAvailability")]
        public bool SortingByAvailability { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.LimitOfFeaturedProducts")]
        public int LimitOfFeaturedProducts { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.NumberOfReview")]
        public int NumberOfReview { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.DefaultViewMode")]
        public string DefaultViewMode { get; set; }
        public IList<SelectListItem> DefaultViewModes { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.DefaultCollectionPageSize")]
        public int DefaultCollectionPageSize { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.ShowProductsFromSubcategoriesInSearchBox")]
        public bool ShowProductsFromSubcategoriesInSearchBox { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Catalog.PublishBackProductWhenCancellingOrders")]
        public bool PublishBackProductWhenCancellingOrders { get; set; }
    }
}