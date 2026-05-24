using Grand.Mapping;
using Grand.Domain.Admin;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Media;
using Grand.Domain.News;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.PushNotifications;
using Grand.Domain.Seo;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.AdminShared.Models.Tax;
using Grand.Web.Common.Security.Captcha;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;

namespace Grand.Mapping.Tests.AdminShared;

[TestClass]
public class SettingsMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<CatalogSettingsProfile>();
            cfg.AddProfile<MediaSettingsProfile>();
            cfg.AddProfile<CommonSettingsProfile>();
            cfg.AddProfile<CaptchaSettingsProfile>();
            cfg.AddProfile<SeoSettingsProfile>();
            cfg.AddProfile<StoreInformationSettingsProfile>();
            cfg.AddProfile<PdfSettingsProfile>();
            cfg.AddProfile<MenuItemSettingsProfile>();
            cfg.AddProfile<AdminSearchSettingsProfile>();
            cfg.AddProfile<ShoppingCartSettingsProfile>();
            cfg.AddProfile<LoyaltyPointsSettingsProfile>();
            cfg.AddProfile<OrderSettingsProfile>();
            cfg.AddProfile<TaxSettingsProfile>();
            cfg.AddProfile<AddressProfile>();
            cfg.AddProfile<PushNotificationsSettingsProfile>();
            cfg.AddProfile<CustomerSettingsProfile>();
            cfg.AddProfile<AddressSettingsProfile>();
            cfg.AddProfile<BlogSettingsProfile>();
            cfg.AddProfile<NewsSettingsProfile>();
            cfg.AddProfile<KnowledgebaseSettingsProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── CatalogSettings ───────────────────────────────────────────────────────

    [TestMethod]
    public Task CatalogSettings_ToModel()
    {
        var source = new CatalogSettings {
            AllowViewUnpublishedProductPage = false,
            DisplayDiscontinuedMessageForUnpublishedProducts = true,
            ShowSkuOnProductDetailsPage = true,
            ShowSkuOnCatalogPages = false,
            ShowGtin = false,
            ShowFreeShippingNotification = true,
            ShowProductImagesInSearchAutoComplete = false,
            PageShareCode = "",
            DefaultPageSizeOptions = "6, 3, 9",
            SearchPageProductsPerPage = 6,
            SearchPageAllowCustomersToSelectPageSize = true
        };
        return Verify(_mapper.Map<CatalogSettingsModel>(source));
    }

    [TestMethod]
    public Task CatalogSettingsModel_ToDomain()
    {
        var model = new CatalogSettingsModel {
            AllowViewUnpublishedProductPage = false,
            ShowSkuOnProductDetailsPage = true,
            SearchPageProductsPerPage = 6
        };
        return Verify(_mapper.Map<CatalogSettings>(model));
    }

    // ── MediaSettings ─────────────────────────────────────────────────────────

    [TestMethod]
    public Task MediaSettings_ToModel()
    {
        var source = new MediaSettings {
            ProductThumbPictureSize = 415,
            ProductDetailsPictureSize = 550,
            ProductThumbPictureSizeOnProductDetailsPage = 100,
            AssociatedProductPictureSize = 220,
            CategoryThumbPictureSize = 450,
            CollectionThumbPictureSize = 450,
            BrandThumbPictureSize = 450,
            VendorThumbPictureSize = 450,
            CartThumbPictureSize = 80,
            MaximumImageSize = 1980,
            AutoCompleteSearchThumbPictureSize = 20
        };
        return Verify(_mapper.Map<MediaSettingsModel>(source));
    }

    [TestMethod]
    public Task MediaSettingsModel_ToDomain()
    {
        var model = new MediaSettingsModel {
            ProductThumbPictureSize = 415,
            MaximumImageSize = 1980
        };
        return Verify(_mapper.Map<MediaSettings>(model));
    }

    // ── CommonSettings ────────────────────────────────────────────────────────

    [TestMethod]
    public Task CommonSettings_ToModel()
    {
        var source = new CommonSettings {
            StoreInDatabaseContactUsForm = true,
            UseSystemEmailForContactUsForm = false,
            SitemapEnabled = true,
            SitemapIncludeCategories = true,
            SitemapIncludeProducts = false,
            PopupForTermsOfServiceLinks = true
        };
        return Verify(_mapper.Map<GeneralCommonSettingsModel.CommonSettingsModel>(source));
    }

    [TestMethod]
    public Task CommonSettingsModel_ToDomain()
    {
        var model = new GeneralCommonSettingsModel.CommonSettingsModel {
            StoreInDatabaseContactUsForm = true,
            SitemapEnabled = true
        };
        return Verify(_mapper.Map<CommonSettings>(model));
    }

    // ── CaptchaSettings ───────────────────────────────────────────────────────

    [TestMethod]
    public Task CaptchaSettings_ToModel()
    {
        var source = new CaptchaSettings {
            Enabled = true,
            ShowOnLoginPage = true,
            ShowOnRegistrationPage = false,
            ShowOnContactUsPage = true,
            ShowOnProductReviewPage = false,
            ReCaptchaPublicKey = "public-key-123",
            ReCaptchaPrivateKey = "private-key-456",
            ReCaptchaVersion = GoogleReCaptchaVersion.V2
        };
        return Verify(_mapper.Map<GeneralCommonSettingsModel.SecuritySettingsModel>(source));
    }

    [TestMethod]
    public Task SecuritySettingsModel_ToDomain()
    {
        var model = new GeneralCommonSettingsModel.SecuritySettingsModel {
            CaptchaEnabled = true,
            CaptchaShowOnLoginPage = true,
            ReCaptchaPublicKey = "public-key-123",
            ReCaptchaPrivateKey = "private-key-456"
        };
        return Verify(_mapper.Map<CaptchaSettings>(model));
    }

    // ── SeoSettings ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task SeoSettings_ToModel()
    {
        var source = new SeoSettings {
            PageTitleSeparator = " | ",
            PageTitleSeoAdjustment = true,
            DefaultTitle = "Grand Store",
            DefaultMetaKeywords = "store, online",
            DefaultMetaDescription = "Best online store",
            GenerateProductMetaDescription = true,
            ConvertNonWesternChars = false,
            AllowUnicodeCharsInUrls = true,
            CanonicalUrlsEnabled = false
        };
        return Verify(_mapper.Map<GeneralCommonSettingsModel.SeoSettingsModel>(source));
    }

    [TestMethod]
    public Task SeoSettingsModel_ToDomain()
    {
        var model = new GeneralCommonSettingsModel.SeoSettingsModel {
            PageTitleSeparator = " | ",
            DefaultTitle = "Grand Store",
            AllowUnicodeCharsInUrls = true
        };
        return Verify(_mapper.Map<SeoSettings>(model));
    }

    // ── AdminSearchSettings ───────────────────────────────────────────────────

    [TestMethod]
    public Task AdminSearchSettings_ToModel()
    {
        var source = new AdminSearchSettings {
            SearchInProducts = true,
            SearchInCategories = true,
            SearchInBlogs = true,
            SearchInOrders = true,
            SearchInCustomers = true,
            SearchInNews = false,
            SearchInPages = true,
            MinSearchTermLength = 3,
            MaxSearchResultsCount = 10
        };
        return Verify(_mapper.Map<AdminSearchSettingsModel>(source));
    }

    [TestMethod]
    public Task AdminSearchSettingsModel_ToDomain()
    {
        var model = new AdminSearchSettingsModel {
            SearchInProducts = true,
            SearchInCategories = true,
            MinSearchTermLength = 3,
            MaxSearchResultsCount = 10
        };
        return Verify(_mapper.Map<AdminSearchSettings>(model));
    }

    // ── ShoppingCartSettings ──────────────────────────────────────────────────

    [TestMethod]
    public Task ShoppingCartSettings_ToModel()
    {
        var source = new ShoppingCartSettings {
            DisplayCartAfterAddingProduct = true,
            DisplayWishlistAfterAddingProduct = false,
            MaximumShoppingCartItems = 1000,
            MaximumWishlistItems = 1000,
            AllowOutOfStockItemsToBeAddedToWishlist = true,
            MoveItemsFromWishlistToCart = true,
            ShowProductImagesOnShoppingCart = true,
            ShowProductImagesOnWishList = true,
            ShowDiscountBox = true,
            ShowGiftVoucherBox = true,
            CrossSellsNumber = 4,
            EmailWishlistEnabled = true,
            AllowAnonymousUsersToEmailWishlist = false
        };
        return Verify(_mapper.Map<SalesSettingsModel.ShoppingCartSettingsModel>(source));
    }

    [TestMethod]
    public Task ShoppingCartSettingsModel_ToDomain()
    {
        var model = new SalesSettingsModel.ShoppingCartSettingsModel {
            DisplayCartAfterAddingProduct = true,
            MaximumShoppingCartItems = 1000,
            ShowDiscountBox = true
        };
        return Verify(_mapper.Map<ShoppingCartSettings>(model));
    }

    // ── LoyaltyPointsSettings ─────────────────────────────────────────────────

    [TestMethod]
    public Task LoyaltyPointsSettings_ToModel()
    {
        var source = new LoyaltyPointsSettings {
            Enabled = true,
            ExchangeRate = 0.01,
            MinimumLoyaltyPointsToUse = 0,
            PointsForRegistration = 100,
            PointsForPurchases_Amount = 10,
            PointsForPurchases_Points = 1
        };
        return Verify(_mapper.Map<SalesSettingsModel.LoyaltyPointsSettingsModel>(source));
    }

    [TestMethod]
    public Task LoyaltyPointsSettingsModel_ToDomain()
    {
        var model = new SalesSettingsModel.LoyaltyPointsSettingsModel {
            Enabled = true,
            ExchangeRate = 0.01,
            PointsForRegistration = 100
        };
        return Verify(_mapper.Map<LoyaltyPointsSettings>(model));
    }

    // ── OrderSettings ─────────────────────────────────────────────────────────

    [TestMethod]
    public Task OrderSettings_ToModel()
    {
        var source = new OrderSettings {
            IsReOrderAllowed = true,
            MinOrderSubtotalAmount = 0,
            MinOrderSubtotalAmountIncludingTax = false,
            MinOrderTotalAmount = 0,
            AnonymousCheckoutAllowed = true,
            TermsOfServiceOnShoppingCartPage = true,
            TermsOfServiceOnOrderConfirmPage = false,
            GiftVouchers_Activated_OrderStatusId = 30
        };
        return Verify(_mapper.Map<SalesSettingsModel.OrderSettingsModel>(source));
    }

    [TestMethod]
    public Task OrderSettingsModel_ToDomain()
    {
        var model = new SalesSettingsModel.OrderSettingsModel {
            IsReOrderAllowed = true,
            AnonymousCheckoutAllowed = true
        };
        return Verify(_mapper.Map<OrderSettings>(model));
    }

    // ── TaxSettings ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task TaxSettings_ToModel()
    {
        var source = new TaxSettings {
            PricesIncludeTax = false,
            AllowCustomersToSelectTaxDisplayType = true,
            TaxDisplayType = TaxDisplayType.ExcludingTax,
            DisplayTaxRates = false,
            HideZeroTax = false,
            HideTaxInOrderSummary = false,
            ForceTaxExclusionFromOrderSubtotal = false,
            ShippingIsTaxable = false,
            EuVatEnabled = false,
            EuVatShopCountryId = "",
            EuVatAllowVatExemption = true,
            EuVatUseWebService = false
        };
        return Verify(_mapper.Map<TaxSettingsModel>(source));
    }

    [TestMethod]
    public Task TaxSettingsModel_ToDomain()
    {
        var model = new TaxSettingsModel {
            PricesIncludeTax = false,
            TaxDisplayType = 1,
            EuVatEnabled = false
        };
        return Verify(_mapper.Map<TaxSettings>(model));
    }

    // ── PushNotificationsSettings ─────────────────────────────────────────────

    [TestMethod]
    public Task PushNotificationsSettings_ToModel()
    {
        var source = new PushNotificationsSettings {
            Enabled = false,
            AllowGuestNotifications = true,
            AuthDomain = "",
            DatabaseUrl = "",
            ProjectId = "",
            PublicApiKey = "",
            SenderId = "",
            StorageBucket = "",
            AppId = ""
        };
        return Verify(_mapper.Map<PushNotificationsSettingsModel>(source));
    }

    [TestMethod]
    public Task PushNotificationsSettingsModel_ToDomain()
    {
        var model = new PushNotificationsSettingsModel {
            Enabled = false,
            AllowGuestNotifications = true
        };
        return Verify(_mapper.Map<PushNotificationsSettings>(model));
    }

    // ── CustomerSettings ──────────────────────────────────────────────────────

    [TestMethod]
    public Task CustomerSettings_ToModel()
    {
        var source = new CustomerSettings {
            UsernamesEnabled = false,
            CheckUsernameAvailabilityEnabled = false,
            AllowUsersToChangeUsernames = true,
            NotifyNewCustomerRegistration = true,
            NewsletterEnabled = true,
            GeoEnabled = false
        };
        return Verify(_mapper.Map<CustomerSettingsModel.CustomersSettingsModel>(source));
    }

    [TestMethod]
    public Task CustomerSettingsModel_ToDomain()
    {
        var model = new CustomerSettingsModel.CustomersSettingsModel {
            UsernamesEnabled = false,
            DefaultPasswordFormat = 1
        };
        return Verify(_mapper.Map<CustomerSettings>(model));
    }

    // ── AddressSettings ───────────────────────────────────────────────────────

    [TestMethod]
    public Task AddressSettings_ToModel()
    {
        var source = new AddressSettings {
            NameEnabled = true,
            CompanyEnabled = true,
            CompanyRequired = false,
            CountryEnabled = true,
            StateProvinceEnabled = true,
            CityEnabled = true,
            CityRequired = false,
            StreetAddressEnabled = true,
            StreetAddressRequired = true,
            StreetAddress2Enabled = false,
            ZipPostalCodeEnabled = true,
            ZipPostalCodeRequired = true,
            PhoneEnabled = true,
            PhoneRequired = false,
            FaxEnabled = false,
            FaxRequired = false
        };
        return Verify(_mapper.Map<CustomerSettingsModel.AddressSettingsModel>(source));
    }

    [TestMethod]
    public Task AddressSettingsModel_ToDomain()
    {
        var model = new CustomerSettingsModel.AddressSettingsModel {
            NameEnabled = true,
            CompanyEnabled = true,
            CountryEnabled = true,
            StreetAddressEnabled = true
        };
        return Verify(_mapper.Map<AddressSettings>(model));
    }
}
