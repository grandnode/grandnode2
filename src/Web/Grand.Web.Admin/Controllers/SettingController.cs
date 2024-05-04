using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Admin;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Documents;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.News;
using Grand.Domain.Orders;
using Grand.Domain.PushNotifications;
using Grand.Domain.Security;
using Grand.Domain.Seo;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Extensions.Mapping.Settings;
using Grand.Web.Admin.Models.Settings;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Themes;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OperatingSystem = Grand.Infrastructure.OperatingSystem;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Settings)]
public class SettingController(
    ISettingService settingService,
    ICurrencyService currencyService,
    IPictureService pictureService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    IWorkContext workContext,
    IMediator mediator,
    IMerchandiseReturnService merchandiseReturnService,
    ILanguageService languageService,
    IOrderStatusService orderStatusService,
    ICacheBase cacheBase)
    : BaseAdminController
{
    #region Fields

    #endregion

    #region Constructors

    #endregion

    #region Utilities

    protected async Task ClearCache()
    {
        await cacheBase.Clear();
    }

    public async Task<IActionResult> Content()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var blogSettings = settingService.LoadSetting<BlogSettings>(storeScope);
        var newsSettings = settingService.LoadSetting<NewsSettings>(storeScope);
        var knowledgebaseSettings = settingService.LoadSetting<KnowledgebaseSettings>(storeScope);
        var model = new ContentSettingsModel {
            BlogSettings = blogSettings.ToModel(),
            NewsSettings = newsSettings.ToModel(),
            KnowledgebaseSettings = knowledgebaseSettings.ToModel(),
            ActiveStore = storeScope
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Content(ContentSettingsModel model)
    {
        var storeScope = await GetActiveStore();
        //blog
        var blogSettings = settingService.LoadSetting<BlogSettings>(storeScope);
        blogSettings = model.BlogSettings.ToEntity(blogSettings);
        await settingService.SaveSetting(blogSettings, storeScope);

        //news
        var newsSettings = settingService.LoadSetting<NewsSettings>(storeScope);
        newsSettings = model.NewsSettings.ToEntity(newsSettings);
        await settingService.SaveSetting(newsSettings, storeScope);

        //knowledgebase
        var knowledgeBaseSettings = settingService.LoadSetting<KnowledgebaseSettings>(storeScope);
        knowledgeBaseSettings = model.KnowledgebaseSettings.ToEntity(knowledgeBaseSettings);
        await settingService.SaveSetting(knowledgeBaseSettings, storeScope);

        //selected tab
        await SaveSelectedTabIndex();

        //now clear cache
        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Content");
    }

    public async Task<IActionResult> Vendor()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var vendorSettings = settingService.LoadSetting<VendorSettings>(storeScope);
        var model = vendorSettings.ToModel();

        model.ActiveStore = storeScope;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Vendor(VendorSettingsModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var vendorSettings = settingService.LoadSetting<VendorSettings>(storeScope);
        vendorSettings = model.ToEntity(vendorSettings);

        await settingService.SaveSetting(vendorSettings, storeScope);

        //now clear cache
        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Vendor");
    }

    public async Task<IActionResult> Catalog()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var catalogSettings = settingService.LoadSetting<CatalogSettings>(storeScope);
        var model = catalogSettings.ToModel();
        model.ActiveStore = storeScope;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Catalog(CatalogSettingsModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var catalogSettings = settingService.LoadSetting<CatalogSettings>(storeScope);
        catalogSettings = model.ToEntity(catalogSettings);

        await settingService.SaveSetting(catalogSettings, storeScope);

        //now clear cache
        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));

        //selected tab
        await SaveSelectedTabIndex();

        return RedirectToAction("Catalog");
    }

    #region Sort options

    [HttpPost]
    public async Task<IActionResult> SortOptionsList(DataSourceRequest command)
    {
        var storeScope = await GetActiveStore();
        var catalogSettings = settingService.LoadSetting<CatalogSettings>(storeScope);
        var model = new List<SortOptionModel>();
        foreach (int option in Enum.GetValues(typeof(ProductSortingEnum)))
            model.Add(new SortOptionModel {
                Id = option,
                Name = ((ProductSortingEnum)option).GetTranslationEnum(translationService, workContext),
                IsActive = !catalogSettings.ProductSortingEnumDisabled.Contains(option),
                DisplayOrder = catalogSettings.ProductSortingEnumDisplayOrder.TryGetValue(option, out var value)
                    ? value
                    : option
            });
        var gridModel = new DataSourceResult {
            Data = model.OrderBy(option => option.DisplayOrder),
            Total = model.Count
        };
        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> SortOptionUpdate(SortOptionModel model)
    {
        var storeScope = await GetActiveStore();
        var catalogSettings = settingService.LoadSetting<CatalogSettings>(storeScope);

        catalogSettings.ProductSortingEnumDisplayOrder[model.Id] = model.DisplayOrder;
        switch (model.IsActive)
        {
            case true when catalogSettings.ProductSortingEnumDisabled.Contains(model.Id):
                catalogSettings.ProductSortingEnumDisabled.Remove(model.Id);
                break;
            case false when !catalogSettings.ProductSortingEnumDisabled.Contains(model.Id):
                catalogSettings.ProductSortingEnumDisabled.Add(model.Id);
                break;
        }

        await settingService.SaveSetting(catalogSettings, storeScope);

        await ClearCache();

        return new JsonResult("");
    }

    #endregion

    public async Task<IActionResult> Sales()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var loyaltyPointsSettings = settingService.LoadSetting<LoyaltyPointsSettings>(storeScope);
        var orderSettings = settingService.LoadSetting<OrderSettings>(storeScope);
        var shoppingCartSettings = settingService.LoadSetting<ShoppingCartSettings>(storeScope);

        var model = new SalesSettingsModel {
            LoyaltyPointsSettings = loyaltyPointsSettings.ToModel(),
            OrderSettings = orderSettings.ToModel(),
            ShoppingCartSettings = shoppingCartSettings.ToModel(),
            ActiveStore = storeScope
        };

        var currencySettings = settingService.LoadSetting<CurrencySettings>();
        var currency = await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId);

        //loyal
        model.LoyaltyPointsSettings.PrimaryStoreCurrencyCode = currency?.CurrencyCode;
        //order statuses
        var status = await orderStatusService.GetAll();
        model.LoyaltyPointsSettings.PointsForPurchases_Awarded_OrderStatuses = status
            .Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList();

        //orders
        model.OrderSettings.PrimaryStoreCurrencyCode = currency?.CurrencyCode;

        //gift voucher activation
        model.OrderSettings.GiftVouchers_Activated_OrderStatuses = status
            .Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList();
        model.OrderSettings.GiftVouchers_Activated_OrderStatuses.Insert(0,
            new SelectListItem { Text = "---", Value = "0" });

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Sales(SalesSettingsModel model)
    {
        var storeScope = await GetActiveStore();

        if (ModelState.IsValid)
        {
            var loyaltyPointsSettings = settingService.LoadSetting<LoyaltyPointsSettings>(storeScope);
            loyaltyPointsSettings = model.LoyaltyPointsSettings.ToEntity(loyaltyPointsSettings);
            await settingService.SaveSetting(loyaltyPointsSettings, storeScope);

            var shoppingCartSettings = settingService.LoadSetting<ShoppingCartSettings>(storeScope);
            shoppingCartSettings = model.ShoppingCartSettings.ToEntity(shoppingCartSettings);
            await settingService.SaveSetting(shoppingCartSettings, storeScope);

            var orderSettings = settingService.LoadSetting<OrderSettings>(storeScope);
            orderSettings = model.OrderSettings.ToEntity(orderSettings);

            await settingService.SaveSetting(orderSettings, storeScope);

            //now clear cache
            await ClearCache();
        }
        else
        {
            //If we got this far, something failed, redisplay form
            foreach (var modelState in ModelState.Values)
            foreach (var error in modelState.Errors)
                Error(error.ErrorMessage);
        }

        //selected tab
        await SaveSelectedTabIndex();

        //now clear cache
        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Sales");
    }

    #region Merchandise return reasons

    public async Task<IActionResult> MerchandiseReturnReasonList()
    {
        //select second tab
        const int customerFormFieldIndex = 1;
        await SaveSelectedTabIndex(customerFormFieldIndex);
        return RedirectToAction("Sales", "Setting");
    }

    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnReasonList(DataSourceRequest command)
    {
        var reasons = await merchandiseReturnService.GetAllMerchandiseReturnReasons();
        var gridModel = new DataSourceResult {
            Data = reasons.Select(x => x.ToModel()),
            Total = reasons.Count
        };
        return Json(gridModel);
    }

    //create
    public async Task<IActionResult> MerchandiseReturnReasonCreate()
    {
        var model = new MerchandiseReturnReasonModel();
        //locales
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> MerchandiseReturnReasonCreate(MerchandiseReturnReasonModel model,
        bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var rrr = model.ToEntity();
            await merchandiseReturnService.InsertMerchandiseReturnReason(rrr);

            Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Added"));
            return continueEditing
                ? RedirectToAction("MerchandiseReturnReasonEdit", new { id = rrr.Id })
                : RedirectToAction("MerchandiseReturnReasonList");
        }

        //If we got this far, something failed, redisplay form
        return View(model);
    }

    //edit
    public async Task<IActionResult> MerchandiseReturnReasonEdit(string id)
    {
        var rrr = await merchandiseReturnService.GetMerchandiseReturnReasonById(id);
        if (rrr == null)
            //No reason found with the specified id
            return RedirectToAction("MerchandiseReturnReasonList");

        var model = rrr.ToModel();
        //locales
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = rrr.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> MerchandiseReturnReasonEdit(MerchandiseReturnReasonModel model,
        bool continueEditing)
    {
        var rrr = await merchandiseReturnService.GetMerchandiseReturnReasonById(model.Id);
        if (rrr == null)
            //No reason found with the specified id
            return RedirectToAction("MerchandiseReturnReasonList");

        if (ModelState.IsValid)
        {
            rrr = model.ToEntity(rrr);
            await merchandiseReturnService.UpdateMerchandiseReturnReason(rrr);

            Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Updated"));
            if (continueEditing)
            {
                //selected tab
                await SaveSelectedTabIndex();

                return RedirectToAction("MerchandiseReturnReasonEdit", new { id = rrr.Id });
            }

            return RedirectToAction("MerchandiseReturnReasonList");
        }

        //If we got this far, something failed, redisplay form
        return View(model);
    }

    //delete
    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnReasonDelete(string id)
    {
        var rrr = await merchandiseReturnService.GetMerchandiseReturnReasonById(id);
        await merchandiseReturnService.DeleteMerchandiseReturnReason(rrr);

        Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Deleted"));
        return RedirectToAction("MerchandiseReturnReasonList");
    }

    #endregion

    #region Merchandise return actions

    public async Task<IActionResult> MerchandiseReturnActionList()
    {
        //select second tab
        const int customerFormFieldIndex = 1;
        await SaveSelectedTabIndex(customerFormFieldIndex);
        return RedirectToAction("Sales", "Setting");
    }

    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnActionList(DataSourceRequest command)
    {
        var actions = await merchandiseReturnService.GetAllMerchandiseReturnActions();
        var gridModel = new DataSourceResult {
            Data = actions.Select(x => x.ToModel()),
            Total = actions.Count
        };
        return Json(gridModel);
    }

    //create
    public async Task<IActionResult> MerchandiseReturnActionCreate()
    {
        var model = new MerchandiseReturnActionModel();
        //locales
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> MerchandiseReturnActionCreate(MerchandiseReturnActionModel model,
        bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var rra = model.ToEntity();
            await merchandiseReturnService.InsertMerchandiseReturnAction(rra);

            //now clear cache
            await ClearCache();

            Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Added"));
            return continueEditing
                ? RedirectToAction("MerchandiseReturnActionEdit", new { id = rra.Id })
                : RedirectToAction("MerchandiseReturnActionList");
        }

        //If we got this far, something failed, redisplay form
        return View(model);
    }

    //edit
    public async Task<IActionResult> MerchandiseReturnActionEdit(string id)
    {
        var rra = await merchandiseReturnService.GetMerchandiseReturnActionById(id);
        if (rra == null)
            //No action found with the specified id
            return RedirectToAction("MerchandiseReturnActionList");

        var model = rra.ToModel();
        //locales
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = rra.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> MerchandiseReturnActionEdit(MerchandiseReturnActionModel model,
        bool continueEditing)
    {
        var rra = await merchandiseReturnService.GetMerchandiseReturnActionById(model.Id);
        if (rra == null)
            //No action found with the specified id
            return RedirectToAction("MerchandiseReturnActionList");

        if (ModelState.IsValid)
        {
            rra = model.ToEntity(rra);
            await merchandiseReturnService.UpdateMerchandiseReturnAction(rra);

            Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Updated"));
            if (continueEditing)
            {
                //selected tab
                await SaveSelectedTabIndex();

                return RedirectToAction("MerchandiseReturnActionEdit", new { id = rra.Id });
            }

            return RedirectToAction("MerchandiseReturnActionList");
        }

        //If we got this far, something failed, redisplay form
        return View(model);
    }

    //delete
    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnActionDelete(string id)
    {
        var rra = await merchandiseReturnService.GetMerchandiseReturnActionById(id);
        await merchandiseReturnService.DeleteMerchandiseReturnAction(rra);

        Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Deleted"));
        return RedirectToAction("MerchandiseReturnActionList");
    }

    #endregion

    public async Task<IActionResult> Media()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var mediaSettings = settingService.LoadSetting<MediaSettings>(storeScope);
        var model = mediaSettings.ToModel();
        model.ActiveStore = storeScope;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Media(MediaSettingsModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();

        var mediaSettings = settingService.LoadSetting<MediaSettings>(storeScope);
        mediaSettings = model.ToEntity(mediaSettings);

        await settingService.SaveSetting(mediaSettings, storeScope);

        //now clear cache
        await ClearCache();

        //clear old Thumbs
        await pictureService.ClearThumbs();
        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Media");
    }

    public async Task<IActionResult> Customer()
    {
        var storeScope = await GetActiveStore();
        var customerSettings = settingService.LoadSetting<CustomerSettings>(storeScope);
        var addressSettings = settingService.LoadSetting<AddressSettings>(storeScope);

        //merge settings
        var model = new CustomerSettingsModel {
            CustomerSettings = customerSettings.ToModel(),
            AddressSettings = addressSettings.ToModel()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Customer(CustomerSettingsModel model)
    {
        var storeScope = await GetActiveStore();
        var customerSettings = settingService.LoadSetting<CustomerSettings>(storeScope);
        var addressSettings = settingService.LoadSetting<AddressSettings>(storeScope);

        customerSettings = model.CustomerSettings.ToEntity(customerSettings);
        await settingService.SaveSetting(customerSettings, storeScope);

        addressSettings = model.AddressSettings.ToEntity(addressSettings);
        await settingService.SaveSetting(addressSettings, storeScope);

        //now clear cache
        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));

        //selected tab
        await SaveSelectedTabIndex();

        return RedirectToAction("Customer");
    }

    public async Task<IActionResult> GeneralCommon([FromServices] IEnumerable<IThemeView> themes)
    {
        var model = new GeneralCommonSettingsModel();
        var storeScope = await GetActiveStore();
        model.ActiveStore = storeScope;
        //datettime settings
        var dateTimeSettings = settingService.LoadSetting<DateTimeSettings>(storeScope);
        model.DateTimeSettings.DefaultStoreTimeZoneId = dateTimeSettings.DefaultStoreTimeZoneId;
        var iswindows = OperatingSystem.IsWindows();
        foreach (var timeZone in dateTimeService.GetSystemTimeZones())
        {
            var name = iswindows ? timeZone.DisplayName : $"{timeZone.StandardName} ({timeZone.Id})";
            model.DateTimeSettings.AvailableTimeZones.Add(new SelectListItem {
                Text = name,
                Value = timeZone.Id,
                Selected = timeZone.Id.Equals(dateTimeSettings.DefaultStoreTimeZoneId,
                    StringComparison.OrdinalIgnoreCase)
            });
        }

        //store information
        var storeInformationSettings = settingService.LoadSetting<StoreInformationSettings>(storeScope);
        model.StoreInformationSettings = storeInformationSettings.ToModel();

        model.StoreInformationSettings.AvailableStoreThemes =
            themes.Where(x => x.AreaName == "").Select(x =>
                new GeneralCommonSettingsModel.StoreInformationSettingsModel.ThemeConfigurationModel {
                    ThemeName = x.ThemeName,
                    ThemeTitle = x.ThemeInfo.Title,
                    PreviewImageUrl = x.ThemeInfo.PreviewImageUrl,
                    PreviewText = x.ThemeInfo.PreviewText,
                    SupportRtl = x.ThemeInfo.SupportRtl,
                    Selected = x.ThemeName == storeInformationSettings.DefaultStoreTheme
                }).ToList();

        //common
        var commonSettings = settingService.LoadSetting<CommonSettings>(storeScope);
        model.CommonSettings = commonSettings.ToModel();

        //seo settings
        var seoSettings = settingService.LoadSetting<SeoSettings>(storeScope);
        model.SeoSettings = seoSettings.ToModel();

        //security settings
        var securitySettings = settingService.LoadSetting<SecuritySettings>(storeScope);
        //captcha settings
        var captchaSettings = settingService.LoadSetting<CaptchaSettings>(storeScope);
        model.SecuritySettings = captchaSettings.ToModel();

        if (securitySettings.AdminAreaAllowedIpAddresses != null)
            for (var i = 0; i < securitySettings.AdminAreaAllowedIpAddresses.Count; i++)
            {
                model.SecuritySettings.AdminAreaAllowedIpAddresses += securitySettings.AdminAreaAllowedIpAddresses[i];
                if (i != securitySettings.AdminAreaAllowedIpAddresses.Count - 1)
                    model.SecuritySettings.AdminAreaAllowedIpAddresses += ",";
            }

        model.SecuritySettings.AvailableReCaptchaVersions =
            GoogleReCaptchaVersion.V2.ToSelectList(HttpContext, false).ToList();

        //PDF settings
        var pdfSettings = settingService.LoadSetting<PdfSettings>(storeScope);
        model.PdfSettings = pdfSettings.ToModel();

        //display menu settings
        var displayMenuItemSettings = settingService.LoadSetting<MenuItemSettings>(storeScope);
        model.DisplayMenuSettings = displayMenuItemSettings.ToModel();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GeneralCommon(GeneralCommonSettingsModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();

        //store information settings
        var storeInformationSettings = settingService.LoadSetting<StoreInformationSettings>(storeScope);
        storeInformationSettings = model.StoreInformationSettings.ToEntity(storeInformationSettings);
        await settingService.SaveSetting(storeInformationSettings, storeScope);

        //datetime settings
        var dateTimeSettings = settingService.LoadSetting<DateTimeSettings>(storeScope);
        dateTimeSettings.DefaultStoreTimeZoneId = model.DateTimeSettings.DefaultStoreTimeZoneId;
        await settingService.SaveSetting(dateTimeSettings, storeScope);

        //common settings
        var commonSettings = settingService.LoadSetting<CommonSettings>(storeScope);
        commonSettings = model.CommonSettings.ToEntity(commonSettings);
        await settingService.SaveSetting(commonSettings, storeScope);

        //seo settings
        var seoSettings = settingService.LoadSetting<SeoSettings>(storeScope);
        seoSettings = model.SeoSettings.ToEntity(seoSettings);
        await settingService.SaveSetting(seoSettings, storeScope);

        //security settings
        var securitySettings = settingService.LoadSetting<SecuritySettings>(storeScope);

        securitySettings.AdminAreaAllowedIpAddresses ??= new List<string>();
        securitySettings.AdminAreaAllowedIpAddresses.Clear();
        if (!string.IsNullOrEmpty(model.SecuritySettings.AdminAreaAllowedIpAddresses))
            foreach (var s in model.SecuritySettings.AdminAreaAllowedIpAddresses.Split(new[] { ',' },
                         StringSplitOptions.RemoveEmptyEntries))
                if (!string.IsNullOrWhiteSpace(s))
                    securitySettings.AdminAreaAllowedIpAddresses.Add(s.Trim());

        await settingService.SaveSetting(securitySettings);

        //captcha settings
        var captchaSettings = settingService.LoadSetting<CaptchaSettings>(storeScope);
        captchaSettings = model.SecuritySettings.ToEntity(captchaSettings);
        await settingService.SaveSetting(captchaSettings);
        if (captchaSettings.Enabled &&
            (string.IsNullOrWhiteSpace(captchaSettings.ReCaptchaPublicKey) ||
             string.IsNullOrWhiteSpace(captchaSettings.ReCaptchaPrivateKey)))
            //captcha is enabled but the keys are not entered
            Error("Captcha is enabled but the appropriate keys are not entered");

        //PDF settings
        var pdfSettings = settingService.LoadSetting<PdfSettings>(storeScope);
        pdfSettings = model.PdfSettings.ToEntity(pdfSettings);
        await settingService.SaveSetting(pdfSettings, storeScope);

        //menu item settings
        var displayMenuItemSettings = settingService.LoadSetting<MenuItemSettings>(storeScope);
        displayMenuItemSettings = model.DisplayMenuSettings.ToEntity(displayMenuItemSettings);
        await settingService.SaveSetting(displayMenuItemSettings, storeScope);

        //now clear cache
        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));

        //selected tab
        await SaveSelectedTabIndex();

        return RedirectToAction("GeneralCommon");
    }

    public async Task<IActionResult> PushNotifications()
    {
        var storeScope = await GetActiveStore();
        var settings = settingService.LoadSetting<PushNotificationsSettings>(storeScope);
        var model = settings.ToModel();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> PushNotifications(PushNotificationsSettingsModel model)
    {
        var storeScope = await GetActiveStore();
        var settings = settingService.LoadSetting<PushNotificationsSettings>(storeScope);
        settings = model.ToEntity(settings);

        await settingService.SaveSetting(settings);

        //now clear cache
        await ClearCache();

        //save to file
        SavePushNotificationsToFile(model);

        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return await PushNotifications();
    }

    private void SavePushNotificationsToFile(PushNotificationsSettingsModel model)
    {
        //edit js file needed by firebase
        var filename = "firebase-messaging-sw.js";
        var oryginalFilePath = CommonPath.WebHostMapPath(filename);
        var savedFilePath = CommonPath.WebMapPath(filename);
        if (System.IO.File.Exists(oryginalFilePath))
        {
            var lines = System.IO.File.ReadAllLines(oryginalFilePath);
            var i = 0;
            foreach (var line in lines)
            {
                if (line.Contains("apiKey")) lines[i] = "apiKey: \"" + model.PushApiKey + "\",";
                if (line.Contains("authDomain")) lines[i] = "authDomain: \"" + model.AuthDomain + "\",";
                if (line.Contains("databaseURL")) lines[i] = "databaseURL: \"" + model.DatabaseUrl + "\",";
                if (line.Contains("projectId")) lines[i] = "projectId: \"" + model.ProjectId + "\",";
                if (line.Contains("storageBucket")) lines[i] = "storageBucket: \"" + model.StorageBucket + "\",";
                if (line.Contains("messagingSenderId")) lines[i] = "messagingSenderId: \"" + model.SenderId + "\",";
                if (line.Contains("appId")) lines[i] = "appId: \"" + model.AppId + "\",";
                i++;
            }

            System.IO.File.WriteAllLines(savedFilePath, lines);
        }
        else
        {
            throw new ArgumentNullException($"{oryginalFilePath} not exist");
        }
    }

    public IActionResult AdminSearch()
    {
        var settings = settingService.LoadSetting<AdminSearchSettings>();
        var model = settings.ToModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AdminSearch(AdminSearchSettingsModel model)
    {
        var settings = settingService.LoadSetting<AdminSearchSettings>();
        settings = model.ToEntity(settings);
        await settingService.SaveSetting(settings);

        //now clear cache
        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return AdminSearch();
    }

    #region System settings

    public async Task<IActionResult> SystemSetting()
    {
        var settings = settingService.LoadSetting<SystemSettings>();

        var model = new SystemSettingsModel {
            //order ident
            OrderIdent = await mediator.Send(new MaxOrderNumberCommand()),
            //system settings
            DaysToCancelUnpaidOrder = settings.DaysToCancelUnpaidOrder,
            DeleteGuestTaskOlderThanMinutes = settings.DeleteGuestTaskOlderThanMinutes
        };

        //storage settings
        var storagesettings = settingService.LoadSetting<StorageSettings>();
        model.PicturesStoredIntoDatabase = storagesettings.PictureStoreInDb;

        //area admin settings
        var adminsettings = settingService.LoadSetting<AdminAreaSettings>();
        model.DefaultGridPageSize = adminsettings.DefaultGridPageSize;
        model.GridPageSizes = adminsettings.GridPageSizes;
        model.UseIsoDateTimeConverterInJson = adminsettings.UseIsoDateTimeConverterInJson;
        model.HideStoreColumn = adminsettings.HideStoreColumn;

        //language settings 
        var langsettings = settingService.LoadSetting<LanguageSettings>();
        model.IgnoreRtlPropertyForAdminArea = langsettings.IgnoreRtlPropertyForAdminArea;
        model.AutomaticallyDetectLanguage = langsettings.AutomaticallyDetectLanguage;
        model.DefaultAdminLanguageId = langsettings.DefaultAdminLanguageId;

        //others
        var docsettings = settingService.LoadSetting<DocumentSettings>();
        model.DocumentPageSizeSettings = docsettings.PageSize;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SystemSetting(SystemSettingsModel model)
    {
        //system 
        var settings = settingService.LoadSetting<SystemSettings>();
        settings.DaysToCancelUnpaidOrder = model.DaysToCancelUnpaidOrder;
        settings.DeleteGuestTaskOlderThanMinutes = model.DeleteGuestTaskOlderThanMinutes;
        await settingService.SaveSetting(settings);

        //order ident
        if (model.OrderIdent is > 0) await mediator.Send(new MaxOrderNumberCommand { OrderNumber = model.OrderIdent });

        //admin area
        var adminAreaSettings = settingService.LoadSetting<AdminAreaSettings>();
        adminAreaSettings.DefaultGridPageSize = model.DefaultGridPageSize;
        adminAreaSettings.GridPageSizes = model.GridPageSizes;
        adminAreaSettings.UseIsoDateTimeConverterInJson = model.UseIsoDateTimeConverterInJson;
        adminAreaSettings.HideStoreColumn = model.HideStoreColumn;
        await settingService.SaveSetting(adminAreaSettings);

        //language settings 
        var langsettings = settingService.LoadSetting<LanguageSettings>();
        langsettings.IgnoreRtlPropertyForAdminArea = model.IgnoreRtlPropertyForAdminArea;
        langsettings.AutomaticallyDetectLanguage = model.AutomaticallyDetectLanguage;
        langsettings.DefaultAdminLanguageId = model.DefaultAdminLanguageId;
        await settingService.SaveSetting(langsettings);

        //doc settings 
        var docsettings = settingService.LoadSetting<DocumentSettings>();
        docsettings.PageSize = model.DocumentPageSizeSettings;
        await settingService.SaveSetting(docsettings);

        //now clear cache
        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));

        return await SystemSetting();
    }

    [HttpPost]
    public async Task<IActionResult> ChangePictureStorage()
    {
        var storageSettings = settingService.LoadSetting<StorageSettings>();
        var storeIdDb = !storageSettings.PictureStoreInDb;
        storageSettings.PictureStoreInDb = storeIdDb;

        //save the new setting value
        await settingService.SaveSetting(storageSettings);

        //save picture
        await SavePictureStorage(storeIdDb);

        //now clear cache
        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("SystemSetting");
    }

    private async Task SavePictureStorage(bool storeIdDb)
    {
        var pageIndex = 0;
        const int pageSize = 100;
        while (true)
        {
            var pictures = pictureService.GetPictures(pageIndex, pageSize);
            pageIndex++;
            if (!pictures.Any())
                break;

            foreach (var picture in pictures)
            {
                var pictureBinary = await pictureService.LoadPictureBinary(picture, !storeIdDb);
                if (storeIdDb)
                {
                    await pictureService.DeletePictureOnFileSystem(picture);
                }
                else
                {
                    //now on file system
                    if (pictureBinary != null)
                        await pictureService.SavePictureInFile(picture.Id, pictureBinary, picture.MimeType);
                }

                picture.PictureBinary = storeIdDb ? pictureBinary : Array.Empty<byte>();
                picture.IsNew = true;

                await pictureService.UpdatePicture(picture);
            }
        }
    }

    #endregion

    #endregion
}