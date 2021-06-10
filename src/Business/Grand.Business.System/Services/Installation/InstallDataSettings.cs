using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.AdminSearch;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Cms;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Messages;
using Grand.Domain.News;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.PushNotifications;
using Grand.Domain.Security;
using Grand.Domain.Seo;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Domain.Vendors;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallSettings(bool installSampleData)
        {
            var _settingService = _serviceProvider.GetRequiredService<ISettingService>();

            await _settingService.SaveSetting(new MenuItemSettings {
                DisplayHomePageMenu = !installSampleData,
                DisplayNewProductsMenu = true,
                DisplaySearchMenu = !installSampleData,
                DisplayCustomerMenu = !installSampleData,
                DisplayBlogMenu = true,
                DisplayContactUsMenu = true
            });

            await _settingService.SaveSetting(new PdfSettings {
                LogoPictureId = "",
                InvoiceHeaderText = null,
                InvoiceFooterText = null,
            });

            await _settingService.SaveSetting(new CommonSettings {
                StoreInDatabaseContactUsForm = true,
                UseSystemEmailForContactUsForm = true,
                SitemapEnabled = true,
                SitemapIncludeCategories = true,
                SitemapIncludeBrands = true,
                SitemapIncludeProducts = false,
                Log404Errors = true,
                PopupForTermsOfServiceLinks = true,
                AllowToSelectStore = false,
            });
            await _settingService.SaveSetting(new SystemSettings {
                DeleteGuestTaskOlderThanMinutes = 1440,
            });
            await _settingService.SaveSetting(new SecuritySettings {
                AdminAreaAllowedIpAddresses = null
            });
            await _settingService.SaveSetting(new MediaSettings {
                BlogThumbPictureSize = 450,
                ProductThumbPictureSize = 415,
                ProductDetailsPictureSize = 800,
                ProductThumbPictureSizeOnProductDetailsPage = 100,
                AssociatedProductPictureSize = 220,
                CategoryThumbPictureSize = 450,
                BrandThumbPictureSize = 420,
                CollectionThumbPictureSize = 420,
                VendorThumbPictureSize = 450,
                CourseThumbPictureSize = 200,
                LessonThumbPictureSize = 64,
                CartThumbPictureSize = 80,
                MiniCartThumbPictureSize = 100,
                AddToCartThumbPictureSize = 200,
                AutoCompleteSearchThumbPictureSize = 50,
                ImageSquarePictureSize = 32,
                MaximumImageSize = 1980,
                DefaultPictureZoomEnabled = true,
                StoreLocation = "/",
                StoreInDb = true
            });

            await _settingService.SaveSetting(new SeoSettings {
                PageTitleSeparator = ". ",
                PageTitleSeoAdjustment = true,
                DefaultTitle = "Your store",
                DefaultMetaKeywords = "",
                DefaultMetaDescription = "",
                GenerateProductMetaDescription = true,
                ConvertNonWesternChars = false,
                SeoCharConversion = "ą:a;ę:e;ó:o;ć:c;ł:l;ś:s;ź:z;ż:z",
                AllowUnicodeCharsInUrls = true,
                CanonicalUrlsEnabled = false,
                TwitterMetaTags = true,
                OpenGraphMetaTags = true,
                ReservedEntityUrlSlugs = new List<string>
                    {
                        "admin",
                        "install",
                        "recentlyviewedproducts",
                        "newproducts",
                        "compareproducts",
                        "clearcomparelist",
                        "setproductreviewhelpfulness",
                        "login",
                        "register",
                        "logout",
                        "cart",
                        "wishlist",
                        "emailwishlist",
                        "checkout",
                        "onepagecheckout",
                        "contactus",
                        "passwordrecovery",
                        "subscribenewsletter",
                        "blog",
                        "knowledgebase",
                        "news",
                        "sitemap",
                        "search",
                        "config",
                        "cookieaccept",
                        "access-denied",
                        "page-not-found",
                        "home",
                        "con",
                        "lpt1",
                        "lpt2",
                        "lpt3",
                        "lpt4",
                        "lpt5",
                        "lpt6",
                        "lpt7",
                        "lpt8",
                        "lpt9",
                        "com1",
                        "com2",
                        "com3",
                        "com4",
                        "com5",
                        "com6",
                        "com7",
                        "com8",
                        "com9",
                        "null",
                        "prn",
                        "aux"
                    },
            });

            await _settingService.SaveSetting(new AdminAreaSettings {
                DefaultGridPageSize = 15,
                GridPageSizes = "10, 15, 20, 50, 100",
                UseIsoDateTimeConverterInJson = true,
            });

            await _settingService.SaveSetting(new CatalogSettings {
                AllowViewUnpublishedProductPage = true,
                DisplayDiscontinuedMessageForUnpublishedProducts = true,
                PublishBackProductWhenCancellingOrders = false,
                ShowSkuOnProductDetailsPage = false,
                ShowSkuOnCatalogPages = false,
                ShowMpn = false,
                ShowGtin = false,
                ShowFreeShippingNotification = true,
                AllowProductSorting = true,
                AllowProductViewModeChanging = true,
                DefaultViewMode = "grid",
                ShowProductsFromSubcategories = true,
                ShowCategoryProductNumber = false,
                ShowCategoryProductNumberIncludingSubcategories = false,
                CategoryBreadcrumbEnabled = true,
                ShowShareButton = false,
                PageShareCode = "<!-- AddThis Button BEGIN --><div class=\"addthis_inline_share_toolbox\"></div><script type=\"text/javascript\" src=\"//s7.addthis.com/js/300/addthis_widget.js#pubid=ra-5bbf4b026e74abf6\"></script><!-- AddThis Button END -->",
                ProductReviewsMustBeApproved = false,
                DefaultProductRatingValue = 5,
                AllowAnonymousUsersToReviewProduct = false,
                ProductReviewPossibleOnlyAfterPurchasing = false,
                NotifyStoreOwnerAboutNewProductReviews = false,
                EmailAFriendEnabled = true,
                AskQuestionEnabled = false,
                AskQuestionOnProduct = true,
                AllowAnonymousUsersToEmailAFriend = false,
                RecentlyViewedProductsNumber = 3,
                RecentlyViewedProductsEnabled = true,
                RecommendedProductsEnabled = false,
                SuggestedProductsEnabled = false,
                SuggestedProductsNumber = 6,
                PersonalizedProductsEnabled = false,
                PersonalizedProductsNumber = 6,
                NewProductsNumber = 6,
                NewProductsEnabled = true,
                NewProductsOnHomePage = false,
                NewProductsNumberOnHomePage = 6,
                CompareProductsEnabled = true,
                CompareProductsNumber = 4,
                ProductSearchAutoCompleteEnabled = true,
                ProductSearchAutoCompleteNumberOfProducts = 10,
                ProductSearchTermMinimumLength = 3,
                ShowProductImagesInSearchAutoComplete = true,
                ShowBestsellersOnHomepage = false,
                NumberOfBestsellersOnHomepage = 4,
                BestsellersFromReports = false,
                PeriodBestsellers = 6,
                NumberOfReview = 10,
                SearchPageProductsPerPage = 6,
                SearchPageAllowCustomersToSelectPageSize = true,
                SearchPagePageSizeOptions = "6, 3, 9, 18",
                ProductsAlsoPurchasedEnabled = true,
                ProductsAlsoPurchasedNumber = 3,
                NumberOfProductTags = 15,
                ProductsByTagPageSize = 6,
                IncludeShortDescriptionInCompareProducts = false,
                IncludeFullDescriptionInCompareProducts = false,
                IncludeFeaturedProductsInNormalLists = false,
                DisplayTierPricesWithDiscounts = true,
                IgnoreDiscounts = false,
                IgnoreFeaturedProducts = false,
                IgnoreFilterableSpecAttributeOption = false,
                IgnoreFilterableAvailableStartEndDateTime = true,
                CustomerProductPrice = false,
                ProductsByTagAllowCustomersToSelectPageSize = true,
                ProductsByTagPageSizeOptions = "6, 3, 9, 18",
                CollectionsBlockItemsToDisplay = 2,
                DefaultCategoryPageSizeOptions = "6, 3, 9",
                DefaultCollectionPageSize = 6,
                LimitOfFeaturedProducts = 30,
                SecondPictureOnCatalogPages = true
            });

            await _settingService.SaveSetting(new LanguageSettings {
                DefaultAdminLanguageId = _languageRepository.Table.Single(l => l.Name == "English").Id,
                AutomaticallyDetectLanguage = false,
                IgnoreRtlPropertyForAdminArea = false,
            });

            await _settingService.SaveSetting(new CustomerSettings {
                UsernamesEnabled = false,
                CheckUsernameAvailabilityEnabled = false,
                AllowUsersToChangeUsernames = false,
                DefaultPasswordFormat = PasswordFormat.Hashed,
                HashedPasswordFormat = HashedPasswordFormat.SHA1,
                PasswordRegularExpression = "^.{6,}$",
                PasswordRecoveryLinkDaysValid = 7,
                PasswordLifetime = 90,
                FailedPasswordAllowedAttempts = 0,
                FailedPasswordLockoutMinutes = 30,
                UserRegistrationType = UserRegistrationType.Standard,
                NotifyNewCustomerRegistration = false,
                HideDownloadableProductsTab = true,
                HideReviewsTab = false,
                HideCoursesTab = true,
                HideSubAccountsTab = true,
                HideOutOfStockSubscriptionsTab = false,
                HideAuctionsTab = true,
                HideNotesTab = true,
                HideDocumentsTab = true,
                DownloadableProductsValidateUser = true,
                CustomerNameFormat = CustomerNameFormat.FirstName,
                GenderEnabled = false,
                GeoEnabled = false,
                DateOfBirthEnabled = false,
                DateOfBirthRequired = false,
                DateOfBirthMinimumAge = 0,
                CompanyEnabled = false,
                StreetAddressEnabled = false,
                StreetAddress2Enabled = false,
                ZipPostalCodeEnabled = false,
                CityEnabled = false,
                CountryEnabled = false,
                CountryRequired = false,
                StateProvinceEnabled = false,
                StateProvinceRequired = false,
                PhoneEnabled = false,
                FaxEnabled = false,
                AcceptPrivacyPolicyEnabled = false,
                NewsletterEnabled = true,
                NewsletterTickedByDefault = true,
                HideNewsletterBlock = false,
                RegistrationFreeShipping = false,
                NewsletterBlockAllowToUnsubscribe = false,
                OnlineCustomerMinutes = 20,
                OnlineShoppingCartMinutes = 60,
                StoreLastVisitedPage = true,
                SaveVisitedPage = false,
                AllowUsersToDeleteAccount = false,
                AllowUsersToExportData = false,
                TwoFactorAuthenticationEnabled = false,
            });

            await _settingService.SaveSetting(new AddressSettings {
                CompanyEnabled = true,
                StreetAddressEnabled = true,
                StreetAddressRequired = true,
                StreetAddress2Enabled = true,
                ZipPostalCodeEnabled = true,
                ZipPostalCodeRequired = true,
                CityEnabled = true,
                CityRequired = true,
                CountryEnabled = true,
                StateProvinceEnabled = true,
                PhoneEnabled = true,
                PhoneRequired = true,
                FaxEnabled = false,
                NoteEnabled = false,
            });

            await _settingService.SaveSetting(new StoreInformationSettings {
                StoreClosed = false,
                DefaultStoreTheme = "Default",
                AllowCustomerToSelectTheme = false,
                DisplayCookieInformation = false,
                LogoPicture = "logo.png",
                FacebookLink = "https://www.facebook.com/grandnodecom",
                TwitterLink = "https://twitter.com/grandnode",
                YoutubeLink = "http://www.youtube.com/user/grandnode",
                InstagramLink = "https://www.instagram.com/grandnode/",
                LinkedInLink = "https://www.linkedin.com/company/grandnode.com/",
                PinterestLink = "",
            });

            await _settingService.SaveSetting(new LoyaltyPointsSettings {
                Enabled = true,
                ExchangeRate = 1,
                PointsForRegistration = 0,
                PointsForPurchases_Amount = 10,
                PointsForPurchases_Points = 1,
                PointsForPurchases_Awarded = (int)OrderStatusSystem.Complete,
                ReduceLoyaltyPointsAfterCancelOrder = true,
                DisplayHowMuchWillBeEarned = true,
                PointsAccumulatedForAllStores = true,
            });

            await _settingService.SaveSetting(new CurrencySettings {
                PrimaryStoreCurrencyId = _currencyRepository.Table.Single(c => c.CurrencyCode == "USD").Id,
                PrimaryExchangeRateCurrencyId = _currencyRepository.Table.Single(c => c.CurrencyCode == "USD").Id,
                ActiveExchangeRateProviderSystemName = "CurrencyExchange.MoneyConverter",
                AutoUpdateEnabled = false
            });

            await _settingService.SaveSetting(new MeasureSettings {
                BaseDimensionId = _measureDimensionRepository.Table.Single(m => m.SystemKeyword == "centimetres").Id,
                BaseWeightId = _measureWeightRepository.Table.Single(m => m.SystemKeyword == "lb").Id,
            });

            await _settingService.SaveSetting(new ShoppingCartSettings {
                DisplayCartAfterAddingProduct = false,
                DisplayWishlistAfterAddingProduct = false,
                MaximumShoppingCartItems = 1000,
                MaximumWishlistItems = 1000,
                AllowOutOfStockItemsToBeAddedToWishlist = false,
                MoveItemsFromWishlistToCart = true,
                ShowProductImagesOnShoppingCart = true,
                ShowProductImagesOnWishList = true,
                ShowDiscountBox = true,
                ShowGiftVoucherBox = true,
                CrossSellsNumber = 4,
                EmailWishlistEnabled = true,
                AllowAnonymousUsersToEmailWishlist = false,
                MiniShoppingCartEnabled = true,
                ShowImagesInsidebarCart = true,
                MiniCartProductNumber = 5,
                RoundPrices = true,
                GroupTierPrices = false,
                AllowCartItemEditing = true,
                AllowOnHoldCart = true,
            });

            await _settingService.SaveSetting(new OrderSettings {
                IsReOrderAllowed = true,
                MinOrderSubtotalAmount = 0,
                MinOrderSubtotalAmountIncludingTax = false,
                MinOrderTotalAmount = 0,
                AnonymousCheckoutAllowed = true,
                TermsOfServiceOnShoppingCartPage = true,
                TermsOfServiceOnOrderConfirmPage = false,
                DisableOrderCompletedPage = false,
                AttachPdfInvoiceToOrderPlacedEmail = false,
                AttachPdfInvoiceToOrderCompletedEmail = false,
                AttachPdfInvoiceToOrderPaidEmail = false,
                MerchandiseReturnsEnabled = true,
                MerchandiseReturns_AllowToSpecifyPickupAddress = false,
                MerchandiseReturns_AllowToSpecifyPickupDate = false,
                MerchandiseReturns_PickupDateRequired = false,
                NumberOfDaysMerchandiseReturnAvailable = 365,
                MinimumOrderPlacementInterval = 30,
                DeactivateGiftVouchersAfterDeletingOrder = true,
                DeactivateGiftVouchersAfterCancelOrder = true,
                GiftVouchers_Activated_OrderStatusId = 30,
                CompleteOrderWhenDelivered = true,
                UserCanCancelUnpaidOrder = false,
                LengthCode = 8,
                PageSize = 10
            });

            await _settingService.SaveSetting(new ShippingSettings {
                AllowPickUpInStore = true,
                FreeShippingOverXEnabled = false,
                FreeShippingOverXValue = 0,
                FreeShippingOverXIncludingTax = false,
                EstimateShippingEnabled = false,
                DisplayShipmentEventsToCustomers = false,
                DisplayShipmentEventsToStoreOwner = false,
                SkipShippingMethodSelectionIfOnlyOne = false,
            });

            await _settingService.SaveSetting(new ShippingProviderSettings {
                ActiveSystemNames = new List<string> { "Shipping.FixedRate" },
            });

            await _settingService.SaveSetting(new PaymentSettings {
                ActivePaymentProviderSystemNames = new List<string>
                    {
                        "Payments.CashOnDelivery",
                        "Payments.PayPalStandard",
                        "Payments.BrainTree",
                    },
                AllowRePostingPayments = true,
                SkipPaymentIfOnlyOne = true,
                ShowPaymentDescriptions = true,
                SkipPaymentInfo = false,
            });

            await _settingService.SaveSetting(new TaxSettings {
                TaxBasedOn = TaxBasedOn.BillingAddress,
                TaxDisplayType = TaxDisplayType.ExcludingTax,
                DisplayTaxSuffix = false,
                DisplayTaxRates = false,
                PricesIncludeTax = false,
                CalculateRoundPrice = 2,
                MidpointRounding = MidpointRounding.ToEven,
                AllowCustomersToSelectTaxDisplayType = false,
                ForceTaxExclusionFromOrderSubtotal = false,
                HideZeroTax = false,
                HideTaxInOrderSummary = false,
                DefaultTaxCategoryId = "",
                ShippingIsTaxable = false,
                ShippingPriceIncludesTax = false,
                ShippingTaxCategoryId = "",
                PaymentMethodAdditionalFeeIsTaxable = false,
                PaymentMethodAdditionalFeeIncludesTax = false,
                PaymentMethodAdditionalFeeTaxCategoryId = "",
                EuVatEnabled = false,
                EuVatShopCountryId = "",
                EuVatAllowVatExemption = true,
                EuVatUseWebService = false,
                EuVatAssumeValid = false
            });

            await _settingService.SaveSetting(new TaxProviderSettings {
                ActiveTaxProviderSystemName = "Tax.FixedRate",
            });

            await _settingService.SaveSetting(new DateTimeSettings {
                DefaultStoreTimeZoneId = "",
            });

            await _settingService.SaveSetting(new BlogSettings {
                Enabled = true,
                PostsPageSize = 10,
                AllowNotRegisteredUsersToLeaveComments = false,
                NotifyAboutNewBlogComments = false,
                NumberOfTags = 15,
                ShowBlogOnHomePage = false,
                HomePageBlogCount = 3,
                MaxTextSizeHomePage = 200
            });

            await _settingService.SaveSetting(new KnowledgebaseSettings {
                Enabled = false,
                AllowNotRegisteredUsersToLeaveComments = false,
                NotifyAboutNewArticleComments = false
            });

            await _settingService.SaveSetting(new PushNotificationsSettings {
                Enabled = false,
                AllowGuestNotifications = true
            });

            await _settingService.SaveSetting(new AdminSearchSettings {
                BlogsDisplayOrder = 0,
                CategoriesDisplayOrder = 0,
                CustomersDisplayOrder = 0,
                CollectionsDisplayOrder = 0,
                MaxSearchResultsCount = 10,
                MinSearchTermLength = 3,
                NewsDisplayOrder = 0,
                OrdersDisplayOrder = 0,
                ProductsDisplayOrder = 0,
                SearchInBlogs = true,
                SearchInCategories = true,
                SearchInCustomers = true,
                SearchInCollections = true,
                SearchInNews = true,
                SearchInOrders = true,
                SearchInProducts = true,
                SearchInPages = true,
                PagesDisplayOrder = 0,
                SearchInMenu = true,
                MenuDisplayOrder = -1,
                CategorySizeLimit = 100,
                BrandSizeLimit = 100,
                CollectionSizeLimit = 100,
                VendorSizeLimit = 100,
                CustomerGroupSizeLimit = 100,
            });

            await _settingService.SaveSetting(new NewsSettings {
                Enabled = true,
                AllowNotRegisteredUsersToLeaveComments = false,
                NotifyAboutNewNewsComments = false,
                ShowNewsOnMainPage = true,
                MainPageNewsCount = 3,
                NewsArchivePageSize = 10
            });

            await _settingService.SaveSetting(new VendorSettings {
                DefaultVendorPageSizeOptions = "6, 3, 9",
                VendorsBlockItemsToDisplay = 0,
                ShowVendorOnProductDetailsPage = true,
                AllowCustomersToContactVendors = true,
                AllowCustomersToApplyForVendorAccount = false,
                AllowAnonymousUsersToReviewVendor = false,
                DefaultVendorRatingValue = 5,
                VendorReviewsMustBeApproved = true,
                VendorReviewPossibleOnlyAfterPurchasing = true,
                NotifyVendorAboutNewVendorReviews = true,
            });

            var eaGeneral = _emailAccountRepository.Table.FirstOrDefault();
            if (eaGeneral == null)
                throw new Exception("Default email account cannot be loaded");
            await _settingService.SaveSetting(new EmailAccountSettings {
                DefaultEmailAccountId = eaGeneral.Id
            });

            await _settingService.SaveSetting(new WidgetSettings {
                ActiveWidgetSystemNames = new List<string> { "Widgets.Slider" },
            });

            await _settingService.SaveSetting(new GoogleAnalyticsSettings() {
                gaprivateKey = "",
                gaserviceAccountEmail = "",
                gaviewID = ""
            });
        }
    }
}
