using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.News;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Domain.Security;
using Grand.Domain.Seo;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Extensions.Mapping.Settings;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.Settings)]
public class SettingController(
    ISettingService settingService,
    IPictureService pictureService,
    ITranslationService translationService,
    IMerchandiseReturnService merchandiseReturnService,
    ILanguageService languageService,
    ICacheBase cacheBase,
    IContextAccessor contextAccessor,
    IEnumTranslationService enumTranslationService)
    : BaseStoreController
{
    #region Utilities

    private async Task ClearCache()
    {
        await cacheBase.Clear();
    }

    private string GetStoreScope() => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    /// <summary>
    /// Returns true if the store owner is allowed to access (edit/delete) the given store-linked item.
    /// Store owners can only access items that are explicitly assigned to their store
    /// (LimitedToStores = true and their storeId is in the Stores collection).
    /// Items available to all stores (LimitedToStores = false) are not editable by store owners.
    /// </summary>
    private static bool IsStoreOwnerAccessAllowed(string storeId, bool limitedToStores, ICollection<string> stores)
        => string.IsNullOrEmpty(storeId) || (limitedToStores && stores.Contains(storeId));

    #endregion

    #region General common settings

    public async Task<IActionResult> GeneralCommon([FromServices] IEnumerable<IThemeView> themes,
        [FromServices] IDateTimeService dateTimeService)
    {
        var storeScope = GetStoreScope();
        var model = new GeneralCommonSettingsModel {
            ActiveStore = storeScope
        };

        var dateTimeSettings = await settingService.LoadSetting<DateTimeSettings>(storeScope);
        model.DateTimeSettings.DefaultStoreTimeZoneId = dateTimeSettings.DefaultStoreTimeZoneId;
        var isWindows = Grand.Infrastructure.OperatingSystem.IsWindows();
        foreach (var timeZone in dateTimeService.GetSystemTimeZones())
        {
            var name = isWindows ? timeZone.DisplayName : $"{timeZone.StandardName} ({timeZone.Id})";
            model.DateTimeSettings.AvailableTimeZones.Add(new SelectListItem {
                Text = name,
                Value = timeZone.Id,
                Selected = timeZone.Id.Equals(dateTimeSettings.DefaultStoreTimeZoneId,
                    StringComparison.OrdinalIgnoreCase)
            });
        }

        var storeInformationSettings = await settingService.LoadSetting<StoreInformationSettings>(storeScope);
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

        var commonSettings = await settingService.LoadSetting<CommonSettings>(storeScope);
        model.CommonSettings = commonSettings.ToModel();

        var seoSettings = await settingService.LoadSetting<SeoSettings>(storeScope);
        model.SeoSettings = seoSettings.ToModel();

        var captchaSettings = await settingService.LoadSetting<CaptchaSettings>(storeScope);
        model.SecuritySettings = captchaSettings.ToModel();
        model.SecuritySettings.AvailableReCaptchaVersions =
            enumTranslationService.ToSelectList(GoogleReCaptchaVersion.V2, false).ToList();

        var pdfSettings = await settingService.LoadSetting<PdfSettings>(storeScope);
        model.PdfSettings = pdfSettings.ToModel();

        var displayMenuItemSettings = await settingService.LoadSetting<MenuItemSettings>(storeScope);
        model.DisplayMenuSettings = displayMenuItemSettings.ToModel();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GeneralCommon(GeneralCommonSettingsModel model)
    {
        var storeScope = GetStoreScope();

        var storeInformationSettings = await settingService.LoadSetting<StoreInformationSettings>(storeScope);
        storeInformationSettings = model.StoreInformationSettings.ToEntity(storeInformationSettings);
        await settingService.SaveSetting(storeInformationSettings, storeScope);

        var dateTimeSettings = await settingService.LoadSetting<DateTimeSettings>(storeScope);
        dateTimeSettings.DefaultStoreTimeZoneId = model.DateTimeSettings.DefaultStoreTimeZoneId;
        await settingService.SaveSetting(dateTimeSettings, storeScope);

        var commonSettings = await settingService.LoadSetting<CommonSettings>(storeScope);
        commonSettings = model.CommonSettings.ToEntity(commonSettings);
        await settingService.SaveSetting(commonSettings, storeScope);

        var seoSettings = await settingService.LoadSetting<SeoSettings>(storeScope);
        seoSettings = model.SeoSettings.ToEntity(seoSettings);
        await settingService.SaveSetting(seoSettings, storeScope);

        var captchaSettings = await settingService.LoadSetting<CaptchaSettings>(storeScope);
        captchaSettings = model.SecuritySettings.ToEntity(captchaSettings);
        await settingService.SaveSetting(captchaSettings, storeScope);
        if (captchaSettings.Enabled &&
            (string.IsNullOrWhiteSpace(captchaSettings.ReCaptchaPublicKey) ||
             string.IsNullOrWhiteSpace(captchaSettings.ReCaptchaPrivateKey)))
            Error("Captcha is enabled but the appropriate keys are not entered");

        var pdfSettings = await settingService.LoadSetting<PdfSettings>(storeScope);
        pdfSettings = model.PdfSettings.ToEntity(pdfSettings);
        await settingService.SaveSetting(pdfSettings, storeScope);

        var displayMenuItemSettings = await settingService.LoadSetting<MenuItemSettings>(storeScope);
        displayMenuItemSettings = model.DisplayMenuSettings.ToEntity(displayMenuItemSettings);
        await settingService.SaveSetting(displayMenuItemSettings, storeScope);

        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));
        await SaveSelectedTabIndex();
        return RedirectToAction("GeneralCommon");
    }

    #endregion

    #region Catalog settings

    public async Task<IActionResult> Catalog()
    {
        var storeScope = GetStoreScope();
        var catalogSettings = await settingService.LoadSetting<CatalogSettings>(storeScope);
        var model = catalogSettings.ToModel();
        model.ActiveStore = storeScope;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Catalog(CatalogSettingsModel model)
    {
        var storeScope = GetStoreScope();
        var catalogSettings = await settingService.LoadSetting<CatalogSettings>(storeScope);
        catalogSettings = model.ToEntity(catalogSettings);
        await settingService.SaveSetting(catalogSettings, storeScope);
        await ClearCache();
        Success(translationService.GetResource("Admin.Configuration.Updated"));
        await SaveSelectedTabIndex();
        return RedirectToAction("Catalog");
    }

    #region Sort options

    [HttpPost]
    public async Task<IActionResult> SortOptionsList(DataSourceRequest command)
    {
        var storeScope = GetStoreScope();
        var catalogSettings = await settingService.LoadSetting<CatalogSettings>(storeScope);
        var model = new List<SortOptionModel>();
        foreach (int option in Enum.GetValues(typeof(ProductSortingEnum)))
            model.Add(new SortOptionModel {
                Id = option,
                Name = enumTranslationService.GetTranslationEnum((ProductSortingEnum)option),
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
        var storeScope = GetStoreScope();
        var catalogSettings = await settingService.LoadSetting<CatalogSettings>(storeScope);
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

    #endregion

    #region Sales settings

    public async Task<IActionResult> Sales([FromServices] IOrderStatusService orderStatusService,
        [FromServices] ICurrencyService currencyService)
    {
        var storeScope = GetStoreScope();
        var loyaltyPointsSettings = await settingService.LoadSetting<LoyaltyPointsSettings>(storeScope);
        var orderSettings = await settingService.LoadSetting<OrderSettings>(storeScope);
        var shoppingCartSettings = await settingService.LoadSetting<ShoppingCartSettings>(storeScope);

        var model = new SalesSettingsModel {
            LoyaltyPointsSettings = loyaltyPointsSettings.ToModel(),
            OrderSettings = orderSettings.ToModel(),
            ShoppingCartSettings = shoppingCartSettings.ToModel(),
            ActiveStore = storeScope
        };

        var currencySettings = await settingService.LoadSetting<CurrencySettings>();
        var currency = await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId);

        model.LoyaltyPointsSettings.PrimaryStoreCurrencyCode = currency?.CurrencyCode;
        var status = await orderStatusService.GetAll();
        model.LoyaltyPointsSettings.PointsForPurchases_Awarded_OrderStatuses = status
            .Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList();

        model.OrderSettings.PrimaryStoreCurrencyCode = currency?.CurrencyCode;
        model.OrderSettings.GiftVouchers_Activated_OrderStatuses = status
            .Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList();
        model.OrderSettings.GiftVouchers_Activated_OrderStatuses.Insert(0,
            new SelectListItem { Text = "---", Value = "0" });

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Sales(SalesSettingsModel model,
        [FromServices] IOrderStatusService orderStatusService,
        [FromServices] ICurrencyService currencyService)
    {
        var storeScope = GetStoreScope();

        if (ModelState.IsValid)
        {
            var loyaltyPointsSettings = await settingService.LoadSetting<LoyaltyPointsSettings>(storeScope);
            loyaltyPointsSettings = model.LoyaltyPointsSettings.ToEntity(loyaltyPointsSettings);
            await settingService.SaveSetting(loyaltyPointsSettings, storeScope);

            var shoppingCartSettings = await settingService.LoadSetting<ShoppingCartSettings>(storeScope);
            shoppingCartSettings = model.ShoppingCartSettings.ToEntity(shoppingCartSettings);
            await settingService.SaveSetting(shoppingCartSettings, storeScope);

            var orderSettings = await settingService.LoadSetting<OrderSettings>(storeScope);
            orderSettings = model.OrderSettings.ToEntity(orderSettings);
            await settingService.SaveSetting(orderSettings, storeScope);

            await ClearCache();
            Success(translationService.GetResource("Admin.Configuration.Updated"));
            await SaveSelectedTabIndex();
            return RedirectToAction("Sales");
        }
        else
        {
            foreach (var modelState in ModelState.Values)
                foreach (var error in modelState.Errors)
                    Error(error.ErrorMessage);

            var currencySettings = await settingService.LoadSetting<CurrencySettings>();
            var currency = await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId);
            model.LoyaltyPointsSettings.PrimaryStoreCurrencyCode = currency?.CurrencyCode;
            model.OrderSettings.PrimaryStoreCurrencyCode = currency?.CurrencyCode;

            var status = await orderStatusService.GetAll();
            model.LoyaltyPointsSettings.PointsForPurchases_Awarded_OrderStatuses = status
                .Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList();
            model.OrderSettings.GiftVouchers_Activated_OrderStatuses = status
                .Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList();
            model.OrderSettings.GiftVouchers_Activated_OrderStatuses.Insert(0,
                new SelectListItem { Text = "---", Value = "0" });

            model.ActiveStore = storeScope;
            return View(model);
        }
    }

    #region Merchandise return reasons

    public async Task<IActionResult> MerchandiseReturnReasonList()
    {
        const int customerFormFieldIndex = 1;
        await SaveSelectedTabIndex(customerFormFieldIndex);
        return RedirectToAction("Sales", "Setting");
    }

    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnReasonList(DataSourceRequest command)
    {
        var storeId = GetStoreScope();
        var reasons = await merchandiseReturnService.GetAllMerchandiseReturnReasons(storeId);
        var gridModel = new DataSourceResult {
            Data = reasons.Select(x => x.ToModel()),
            Total = reasons.Count
        };
        return Json(gridModel);
    }

    public async Task<IActionResult> MerchandiseReturnReasonCreate()
    {
        var storeId = GetStoreScope();
        var model = new MerchandiseReturnReasonModel {
            Stores = !string.IsNullOrEmpty(storeId) ? [storeId] : []
        };
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> MerchandiseReturnReasonCreate(MerchandiseReturnReasonModel model,
        bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var storeId = GetStoreScope();
            if (!string.IsNullOrEmpty(storeId))
                model.Stores = [storeId];
            var rrr = model.ToEntity();
            await merchandiseReturnService.InsertMerchandiseReturnReason(rrr);
            Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Added"));
            return continueEditing
                ? RedirectToAction("MerchandiseReturnReasonEdit", new { id = rrr.Id })
                : RedirectToAction("MerchandiseReturnReasonList");
        }

        return View(model);
    }

    public async Task<IActionResult> MerchandiseReturnReasonEdit(string id)
    {
        var rrr = await merchandiseReturnService.GetMerchandiseReturnReasonById(id);
        if (rrr == null) return RedirectToAction("MerchandiseReturnReasonList");

        var storeId = GetStoreScope();
        if (!IsStoreOwnerAccessAllowed(storeId, rrr.LimitedToStores, rrr.Stores))
            return RedirectToAction("MerchandiseReturnReasonList");

        var model = rrr.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) => {
            locale.Name = rrr.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> MerchandiseReturnReasonEdit(MerchandiseReturnReasonModel model,
        bool continueEditing)
    {
        var rrr = await merchandiseReturnService.GetMerchandiseReturnReasonById(model.Id);
        if (rrr == null) return RedirectToAction("MerchandiseReturnReasonList");

        var storeId = GetStoreScope();
        if (!IsStoreOwnerAccessAllowed(storeId, rrr.LimitedToStores, rrr.Stores))
            return RedirectToAction("MerchandiseReturnReasonList");

        if (ModelState.IsValid)
        {
            var existingStores = rrr.Stores;
            var existingLimitedToStores = rrr.LimitedToStores;
            rrr = model.ToEntity(rrr);
            rrr.Stores = existingStores;
            rrr.LimitedToStores = existingLimitedToStores;
            await merchandiseReturnService.UpdateMerchandiseReturnReason(rrr);
            Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("MerchandiseReturnReasonEdit", new { id = rrr.Id });
            }

            return RedirectToAction("MerchandiseReturnReasonList");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnReasonDelete(string id)
    {
        var rrr = await merchandiseReturnService.GetMerchandiseReturnReasonById(id);
        if (rrr == null) return RedirectToAction("MerchandiseReturnReasonList");

        var storeId = GetStoreScope();
        if (!IsStoreOwnerAccessAllowed(storeId, rrr.LimitedToStores, rrr.Stores))
            return RedirectToAction("MerchandiseReturnReasonList");

        await merchandiseReturnService.DeleteMerchandiseReturnReason(rrr);
        Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Deleted"));
        return RedirectToAction("MerchandiseReturnReasonList");
    }

    #endregion

    #region Merchandise return actions

    public async Task<IActionResult> MerchandiseReturnActionList()
    {
        const int customerFormFieldIndex = 1;
        await SaveSelectedTabIndex(customerFormFieldIndex);
        return RedirectToAction("Sales", "Setting");
    }

    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnActionList(DataSourceRequest command)
    {
        var storeId = GetStoreScope();
        var actions = await merchandiseReturnService.GetAllMerchandiseReturnActions(storeId);
        var gridModel = new DataSourceResult {
            Data = actions.Select(x => x.ToModel()),
            Total = actions.Count
        };
        return Json(gridModel);
    }

    public async Task<IActionResult> MerchandiseReturnActionCreate()
    {
        var storeId = GetStoreScope();
        var model = new MerchandiseReturnActionModel {
            Stores = !string.IsNullOrEmpty(storeId) ? [storeId] : []
        };
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> MerchandiseReturnActionCreate(MerchandiseReturnActionModel model,
        bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var storeId = GetStoreScope();
            if (!string.IsNullOrEmpty(storeId))
                model.Stores = [storeId];
            var rra = model.ToEntity();
            await merchandiseReturnService.InsertMerchandiseReturnAction(rra);
            await ClearCache();
            Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Added"));
            return continueEditing
                ? RedirectToAction("MerchandiseReturnActionEdit", new { id = rra.Id })
                : RedirectToAction("MerchandiseReturnActionList");
        }

        return View(model);
    }

    public async Task<IActionResult> MerchandiseReturnActionEdit(string id)
    {
        var rra = await merchandiseReturnService.GetMerchandiseReturnActionById(id);
        if (rra == null) return RedirectToAction("MerchandiseReturnActionList");

        var storeId = GetStoreScope();
        if (!IsStoreOwnerAccessAllowed(storeId, rra.LimitedToStores, rra.Stores))
            return RedirectToAction("MerchandiseReturnActionList");

        var model = rra.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) => {
            locale.Name = rra.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> MerchandiseReturnActionEdit(MerchandiseReturnActionModel model,
        bool continueEditing)
    {
        var rra = await merchandiseReturnService.GetMerchandiseReturnActionById(model.Id);
        if (rra == null) return RedirectToAction("MerchandiseReturnActionList");

        var storeId = GetStoreScope();
        if (!IsStoreOwnerAccessAllowed(storeId, rra.LimitedToStores, rra.Stores))
            return RedirectToAction("MerchandiseReturnActionList");

        if (ModelState.IsValid)
        {
            var existingStores = rra.Stores;
            var existingLimitedToStores = rra.LimitedToStores;
            rra = model.ToEntity(rra);
            rra.Stores = existingStores;
            rra.LimitedToStores = existingLimitedToStores;
            await merchandiseReturnService.UpdateMerchandiseReturnAction(rra);
            Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("MerchandiseReturnActionEdit", new { id = rra.Id });
            }

            return RedirectToAction("MerchandiseReturnActionList");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnActionDelete(string id)
    {
        var rra = await merchandiseReturnService.GetMerchandiseReturnActionById(id);
        if (rra == null) return RedirectToAction("MerchandiseReturnActionList");

        var storeId = GetStoreScope();
        if (!IsStoreOwnerAccessAllowed(storeId, rra.LimitedToStores, rra.Stores))
            return RedirectToAction("MerchandiseReturnActionList");

        await merchandiseReturnService.DeleteMerchandiseReturnAction(rra);
        Success(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Deleted"));
        return RedirectToAction("MerchandiseReturnActionList");
    }

    #endregion

    #endregion

    #region Media settings

    public async Task<IActionResult> Media()
    {
        var storeScope = GetStoreScope();
        var mediaSettings = await settingService.LoadSetting<MediaSettings>(storeScope);
        var model = mediaSettings.ToModel();
        model.ActiveStore = storeScope;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Media(MediaSettingsModel model)
    {
        var storeScope = GetStoreScope();
        var mediaSettings = await settingService.LoadSetting<MediaSettings>(storeScope);

        // Preserve file manager settings - store owners cannot change these
        var allowedFileTypes = mediaSettings.AllowedFileTypes;
        var fileManagerEnabledCommands = mediaSettings.FileManagerEnabledCommands;
        var fileManagerDisabledUICommands = mediaSettings.FileManagerDisabledUICommands;

        mediaSettings = model.ToEntity(mediaSettings);

        // Restore preserved file manager settings
        mediaSettings.AllowedFileTypes = allowedFileTypes;
        mediaSettings.FileManagerEnabledCommands = fileManagerEnabledCommands;
        mediaSettings.FileManagerDisabledUICommands = fileManagerDisabledUICommands;

        await settingService.SaveSetting(mediaSettings, storeScope);
        await ClearCache();
        await pictureService.ClearThumbs();
        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Media");
    }

    #endregion

    #region Customer settings

    public async Task<IActionResult> Customer()
    {
        var storeScope = GetStoreScope();
        var customerSettings = await settingService.LoadSetting<CustomerSettings>(storeScope);
        var addressSettings = await settingService.LoadSetting<AddressSettings>(storeScope);

        var model = new CustomerSettingsModel {
            CustomerSettings = customerSettings.ToModel(),
            AddressSettings = addressSettings.ToModel()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Customer(CustomerSettingsModel model)
    {
        var storeScope = GetStoreScope();
        var customerSettings = await settingService.LoadSetting<CustomerSettings>(storeScope);
        var addressSettings = await settingService.LoadSetting<AddressSettings>(storeScope);

        customerSettings = model.CustomerSettings.ToEntity(customerSettings);
        await settingService.SaveSetting(customerSettings, storeScope);

        addressSettings = model.AddressSettings.ToEntity(addressSettings);
        await settingService.SaveSetting(addressSettings, storeScope);

        await ClearCache();
        Success(translationService.GetResource("Admin.Configuration.Updated"));
        await SaveSelectedTabIndex();
        return RedirectToAction("Customer");
    }

    #endregion

    #region Content settings

    public async Task<IActionResult> Content()
    {
        var storeScope = GetStoreScope();
        var blogSettings = await settingService.LoadSetting<BlogSettings>(storeScope);
        var newsSettings = await settingService.LoadSetting<NewsSettings>(storeScope);
        var knowledgebaseSettings = await settingService.LoadSetting<KnowledgebaseSettings>(storeScope);
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
        var storeScope = GetStoreScope();

        var blogSettings = await settingService.LoadSetting<BlogSettings>(storeScope);
        blogSettings = model.BlogSettings.ToEntity(blogSettings);
        await settingService.SaveSetting(blogSettings, storeScope);

        var newsSettings = await settingService.LoadSetting<NewsSettings>(storeScope);
        newsSettings = model.NewsSettings.ToEntity(newsSettings);
        await settingService.SaveSetting(newsSettings, storeScope);

        var knowledgeBaseSettings = await settingService.LoadSetting<KnowledgebaseSettings>(storeScope);
        knowledgeBaseSettings = model.KnowledgebaseSettings.ToEntity(knowledgeBaseSettings);
        await settingService.SaveSetting(knowledgeBaseSettings, storeScope);

        await SaveSelectedTabIndex();
        await ClearCache();
        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Content");
    }

    #endregion
}
