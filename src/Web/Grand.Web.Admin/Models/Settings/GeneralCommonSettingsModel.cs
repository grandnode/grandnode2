using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Security.Captcha;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Settings;

public class GeneralCommonSettingsModel : BaseModel
{
    public string ActiveStore { get; set; }
    public StoreInformationSettingsModel StoreInformationSettings { get; set; } = new();
    public CommonSettingsModel CommonSettings { get; set; } = new();
    public DateTimeSettingsModel DateTimeSettings { get; set; } = new();
    public SeoSettingsModel SeoSettings { get; set; } = new();
    public SecuritySettingsModel SecuritySettings { get; set; } = new();
    public PdfSettingsModel PdfSettings { get; set; } = new();
    public DisplayMenuSettingsModel DisplayMenuSettings { get; set; } = new();


    #region Nested classes

    public class StoreInformationSettingsModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.StoreClosed")]
        public bool StoreClosed { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DefaultStoreTheme")]

        public string DefaultStoreTheme { get; set; }

        public IList<ThemeConfigurationModel> AvailableStoreThemes { get; set; } = new List<ThemeConfigurationModel>();

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.AllowCustomerToSelectTheme")]
        public bool AllowCustomerToSelectTheme { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.AllowToSelectAdminTheme")]
        public bool AllowToSelectAdminTheme { get; set; }

        [UIHint("Picture")]
        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.Logo")]
        public string LogoPictureId { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DisplayCookieInformation")]
        public bool DisplayCookieInformation { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DisplayPrivacyPreference")]
        public bool DisplayPrivacyPreference { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.FacebookLink")]
        public string FacebookLink { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.TwitterLink")]
        public string TwitterLink { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.YoutubeLink")]
        public string YoutubeLink { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.InstagramLink")]
        public string InstagramLink { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.LinkedInLink")]
        public string LinkedInLink { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.PinterestLink")]
        public string PinterestLink { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.VoiceNavigation")]
        public bool VoiceNavigation { get; set; }

        #region Nested classes

        public class ThemeConfigurationModel
        {
            public string ThemeName { get; set; }
            public string ThemeTitle { get; set; }
            public string PreviewImageUrl { get; set; }
            public string PreviewText { get; set; }
            public bool SupportRtl { get; set; }
            public bool Selected { get; set; }
        }

        #endregion
    }

    public class CommonSettingsModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.StoreInDatabaseContactUsForm")]
        public bool StoreInDatabaseContactUsForm { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.SubjectFieldOnContactUsForm")]
        public bool SubjectFieldOnContactUsForm { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.UseSystemEmailForContactUsForm")]
        public bool UseSystemEmailForContactUsForm { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.AllowToReadLetsEncryptFile")]
        public bool AllowToReadLetsEncryptFile { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.AllowToSelectStore")]
        public bool AllowToSelectStore { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.PopupForTermsOfServiceLinks")]
        public bool PopupForTermsOfServiceLinks { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.SitemapEnabled")]
        public bool SitemapEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.SitemapIncludeCategories")]
        public bool SitemapIncludeCategories { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.SitemapIncludeImage")]
        public bool SitemapIncludeImage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.SitemapIncludeBrands")]
        public bool SitemapIncludeBrands { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.SitemapIncludeProducts")]
        public bool SitemapIncludeProducts { get; set; }
    }

    public class DateTimeSettingsModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DefaultStoreTimeZone")]
        public string DefaultStoreTimeZoneId { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DefaultStoreTimeZone")]
        public IList<SelectListItem> AvailableTimeZones { get; set; } = new List<SelectListItem>();
    }

    public class SeoSettingsModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.PageTitleSeparator")]

        public string PageTitleSeparator { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.PageTitleSeoAdjustment")]
        public bool PageTitleSeoAdjustment { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DefaultTitle")]

        public string DefaultTitle { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DefaultMetaKeywords")]

        public string DefaultMetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DefaultMetaDescription")]

        public string DefaultMetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.GenerateProductMetaDescription")]
        public bool GenerateProductMetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.ConvertNonWesternChars")]
        public bool ConvertNonWesternChars { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.SeoCharConversion")]
        public string SeoCharConversion { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CanonicalUrlsEnabled")]
        public bool CanonicalUrlsEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.TwitterMetaTags")]
        public bool TwitterMetaTags { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.OpenGraphMetaTags")]
        public bool OpenGraphMetaTags { get; set; }

        [UIHint("Picture")]
        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.StorePicture")]
        public string StorePictureId { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.AllowSlashChar")]
        public bool AllowSlashChar { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.AllowUnicodeCharsInUrls")]
        public bool AllowUnicodeCharsInUrls { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CustomHeadTags")]
        public string CustomHeadTags { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.ReservedEntityUrlSlugs")]
        public List<string> ReservedEntityUrlSlugs { get; set; }
    }

    public class SecuritySettingsModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.AdminAreaAllowedIpAddresses")]
        public string AdminAreaAllowedIpAddresses { get; set; }


        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaEnabled")]
        public bool CaptchaEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnLoginPage")]
        public bool CaptchaShowOnLoginPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnRegistrationPage")]
        public bool CaptchaShowOnRegistrationPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnPasswordRecoveryPage")]
        public bool CaptchaShowOnPasswordRecoveryPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnContactUsPage")]
        public bool CaptchaShowOnContactUsPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnEmailWishlistToFriendPage")]
        public bool CaptchaShowOnEmailWishlistToFriendPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnEmailProductToFriendPage")]
        public bool CaptchaShowOnEmailProductToFriendPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnAskQuestionPage")]
        public bool CaptchaShowOnAskQuestionPage { get; set; }


        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnBlogCommentPage")]
        public bool CaptchaShowOnBlogCommentPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnArticleCommentPage")]
        public bool CaptchaShowOnArticleCommentPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnNewsCommentPage")]
        public bool CaptchaShowOnNewsCommentPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnProductReviewPage")]
        public bool CaptchaShowOnProductReviewPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnApplyVendorPage")]
        public bool CaptchaShowOnApplyVendorPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.CaptchaShowOnVendorReviewPage")]
        public bool CaptchaShowOnVendorReviewPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.reCaptchaPublicKey")]
        public string ReCaptchaPublicKey { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.reCaptchaPrivateKey")]
        public string ReCaptchaPrivateKey { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.ReCaptchaTheme")]
        public string ReCaptchaTheme { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.ReCaptchaScore")]
        public decimal ReCaptchaScore { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.reCaptchaVersion")]
        public GoogleReCaptchaVersion ReCaptchaVersion { get; set; }

        public IList<SelectListItem> AvailableReCaptchaVersions { get; set; } = new List<SelectListItem>();
    }

    public class PdfSettingsModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.PdfLogo")]
        [UIHint("Picture")]
        public string LogoPictureId { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DisablePdfInvoicesForPendingOrders")]
        public bool DisablePdfInvoicesForPendingOrders { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.InvoiceHeaderText")]
        public string InvoiceHeaderText { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.InvoiceFooterText")]
        public string InvoiceFooterText { get; set; }
    }

    public class DisplayMenuSettingsModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DisplayMenuSettings.DisplayHomePageMenu")]
        public bool DisplayHomePageMenu { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DisplayMenuSettings.DisplayNewProductsMenu")]
        public bool DisplayNewProductsMenu { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DisplayMenuSettings.DisplaySearchMenu")]
        public bool DisplaySearchMenu { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DisplayMenuSettings.DisplayCustomerMenu")]
        public bool DisplayCustomerMenu { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DisplayMenuSettings.DisplayBlogMenu")]
        public bool DisplayBlogMenu { get; set; }

        [GrandResourceDisplayName("Admin.Settings.GeneralCommon.DisplayMenuSettings.DisplayContactUsMenu")]
        public bool DisplayContactUsMenu { get; set; }
    }

    #endregion
}