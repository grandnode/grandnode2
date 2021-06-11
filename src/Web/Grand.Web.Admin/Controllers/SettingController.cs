using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.AdminSearch;
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
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.PushNotifications;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Settings)]
    public partial class SettingController : BaseAdminController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IThemeProvider _themeProvider;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IUserFieldService _userFieldService;
        private readonly IMediator _mediator;
        private readonly IMerchandiseReturnService _merchandiseReturnService;
        private readonly ILanguageService _languageService;
        private readonly IOrderStatusService _orderStatusService;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Constructors

        public SettingController(ISettingService settingService,
            ICountryService countryService,
            ITaxCategoryService taxCategoryService,
            ICurrencyService currencyService,
            IPictureService pictureService,
            ITranslationService translationService,
            IDateTimeService dateTimeService,
            IOrderService orderService,
            IEncryptionService encryptionService,
            IThemeProvider themeProvider,
            ICustomerService customerService,
            ICustomerActivityService customerActivityService,
            IStoreService storeService,
            IWorkContext workContext,
            IUserFieldService userFieldService,
            IMediator mediator,
            IMerchandiseReturnService merchandiseReturnService,
            ILanguageService languageService,
            IOrderStatusService orderStatusService,
            ICacheBase cacheBase)
        {
            _settingService = settingService;
            _currencyService = currencyService;
            _pictureService = pictureService;
            _translationService = translationService;
            _dateTimeService = dateTimeService;
            _themeProvider = themeProvider;
            _customerActivityService = customerActivityService;
            _storeService = storeService;
            _workContext = workContext;
            _userFieldService = userFieldService;
            _mediator = mediator;
            _merchandiseReturnService = merchandiseReturnService;
            _languageService = languageService;
            _orderStatusService = orderStatusService;
            _cacheBase = cacheBase;
        }

        #endregion

        #region Utilities


        protected async Task ClearCache()
        {
            await _cacheBase.Clear();
        }

        public async Task<IActionResult> ChangeStore(string storeid, string returnUrl = "")
        {
            if (storeid != null)
                storeid = storeid.Trim();

            var store = await _storeService.GetStoreById(storeid);
            if (store != null || storeid == "")
            {
                await _userFieldService.SaveField(_workContext.CurrentCustomer,
                    SystemCustomerFieldNames.AdminAreaStoreScopeConfiguration, storeid);
            }
            else
                await _userFieldService.SaveField(_workContext.CurrentCustomer,
                    SystemCustomerFieldNames.AdminAreaStoreScopeConfiguration, "");


            //home page
            if (String.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Action("Index", "Home", new { area = Constants.AreaAdmin });
            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                return RedirectToAction("Index", "Home", new { area = Constants.AreaAdmin });
            return Redirect(returnUrl);
        }
        public async Task<IActionResult> Content()
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var blogSettings = _settingService.LoadSetting<BlogSettings>(storeScope);
            var newsSettings = _settingService.LoadSetting<NewsSettings>(storeScope);
            var knowledgebaseSettings = _settingService.LoadSetting<KnowledgebaseSettings>(storeScope);
            var model = new ContentSettingsModel() {
                BlogSettings = blogSettings.ToModel(),
                NewsSettings = newsSettings.ToModel()
            };
            model.KnowledgebaseSettings.Enabled = knowledgebaseSettings.Enabled;
            model.KnowledgebaseSettings.AllowNotRegisteredUsersToLeaveComments = knowledgebaseSettings.AllowNotRegisteredUsersToLeaveComments;
            model.KnowledgebaseSettings.NotifyAboutNewArticleComments = knowledgebaseSettings.NotifyAboutNewArticleComments;

            model.ActiveStore = storeScope;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Content(ContentSettingsModel model)
        {
            var storeScope = await GetActiveStore(_storeService, _workContext);
            //blog
            var blogSettings = _settingService.LoadSetting<BlogSettings>(storeScope);
            blogSettings = model.BlogSettings.ToEntity(blogSettings);
            await _settingService.SaveSetting(blogSettings, storeScope);

            //news
            var newsSettings = _settingService.LoadSetting<NewsSettings>(storeScope);
            newsSettings = model.NewsSettings.ToEntity(newsSettings);
            await _settingService.SaveSetting(newsSettings, storeScope);

            //knowledgebase
            var knowledgeBaseSettings = _settingService.LoadSetting<KnowledgebaseSettings>(storeScope);
            knowledgeBaseSettings.Enabled = model.KnowledgebaseSettings.Enabled;
            knowledgeBaseSettings.AllowNotRegisteredUsersToLeaveComments = model.KnowledgebaseSettings.AllowNotRegisteredUsersToLeaveComments;
            knowledgeBaseSettings.NotifyAboutNewArticleComments = model.KnowledgebaseSettings.NotifyAboutNewArticleComments;
            await _settingService.SaveSetting(knowledgeBaseSettings, storeScope);

            //selected tab
            await SaveSelectedTabIndex();

            //now clear cache
            await ClearCache();

            //activity log
            await _customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            Success(_translationService.GetResource("Admin.Configuration.Updated"));
            return RedirectToAction("Content");
        }

        public async Task<IActionResult> Vendor()
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var vendorSettings = _settingService.LoadSetting<VendorSettings>(storeScope);
            var model = vendorSettings.ToModel();
            model.AddressSettings.CityEnabled = vendorSettings.CityEnabled;
            model.AddressSettings.CityRequired = vendorSettings.CityRequired;
            model.AddressSettings.CompanyEnabled = vendorSettings.CompanyEnabled;
            model.AddressSettings.CompanyRequired = vendorSettings.CompanyRequired;
            model.AddressSettings.CountryEnabled = vendorSettings.CountryEnabled;
            model.AddressSettings.FaxEnabled = vendorSettings.FaxEnabled;
            model.AddressSettings.FaxRequired = vendorSettings.FaxRequired;
            model.AddressSettings.PhoneEnabled = vendorSettings.PhoneEnabled;
            model.AddressSettings.PhoneRequired = vendorSettings.PhoneRequired;
            model.AddressSettings.StateProvinceEnabled = vendorSettings.StateProvinceEnabled;
            model.AddressSettings.StreetAddress2Enabled = vendorSettings.StreetAddress2Enabled;
            model.AddressSettings.StreetAddress2Required = vendorSettings.StreetAddress2Required;
            model.AddressSettings.StreetAddressEnabled = vendorSettings.StreetAddressEnabled;
            model.AddressSettings.StreetAddressRequired = vendorSettings.StreetAddressRequired;
            model.AddressSettings.ZipPostalCodeEnabled = vendorSettings.ZipPostalCodeEnabled;
            model.AddressSettings.ZipPostalCodeRequired = vendorSettings.ZipPostalCodeRequired;
            model.ActiveStore = storeScope;

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Vendor(VendorSettingsModel model)
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var vendorSettings = _settingService.LoadSetting<VendorSettings>(storeScope);
            vendorSettings = model.ToEntity(vendorSettings);
            vendorSettings.CityEnabled = model.AddressSettings.CityEnabled;
            vendorSettings.CityRequired = model.AddressSettings.CityRequired;
            vendorSettings.CompanyEnabled = model.AddressSettings.CompanyEnabled;
            vendorSettings.CompanyRequired = model.AddressSettings.CompanyRequired;
            vendorSettings.CountryEnabled = model.AddressSettings.CountryEnabled;
            vendorSettings.FaxEnabled = model.AddressSettings.FaxEnabled;
            vendorSettings.FaxRequired = model.AddressSettings.FaxRequired;
            vendorSettings.PhoneEnabled = model.AddressSettings.PhoneEnabled;
            vendorSettings.PhoneRequired = model.AddressSettings.PhoneRequired;
            vendorSettings.StateProvinceEnabled = model.AddressSettings.StateProvinceEnabled;
            vendorSettings.StreetAddress2Enabled = model.AddressSettings.StreetAddress2Enabled;
            vendorSettings.StreetAddress2Required = model.AddressSettings.StreetAddress2Required;
            vendorSettings.StreetAddressEnabled = model.AddressSettings.StreetAddressEnabled;
            vendorSettings.StreetAddressRequired = model.AddressSettings.StreetAddressRequired;
            vendorSettings.ZipPostalCodeEnabled = model.AddressSettings.ZipPostalCodeEnabled;
            vendorSettings.ZipPostalCodeRequired = model.AddressSettings.ZipPostalCodeRequired;

            await _settingService.SaveSetting(vendorSettings, storeScope);

            //now clear cache
            await ClearCache();

            //activity log
            await _customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            Success(_translationService.GetResource("Admin.Configuration.Updated"));
            return RedirectToAction("Vendor");
        }

        public async Task<IActionResult> Catalog()
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var catalogSettings = _settingService.LoadSetting<CatalogSettings>(storeScope);
            var model = catalogSettings.ToModel();
            model.ActiveStore = storeScope;
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Catalog(CatalogSettingsModel model)
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var catalogSettings = _settingService.LoadSetting<CatalogSettings>(storeScope);
            catalogSettings = model.ToEntity(catalogSettings);

            await _settingService.SaveSetting(catalogSettings, storeScope);

            //now clear cache
            await ClearCache();

            //activity log
            await _customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            Success(_translationService.GetResource("Admin.Configuration.Updated"));

            //selected tab
            await SaveSelectedTabIndex();

            return RedirectToAction("Catalog");
        }

        #region Sort options

        [HttpPost]
        public async Task<IActionResult> SortOptionsList(DataSourceRequest command)
        {
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var catalogSettings = _settingService.LoadSetting<CatalogSettings>(storeScope);
            var model = new List<SortOptionModel>();
            foreach (int option in Enum.GetValues(typeof(ProductSortingEnum)))
            {
                model.Add(new SortOptionModel() {
                    Id = option,
                    Name = ((ProductSortingEnum)option).GetTranslationEnum(_translationService, _workContext),
                    IsActive = !catalogSettings.ProductSortingEnumDisabled.Contains(option),
                    DisplayOrder = catalogSettings.ProductSortingEnumDisplayOrder.TryGetValue(option, out int value) ? value : option
                });
            }
            var gridModel = new DataSourceResult {
                Data = model.OrderBy(option => option.DisplayOrder),
                Total = model.Count
            };
            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> SortOptionUpdate(SortOptionModel model)
        {
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var catalogSettings = _settingService.LoadSetting<CatalogSettings>(storeScope);

            catalogSettings.ProductSortingEnumDisplayOrder[model.Id] = model.DisplayOrder;
            if (model.IsActive && catalogSettings.ProductSortingEnumDisabled.Contains(model.Id))
                catalogSettings.ProductSortingEnumDisabled.Remove(model.Id);
            if (!model.IsActive && !catalogSettings.ProductSortingEnumDisabled.Contains(model.Id))
                catalogSettings.ProductSortingEnumDisabled.Add(model.Id);

            await _settingService.SaveSetting(catalogSettings, storeScope);

            await ClearCache();

            return new JsonResult("");
        }

        #endregion

        public async Task<IActionResult> Sales()
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var loyaltyPointsSettings = _settingService.LoadSetting<LoyaltyPointsSettings>(storeScope);
            var orderSettings = _settingService.LoadSetting<OrderSettings>(storeScope);
            var shoppingCartSettings = _settingService.LoadSetting<ShoppingCartSettings>(storeScope);

            var model = new SalesSettingsModel() {
                LoyaltyPointsSettings = loyaltyPointsSettings.ToModel(),
                OrderSettings = orderSettings.ToModel(),
                ShoppingCartSettings = shoppingCartSettings.ToModel(),
                ActiveStore = storeScope
            };

            var currencySettings = _settingService.LoadSetting<CurrencySettings>("");
            var currency = await _currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId);

            //loyal
            model.LoyaltyPointsSettings.PrimaryStoreCurrencyCode = currency?.CurrencyCode;
            //order statuses
            var status = await _orderStatusService.GetAll();
            model.LoyaltyPointsSettings.PointsForPurchases_Awarded_OrderStatuses = status.Select(x => new SelectListItem() { Value = x.StatusId.ToString(), Text = x.Name }).ToList();

            //orders
            model.OrderSettings.PrimaryStoreCurrencyCode = currency?.CurrencyCode;

            //gift voucher activation
            model.OrderSettings.GiftVouchers_Activated_OrderStatuses = status.Select(x => new SelectListItem() { Value = x.StatusId.ToString(), Text = x.Name }).ToList();
            model.OrderSettings.GiftVouchers_Activated_OrderStatuses.Insert(0, new SelectListItem { Text = "---", Value = "0" });

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Sales(SalesSettingsModel model)
        {
            var storeScope = await GetActiveStore(_storeService, _workContext);

            if (ModelState.IsValid)
            {
                var loyaltyPointsSettings = _settingService.LoadSetting<LoyaltyPointsSettings>(storeScope);
                loyaltyPointsSettings = model.LoyaltyPointsSettings.ToEntity(loyaltyPointsSettings);
                await _settingService.SaveSetting(loyaltyPointsSettings, storeScope);

                var shoppingCartSettings = _settingService.LoadSetting<ShoppingCartSettings>(storeScope);
                shoppingCartSettings = model.ShoppingCartSettings.ToEntity(shoppingCartSettings);
                await _settingService.SaveSetting(shoppingCartSettings, storeScope);

                var orderSettings = _settingService.LoadSetting<OrderSettings>(storeScope);
                orderSettings = model.OrderSettings.ToEntity(orderSettings);

                await _settingService.SaveSetting(orderSettings, storeScope);

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

            //activity log
            await _customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            Success(_translationService.GetResource("Admin.Configuration.Updated"));
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
            var reasons = await _merchandiseReturnService.GetAllMerchandiseReturnReasons();
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
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> MerchandiseReturnReasonCreate(MerchandiseReturnReasonModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var rrr = model.ToEntity();
                await _merchandiseReturnService.InsertMerchandiseReturnReason(rrr);

                Success(_translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Added"));
                return continueEditing ? RedirectToAction("MerchandiseReturnReasonEdit", new { id = rrr.Id }) : RedirectToAction("MerchandiseReturnReasonList");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }
        //edit
        public async Task<IActionResult> MerchandiseReturnReasonEdit(string id)
        {
            var rrr = await _merchandiseReturnService.GetMerchandiseReturnReasonById(id);
            if (rrr == null)
                //No reason found with the specified id
                return RedirectToAction("MerchandiseReturnReasonList");

            var model = rrr.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = rrr.GetTranslation(x => x.Name, languageId, false);
            });
            return View(model);
        }
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> MerchandiseReturnReasonEdit(MerchandiseReturnReasonModel model, bool continueEditing)
        {
            var rrr = await _merchandiseReturnService.GetMerchandiseReturnReasonById(model.Id);
            if (rrr == null)
                //No reason found with the specified id
                return RedirectToAction("MerchandiseReturnReasonList");

            if (ModelState.IsValid)
            {
                rrr = model.ToEntity(rrr);
                await _merchandiseReturnService.UpdateMerchandiseReturnReason(rrr);

                Success(_translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Updated"));
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
            var rrr = await _merchandiseReturnService.GetMerchandiseReturnReasonById(id);
            await _merchandiseReturnService.DeleteMerchandiseReturnReason(rrr);

            Success(_translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Deleted"));
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
            var actions = await _merchandiseReturnService.GetAllMerchandiseReturnActions();
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
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> MerchandiseReturnActionCreate(MerchandiseReturnActionModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var rra = model.ToEntity();
                await _merchandiseReturnService.InsertMerchandiseReturnAction(rra);

                //now clear cache
                await ClearCache();

                Success(_translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Added"));
                return continueEditing ? RedirectToAction("MerchandiseReturnActionEdit", new { id = rra.Id }) : RedirectToAction("MerchandiseReturnActionList");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }
        //edit
        public async Task<IActionResult> MerchandiseReturnActionEdit(string id)
        {
            var rra = await _merchandiseReturnService.GetMerchandiseReturnActionById(id);
            if (rra == null)
                //No action found with the specified id
                return RedirectToAction("MerchandiseReturnActionList");

            var model = rra.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = rra.GetTranslation(x => x.Name, languageId, false);
            });
            return View(model);
        }
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> MerchandiseReturnActionEdit(MerchandiseReturnActionModel model, bool continueEditing)
        {
            var rra = await _merchandiseReturnService.GetMerchandiseReturnActionById(model.Id);
            if (rra == null)
                //No action found with the specified id
                return RedirectToAction("MerchandiseReturnActionList");

            if (ModelState.IsValid)
            {
                rra = model.ToEntity(rra);
                await _merchandiseReturnService.UpdateMerchandiseReturnAction(rra);

                Success(_translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Updated"));
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
            var rra = await _merchandiseReturnService.GetMerchandiseReturnActionById(id);
            await _merchandiseReturnService.DeleteMerchandiseReturnAction(rra);

            Success(_translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Deleted"));
            return RedirectToAction("MerchandiseReturnActionList");
        }
        #endregion
        public async Task<IActionResult> Media()
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var mediaSettings = _settingService.LoadSetting<MediaSettings>(storeScope);
            var model = mediaSettings.ToModel();
            model.ActiveStore = storeScope;

            var mediaStoreSetting = _settingService.LoadSetting<MediaSettings>("");
            model.PicturesStoredIntoDatabase = mediaStoreSetting.StoreInDb;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Media(MediaSettingsModel model)
        {
            //load settings for a chosen store scope
            var mediaSettings = _settingService.LoadSetting<MediaSettings>();
            mediaSettings = model.ToEntity(mediaSettings);

            await _settingService.SaveSetting(mediaSettings);

            //now clear cache
            await ClearCache();

            //clear old Thumbs
            await _pictureService.ClearThumbs();

            //activity log
            await _customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            Success(_translationService.GetResource("Admin.Configuration.Updated"));
            return RedirectToAction("Media");
        }
        [HttpPost]
        public async Task<IActionResult> ChangePictureStorage()
        {
            var mediaSettings = _settingService.LoadSetting<MediaSettings>();
            var storeIdDb = !mediaSettings.StoreInDb;
            mediaSettings.StoreInDb = storeIdDb;

            //save the new setting value
            await _settingService.SaveSetting(mediaSettings);

            int pageIndex = 0;
            const int pageSize = 100;
            try
            {
                while (true)
                {
                    var pictures = _pictureService.GetPictures(pageIndex, pageSize);
                    pageIndex++;
                    if (!pictures.Any())
                        break;

                    foreach (var picture in pictures)
                    {
                        var pictureBinary = await _pictureService.LoadPictureBinary(picture, !storeIdDb);
                        if (storeIdDb)
                            await _pictureService.DeletePictureOnFileSystem(picture);
                        else
                            //now on file system
                            await _pictureService.SavePictureInFile(picture.Id, pictureBinary, picture.MimeType);
                        picture.PictureBinary = storeIdDb ? pictureBinary : new byte[0];
                        picture.IsNew = true;

                        await _pictureService.UpdatePicture(picture);
                    }
                }
            }
            finally
            {
            }

            //activity log
            await _customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            //now clear cache
            await ClearCache();

            Success(_translationService.GetResource("Admin.Configuration.Updated"));
            return RedirectToAction("Media");
        }

        public async Task<IActionResult> Customer()
        {
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var customerSettings = _settingService.LoadSetting<CustomerSettings>(storeScope);
            var addressSettings = _settingService.LoadSetting<AddressSettings>(storeScope);

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
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var customerSettings = _settingService.LoadSetting<CustomerSettings>(storeScope);
            var addressSettings = _settingService.LoadSetting<AddressSettings>(storeScope);

            customerSettings = model.CustomerSettings.ToEntity(customerSettings);
            await _settingService.SaveSetting(customerSettings, storeScope);

            addressSettings = model.AddressSettings.ToEntity(addressSettings);
            await _settingService.SaveSetting(addressSettings, storeScope);

            //now clear cache
            await ClearCache();

            //activity log
            await _customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            Success(_translationService.GetResource("Admin.Configuration.Updated"));

            //selected tab
            await SaveSelectedTabIndex();

            return RedirectToAction("Customer");
        }
        public async Task<IActionResult> GeneralCommon()
        {
            var model = new GeneralCommonSettingsModel();
            var storeScope = await GetActiveStore(_storeService, _workContext);
            model.ActiveStore = storeScope;
            //store information
            var storeInformationSettings = _settingService.LoadSetting<StoreInformationSettings>(storeScope);
            var commonSettings = _settingService.LoadSetting<CommonSettings>(storeScope);
            var googleAnalyticsSettings = _settingService.LoadSetting<GoogleAnalyticsSettings>(storeScope);
            var adminareasettings = _settingService.LoadSetting<AdminAreaSettings>(storeScope);
            var dateTimeSettings = _settingService.LoadSetting<DateTimeSettings>(storeScope);

            model.DateTimeSettings.DefaultStoreTimeZoneId = dateTimeSettings.DefaultStoreTimeZoneId;
            var iswindows = Grand.Infrastructure.OperatingSystem.IsWindows();
            foreach (TimeZoneInfo timeZone in _dateTimeService.GetSystemTimeZones())
            {
                var name = iswindows ? timeZone.DisplayName : $"{timeZone.StandardName} ({timeZone.Id})";
                model.DateTimeSettings.AvailableTimeZones.Add(new SelectListItem {
                    Text = name,
                    Value = timeZone.Id,
                    Selected = timeZone.Id.Equals(dateTimeSettings.DefaultStoreTimeZoneId, StringComparison.OrdinalIgnoreCase)
                });
            }

            model.StoreInformationSettings.StoreClosed = storeInformationSettings.StoreClosed;

            //themes
            model.StoreInformationSettings.DefaultStoreTheme = storeInformationSettings.DefaultStoreTheme;
            model.StoreInformationSettings.AvailableStoreThemes = _themeProvider
                .GetConfigurations()
                .Select(x => new GeneralCommonSettingsModel.StoreInformationSettingsModel.ThemeConfigurationModel {
                    ThemeTitle = x.Title,
                    ThemeName = x.Name,
                    ThemeVersion = x.Version,
                    PreviewImageUrl = x.PreviewImageUrl,
                    PreviewText = x.PreviewText,
                    SupportRtl = x.SupportRtl,
                    Selected = x.Name.Equals(storeInformationSettings.DefaultStoreTheme, StringComparison.OrdinalIgnoreCase)
                })
                .ToList();
            model.StoreInformationSettings.AllowCustomerToSelectTheme = storeInformationSettings.AllowCustomerToSelectTheme;
            model.StoreInformationSettings.AllowToSelectAdminTheme = storeInformationSettings.AllowToSelectAdminTheme;

            model.StoreInformationSettings.LogoPicture = storeInformationSettings.LogoPicture;
            //EU Cookie law
            model.StoreInformationSettings.DisplayCookieInformation = storeInformationSettings.DisplayCookieInformation;
            model.StoreInformationSettings.DisplayPrivacyPreference = storeInformationSettings.DisplayPrivacyPreference;
            //social pages
            model.StoreInformationSettings.FacebookLink = storeInformationSettings.FacebookLink;
            model.StoreInformationSettings.TwitterLink = storeInformationSettings.TwitterLink;
            model.StoreInformationSettings.YoutubeLink = storeInformationSettings.YoutubeLink;
            model.StoreInformationSettings.InstagramLink = storeInformationSettings.InstagramLink;
            model.StoreInformationSettings.LinkedInLink = storeInformationSettings.LinkedInLink;
            model.StoreInformationSettings.PinterestLink = storeInformationSettings.PinterestLink;

            //common
            model.StoreInformationSettings.StoreInDatabaseContactUsForm = commonSettings.StoreInDatabaseContactUsForm;
            model.StoreInformationSettings.SubjectFieldOnContactUsForm = commonSettings.SubjectFieldOnContactUsForm;
            model.StoreInformationSettings.UseSystemEmailForContactUsForm = commonSettings.UseSystemEmailForContactUsForm;
            model.StoreInformationSettings.SitemapEnabled = commonSettings.SitemapEnabled;
            model.StoreInformationSettings.SitemapIncludeCategories = commonSettings.SitemapIncludeCategories;
            model.StoreInformationSettings.SitemapIncludeImage = commonSettings.SitemapIncludeBrands;
            model.StoreInformationSettings.SitemapIncludeBrands = commonSettings.SitemapIncludeBrands;
            model.StoreInformationSettings.SitemapIncludeProducts = commonSettings.SitemapIncludeProducts;
            model.StoreInformationSettings.AllowToSelectStore = commonSettings.AllowToSelectStore;
            model.StoreInformationSettings.AllowToReadLetsEncryptFile = commonSettings.AllowToReadLetsEncryptFile;
            model.StoreInformationSettings.Log404Errors = commonSettings.Log404Errors;
            model.StoreInformationSettings.PopupForTermsOfServiceLinks = commonSettings.PopupForTermsOfServiceLinks;

            //seo settings
            var seoSettings = _settingService.LoadSetting<SeoSettings>(storeScope);
            model.SeoSettings.PageTitleSeparator = seoSettings.PageTitleSeparator;
            model.SeoSettings.PageTitleSeoAdjustment = seoSettings.PageTitleSeoAdjustment;
            model.SeoSettings.DefaultTitle = seoSettings.DefaultTitle;
            model.SeoSettings.DefaultMetaKeywords = seoSettings.DefaultMetaKeywords;
            model.SeoSettings.DefaultMetaDescription = seoSettings.DefaultMetaDescription;
            model.SeoSettings.GenerateProductMetaDescription = seoSettings.GenerateProductMetaDescription;
            model.SeoSettings.ConvertNonWesternChars = seoSettings.ConvertNonWesternChars;
            model.SeoSettings.SeoCharConversion = seoSettings.SeoCharConversion;
            model.SeoSettings.CanonicalUrlsEnabled = seoSettings.CanonicalUrlsEnabled;
            model.SeoSettings.TwitterMetaTags = seoSettings.TwitterMetaTags;
            model.SeoSettings.OpenGraphMetaTags = seoSettings.OpenGraphMetaTags;
            model.SeoSettings.StorePictureId = seoSettings.StorePictureId;
            model.SeoSettings.AllowSlashChar = seoSettings.AllowSlashChar;
            model.SeoSettings.AllowUnicodeCharsInUrls = seoSettings.AllowUnicodeCharsInUrls;
            model.SeoSettings.CustomHeadTags = seoSettings.CustomHeadTags;

            //security settings
            var securitySettings = _settingService.LoadSetting<SecuritySettings>(storeScope);
            var captchaSettings = _settingService.LoadSetting<CaptchaSettings>(storeScope);
            if (securitySettings.AdminAreaAllowedIpAddresses != null)
                for (int i = 0; i < securitySettings.AdminAreaAllowedIpAddresses.Count; i++)
                {
                    model.SecuritySettings.AdminAreaAllowedIpAddresses += securitySettings.AdminAreaAllowedIpAddresses[i];
                    if (i != securitySettings.AdminAreaAllowedIpAddresses.Count - 1)
                        model.SecuritySettings.AdminAreaAllowedIpAddresses += ",";
                }
            model.SecuritySettings.CaptchaEnabled = captchaSettings.Enabled;
            model.SecuritySettings.CaptchaShowOnLoginPage = captchaSettings.ShowOnLoginPage;
            model.SecuritySettings.CaptchaShowOnRegistrationPage = captchaSettings.ShowOnRegistrationPage;
            model.SecuritySettings.CaptchaShowOnPasswordRecoveryPage = captchaSettings.ShowOnPasswordRecoveryPage;
            model.SecuritySettings.CaptchaShowOnContactUsPage = captchaSettings.ShowOnContactUsPage;
            model.SecuritySettings.CaptchaShowOnEmailWishlistToFriendPage = captchaSettings.ShowOnEmailWishlistToFriendPage;
            model.SecuritySettings.CaptchaShowOnEmailProductToFriendPage = captchaSettings.ShowOnEmailProductToFriendPage;
            model.SecuritySettings.CaptchaShowOnAskQuestionPage = captchaSettings.ShowOnAskQuestionPage;
            model.SecuritySettings.CaptchaShowOnBlogCommentPage = captchaSettings.ShowOnBlogCommentPage;
            model.SecuritySettings.CaptchaShowOnArticleCommentPage = captchaSettings.ShowOnArticleCommentPage;
            model.SecuritySettings.CaptchaShowOnNewsCommentPage = captchaSettings.ShowOnNewsCommentPage;
            model.SecuritySettings.CaptchaShowOnProductReviewPage = captchaSettings.ShowOnProductReviewPage;
            model.SecuritySettings.CaptchaShowOnApplyVendorPage = captchaSettings.ShowOnApplyVendorPage;
            model.SecuritySettings.ReCaptchaVersion = captchaSettings.ReCaptchaVersion;
            model.SecuritySettings.AvailableReCaptchaVersions = GoogleReCaptchaVersion.V2.ToSelectList(HttpContext, false).ToList();
            model.SecuritySettings.ReCaptchaPublicKey = captchaSettings.ReCaptchaPublicKey;
            model.SecuritySettings.ReCaptchaPrivateKey = captchaSettings.ReCaptchaPrivateKey;

            //PDF settings
            var pdfSettings = _settingService.LoadSetting<PdfSettings>(storeScope);
            model.PdfSettings.LogoPictureId = pdfSettings.LogoPictureId;
            model.PdfSettings.DisablePdfInvoicesForPendingOrders = pdfSettings.DisablePdfInvoicesForPendingOrders;
            model.PdfSettings.InvoiceHeaderText = pdfSettings.InvoiceHeaderText;
            model.PdfSettings.InvoiceFooterText = pdfSettings.InvoiceFooterText;

            //google analytics
            model.GoogleAnalyticsSettings.GaprivateKey = googleAnalyticsSettings.gaprivateKey;
            model.GoogleAnalyticsSettings.GaserviceAccountEmail = googleAnalyticsSettings.gaserviceAccountEmail;
            model.GoogleAnalyticsSettings.GaviewID = googleAnalyticsSettings.gaviewID;

            //display menu settings
            var displayMenuItemSettings = _settingService.LoadSetting<MenuItemSettings>(storeScope);
            model.DisplayMenuSettings.DisplayHomePageMenu = displayMenuItemSettings.DisplayHomePageMenu;
            model.DisplayMenuSettings.DisplayNewProductsMenu = displayMenuItemSettings.DisplayNewProductsMenu;
            model.DisplayMenuSettings.DisplaySearchMenu = displayMenuItemSettings.DisplaySearchMenu;
            model.DisplayMenuSettings.DisplayCustomerMenu = displayMenuItemSettings.DisplayCustomerMenu;
            model.DisplayMenuSettings.DisplayBlogMenu = displayMenuItemSettings.DisplayBlogMenu;
            model.DisplayMenuSettings.DisplayContactUsMenu = displayMenuItemSettings.DisplayContactUsMenu;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GeneralCommon(GeneralCommonSettingsModel model)
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);

            //store information settings
            var storeInformationSettings = _settingService.LoadSetting<StoreInformationSettings>(storeScope);
            var commonSettings = _settingService.LoadSetting<CommonSettings>(storeScope);

            storeInformationSettings.StoreClosed = model.StoreInformationSettings.StoreClosed;
            storeInformationSettings.DefaultStoreTheme = model.StoreInformationSettings.DefaultStoreTheme;
            storeInformationSettings.AllowCustomerToSelectTheme = model.StoreInformationSettings.AllowCustomerToSelectTheme;
            storeInformationSettings.AllowToSelectAdminTheme = model.StoreInformationSettings.AllowToSelectAdminTheme;
            storeInformationSettings.LogoPicture = model.StoreInformationSettings.LogoPicture;
            //EU Cookie law
            storeInformationSettings.DisplayCookieInformation = model.StoreInformationSettings.DisplayCookieInformation;
            storeInformationSettings.DisplayPrivacyPreference = model.StoreInformationSettings.DisplayPrivacyPreference;
            //social pages
            storeInformationSettings.FacebookLink = model.StoreInformationSettings.FacebookLink;
            storeInformationSettings.TwitterLink = model.StoreInformationSettings.TwitterLink;
            storeInformationSettings.YoutubeLink = model.StoreInformationSettings.YoutubeLink;
            storeInformationSettings.InstagramLink = model.StoreInformationSettings.InstagramLink;
            storeInformationSettings.LinkedInLink = model.StoreInformationSettings.LinkedInLink;
            storeInformationSettings.PinterestLink = model.StoreInformationSettings.PinterestLink;

            await _settingService.SaveSetting(storeInformationSettings, storeScope);

            var dateTimeSettings = _settingService.LoadSetting<DateTimeSettings>(storeScope);
            dateTimeSettings.DefaultStoreTimeZoneId = model.DateTimeSettings.DefaultStoreTimeZoneId;
            await _settingService.SaveSetting(dateTimeSettings, storeScope);

            //contact us
            commonSettings.StoreInDatabaseContactUsForm = model.StoreInformationSettings.StoreInDatabaseContactUsForm;
            commonSettings.SubjectFieldOnContactUsForm = model.StoreInformationSettings.SubjectFieldOnContactUsForm;
            commonSettings.UseSystemEmailForContactUsForm = model.StoreInformationSettings.UseSystemEmailForContactUsForm;
            commonSettings.SitemapEnabled = model.StoreInformationSettings.SitemapEnabled;
            commonSettings.SitemapIncludeCategories = model.StoreInformationSettings.SitemapIncludeCategories;
            commonSettings.SitemapIncludeImage = model.StoreInformationSettings.SitemapIncludeImage;
            commonSettings.SitemapIncludeBrands = model.StoreInformationSettings.SitemapIncludeBrands;
            commonSettings.SitemapIncludeProducts = model.StoreInformationSettings.SitemapIncludeProducts;
            commonSettings.AllowToSelectStore = model.StoreInformationSettings.AllowToSelectStore;
            commonSettings.AllowToReadLetsEncryptFile = model.StoreInformationSettings.AllowToReadLetsEncryptFile;
            commonSettings.Log404Errors = model.StoreInformationSettings.Log404Errors;
            commonSettings.PopupForTermsOfServiceLinks = model.StoreInformationSettings.PopupForTermsOfServiceLinks;

            await _settingService.SaveSetting(commonSettings, storeScope);

            //seo settings
            var seoSettings = _settingService.LoadSetting<SeoSettings>(storeScope);
            seoSettings.PageTitleSeparator = model.SeoSettings.PageTitleSeparator;
            seoSettings.PageTitleSeoAdjustment = model.SeoSettings.PageTitleSeoAdjustment;
            seoSettings.DefaultTitle = model.SeoSettings.DefaultTitle;
            seoSettings.DefaultMetaKeywords = model.SeoSettings.DefaultMetaKeywords;
            seoSettings.DefaultMetaDescription = model.SeoSettings.DefaultMetaDescription;
            seoSettings.GenerateProductMetaDescription = model.SeoSettings.GenerateProductMetaDescription;
            seoSettings.ConvertNonWesternChars = model.SeoSettings.ConvertNonWesternChars;
            seoSettings.SeoCharConversion = model.SeoSettings.SeoCharConversion;
            seoSettings.CanonicalUrlsEnabled = model.SeoSettings.CanonicalUrlsEnabled;
            seoSettings.TwitterMetaTags = model.SeoSettings.TwitterMetaTags;
            seoSettings.OpenGraphMetaTags = model.SeoSettings.OpenGraphMetaTags;
            seoSettings.StorePictureId = model.SeoSettings.StorePictureId;
            seoSettings.AllowSlashChar = model.SeoSettings.AllowSlashChar;
            seoSettings.AllowUnicodeCharsInUrls = model.SeoSettings.AllowUnicodeCharsInUrls;
            seoSettings.CustomHeadTags = model.SeoSettings.CustomHeadTags;

            await _settingService.SaveSetting(seoSettings, storeScope);

            //security settings
            var securitySettings = _settingService.LoadSetting<SecuritySettings>(storeScope);
            var captchaSettings = _settingService.LoadSetting<CaptchaSettings>(storeScope);
            if (securitySettings.AdminAreaAllowedIpAddresses == null)
                securitySettings.AdminAreaAllowedIpAddresses = new List<string>();
            securitySettings.AdminAreaAllowedIpAddresses.Clear();
            if (!String.IsNullOrEmpty(model.SecuritySettings.AdminAreaAllowedIpAddresses))
                foreach (string s in model.SecuritySettings.AdminAreaAllowedIpAddresses.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    if (!String.IsNullOrWhiteSpace(s))
                        securitySettings.AdminAreaAllowedIpAddresses.Add(s.Trim());

            await _settingService.SaveSetting(securitySettings);
            captchaSettings.Enabled = model.SecuritySettings.CaptchaEnabled;
            captchaSettings.ShowOnLoginPage = model.SecuritySettings.CaptchaShowOnLoginPage;
            captchaSettings.ShowOnRegistrationPage = model.SecuritySettings.CaptchaShowOnRegistrationPage;
            captchaSettings.ShowOnPasswordRecoveryPage = model.SecuritySettings.CaptchaShowOnPasswordRecoveryPage;
            captchaSettings.ShowOnContactUsPage = model.SecuritySettings.CaptchaShowOnContactUsPage;
            captchaSettings.ShowOnEmailWishlistToFriendPage = model.SecuritySettings.CaptchaShowOnEmailWishlistToFriendPage;
            captchaSettings.ShowOnAskQuestionPage = model.SecuritySettings.CaptchaShowOnAskQuestionPage;
            captchaSettings.ShowOnEmailProductToFriendPage = model.SecuritySettings.CaptchaShowOnEmailProductToFriendPage;
            captchaSettings.ShowOnBlogCommentPage = model.SecuritySettings.CaptchaShowOnBlogCommentPage;
            captchaSettings.ShowOnArticleCommentPage = model.SecuritySettings.CaptchaShowOnArticleCommentPage;
            captchaSettings.ShowOnNewsCommentPage = model.SecuritySettings.CaptchaShowOnNewsCommentPage;
            captchaSettings.ShowOnProductReviewPage = model.SecuritySettings.CaptchaShowOnProductReviewPage;
            captchaSettings.ShowOnApplyVendorPage = model.SecuritySettings.CaptchaShowOnApplyVendorPage;
            captchaSettings.ReCaptchaVersion = model.SecuritySettings.ReCaptchaVersion;
            captchaSettings.ReCaptchaPublicKey = model.SecuritySettings.ReCaptchaPublicKey;
            captchaSettings.ReCaptchaPrivateKey = model.SecuritySettings.ReCaptchaPrivateKey;
            await _settingService.SaveSetting(captchaSettings);
            if (captchaSettings.Enabled &&
                (String.IsNullOrWhiteSpace(captchaSettings.ReCaptchaPublicKey) || String.IsNullOrWhiteSpace(captchaSettings.ReCaptchaPrivateKey)))
            {
                //captcha is enabled but the keys are not entered
                Error("Captcha is enabled but the appropriate keys are not entered");
            }

            //PDF settings
            var pdfSettings = _settingService.LoadSetting<PdfSettings>(storeScope);
            pdfSettings.LogoPictureId = model.PdfSettings.LogoPictureId;
            pdfSettings.DisablePdfInvoicesForPendingOrders = model.PdfSettings.DisablePdfInvoicesForPendingOrders;
            pdfSettings.InvoiceHeaderText = model.PdfSettings.InvoiceHeaderText;
            pdfSettings.InvoiceFooterText = model.PdfSettings.InvoiceFooterText;
            await _settingService.SaveSetting(pdfSettings, storeScope);

            //admin settings
            var adminareasettings = _settingService.LoadSetting<AdminAreaSettings>(storeScope);

            await _settingService.SaveSetting(adminareasettings);

            //googleanalytics settings
            var googleAnalyticsSettings = _settingService.LoadSetting<GoogleAnalyticsSettings>(storeScope);
            googleAnalyticsSettings.gaprivateKey = model.GoogleAnalyticsSettings.GaprivateKey;
            googleAnalyticsSettings.gaserviceAccountEmail = model.GoogleAnalyticsSettings.GaserviceAccountEmail;
            googleAnalyticsSettings.gaviewID = model.GoogleAnalyticsSettings.GaviewID;

            await _settingService.SaveSetting(googleAnalyticsSettings, storeScope);

            //Menu item settings
            var displayMenuItemSettings = _settingService.LoadSetting<MenuItemSettings>(storeScope);
            displayMenuItemSettings.DisplayHomePageMenu = model.DisplayMenuSettings.DisplayHomePageMenu;
            displayMenuItemSettings.DisplayNewProductsMenu = model.DisplayMenuSettings.DisplayNewProductsMenu;
            displayMenuItemSettings.DisplaySearchMenu = model.DisplayMenuSettings.DisplaySearchMenu;
            displayMenuItemSettings.DisplayCustomerMenu = model.DisplayMenuSettings.DisplayCustomerMenu;
            displayMenuItemSettings.DisplayBlogMenu = model.DisplayMenuSettings.DisplayBlogMenu;
            displayMenuItemSettings.DisplayContactUsMenu = model.DisplayMenuSettings.DisplayContactUsMenu;

            await _settingService.SaveSetting(displayMenuItemSettings, storeScope);

            //now clear cache
            await ClearCache();

            //activity log
            await _customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            Success(_translationService.GetResource("Admin.Configuration.Updated"));

            //selected tab
            await SaveSelectedTabIndex();

            return RedirectToAction("GeneralCommon");
        }

        public async Task<IActionResult> PushNotifications()
        {
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var settings = _settingService.LoadSetting<PushNotificationsSettings>(storeScope);

            var model = new ConfigurationModel {
                AllowGuestNotifications = settings.AllowGuestNotifications,
                AuthDomain = settings.AuthDomain,
                DatabaseUrl = settings.DatabaseUrl,
                ProjectId = settings.ProjectId,
                PushApiKey = settings.PublicApiKey,
                SenderId = settings.SenderId,
                StorageBucket = settings.StorageBucket,
                PrivateApiKey = settings.PrivateApiKey,
                AppId = settings.AppId,
                Enabled = settings.Enabled
            };

            return View(model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PushNotifications(ConfigurationModel model)
        {
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var settings = _settingService.LoadSetting<PushNotificationsSettings>(storeScope);
            settings.AllowGuestNotifications = model.AllowGuestNotifications;
            settings.AuthDomain = model.AuthDomain;
            settings.DatabaseUrl = model.DatabaseUrl;
            settings.ProjectId = model.ProjectId;
            settings.PublicApiKey = model.PushApiKey;
            settings.SenderId = model.SenderId;
            settings.StorageBucket = model.StorageBucket;
            settings.PrivateApiKey = model.PrivateApiKey;
            settings.AppId = model.AppId;
            settings.Enabled = model.Enabled;
            await _settingService.SaveSetting(settings);

            //now clear cache
            await ClearCache();

            //edit js file needed by firebase
            var jsFilePath = CommonPath.WebMapPath("firebase-messaging-sw.js");
            if (System.IO.File.Exists(jsFilePath))
            {
                string[] lines = System.IO.File.ReadAllLines(jsFilePath);

                int i = 0;
                foreach (var line in lines)
                {
                    if (line.Contains("apiKey"))
                    {
                        lines[i] = "apiKey: \"" + model.PushApiKey + "\",";
                    }

                    if (line.Contains("authDomain"))
                    {
                        lines[i] = "authDomain: \"" + model.AuthDomain + "\",";
                    }

                    if (line.Contains("databaseURL"))
                    {
                        lines[i] = "databaseURL: \"" + model.DatabaseUrl + "\",";
                    }

                    if (line.Contains("projectId"))
                    {
                        lines[i] = "projectId: \"" + model.ProjectId + "\",";
                    }

                    if (line.Contains("storageBucket"))
                    {
                        lines[i] = "storageBucket: \"" + model.StorageBucket + "\",";
                    }

                    if (line.Contains("messagingSenderId"))
                    {
                        lines[i] = "messagingSenderId: \"" + model.SenderId + "\",";
                    }

                    if (line.Contains("appId"))
                    {
                        lines[i] = "appId: \"" + model.AppId + "\",";
                    }

                    i++;
                }

                System.IO.File.WriteAllLines(jsFilePath, lines);
            }

            Success(_translationService.GetResource("Admin.Configuration.Updated"));
            return await PushNotifications();
        }

        public IActionResult AdminSearch()
        {
            var settings = _settingService.LoadSetting<AdminSearchSettings>();
            var model = new AdminSearchSettingsModel {
                SearchInBlogs = settings.SearchInBlogs,
                SearchInCategories = settings.SearchInCategories,
                SearchInCustomers = settings.SearchInCustomers,
                SearchInCollections = settings.SearchInCollections,
                SearchInNews = settings.SearchInNews,
                SearchInOrders = settings.SearchInOrders,
                SearchInProducts = settings.SearchInProducts,
                SearchInPages = settings.SearchInPages,
                MinSearchTermLength = settings.MinSearchTermLength,
                MaxSearchResultsCount = settings.MaxSearchResultsCount,
                ProductsDisplayOrder = settings.ProductsDisplayOrder,
                CategoriesDisplayOrder = settings.CategoriesDisplayOrder,
                CollectionsDisplayOrder = settings.CollectionsDisplayOrder,
                PagesDisplayOrder = settings.PagesDisplayOrder,
                NewsDisplayOrder = settings.NewsDisplayOrder,
                BlogsDisplayOrder = settings.BlogsDisplayOrder,
                CustomersDisplayOrder = settings.CustomersDisplayOrder,
                OrdersDisplayOrder = settings.OrdersDisplayOrder,
                SearchInMenu = settings.SearchInMenu,
                MenuDisplayOrder = settings.MenuDisplayOrder,
                CategorySizeLimit = settings.CategorySizeLimit,
                BrandSizeLimit = settings.BrandSizeLimit,
                CollectionSizeLimit = settings.CollectionSizeLimit,
                VendorSizeLimit = settings.VendorSizeLimit,
                CustomerGroupSizeLimit = settings.CustomerGroupSizeLimit
            };

            return View(model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AdminSearch(AdminSearchSettingsModel model)
        {
            var settings = _settingService.LoadSetting<AdminSearchSettings>();
            settings.SearchInBlogs = model.SearchInBlogs;
            settings.SearchInCategories = model.SearchInCategories;
            settings.SearchInCustomers = model.SearchInCustomers;
            settings.SearchInCollections = model.SearchInCollections;
            settings.SearchInNews = model.SearchInNews;
            settings.SearchInOrders = model.SearchInOrders;
            settings.SearchInProducts = model.SearchInProducts;
            settings.SearchInPages = model.SearchInPages;
            settings.MinSearchTermLength = model.MinSearchTermLength;
            settings.MaxSearchResultsCount = model.MaxSearchResultsCount;
            settings.ProductsDisplayOrder = model.ProductsDisplayOrder;
            settings.CategoriesDisplayOrder = model.CategoriesDisplayOrder;
            settings.CollectionsDisplayOrder = model.CollectionsDisplayOrder;
            settings.PagesDisplayOrder = model.PagesDisplayOrder;
            settings.NewsDisplayOrder = model.NewsDisplayOrder;
            settings.BlogsDisplayOrder = model.BlogsDisplayOrder;
            settings.CustomersDisplayOrder = model.CustomersDisplayOrder;
            settings.OrdersDisplayOrder = model.OrdersDisplayOrder;
            settings.SearchInMenu = model.SearchInMenu;
            settings.MenuDisplayOrder = model.MenuDisplayOrder;
            settings.CategorySizeLimit = model.CategorySizeLimit;
            settings.BrandSizeLimit = model.BrandSizeLimit;
            settings.CollectionSizeLimit = model.CollectionSizeLimit;
            settings.VendorSizeLimit = model.VendorSizeLimit;
            settings.CustomerGroupSizeLimit = model.CustomerGroupSizeLimit;

            await _settingService.SaveSetting(settings);

            //now clear cache
            await ClearCache();

            Success(_translationService.GetResource("Admin.Configuration.Updated"));
            return AdminSearch();
        }

        #region System settings

        public async Task<IActionResult> SystemSetting()
        {
            var settings = _settingService.LoadSetting<SystemSettings>();

            var model = new SystemSettingsModel();

            //order ident
            model.OrderIdent = await _mediator.Send(new MaxOrderNumberCommand());
            //system settings
            model.DaysToCancelUnpaidOrder = settings.DaysToCancelUnpaidOrder;
            model.DeleteGuestTaskOlderThanMinutes = settings.DeleteGuestTaskOlderThanMinutes;

            //area admin settings
            var adminsettings = _settingService.LoadSetting<AdminAreaSettings>();
            model.DefaultGridPageSize = adminsettings.DefaultGridPageSize;
            model.GridPageSizes = adminsettings.GridPageSizes;
            model.UseIsoDateTimeConverterInJson = adminsettings.UseIsoDateTimeConverterInJson;

            //language settings 
            var langsettings = _settingService.LoadSetting<LanguageSettings>();
            model.IgnoreRtlPropertyForAdminArea = langsettings.IgnoreRtlPropertyForAdminArea;
            model.AutomaticallyDetectLanguage = langsettings.AutomaticallyDetectLanguage;
            model.DefaultAdminLanguageId = langsettings.DefaultAdminLanguageId;

            //others
            var docsettings = _settingService.LoadSetting<DocumentSettings>();
            model.DocumentPageSizeSettings = docsettings.PageSize;

            return View(model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SystemSetting(SystemSettingsModel model)
        {
            //system 
            var settings = _settingService.LoadSetting<SystemSettings>();
            settings.DaysToCancelUnpaidOrder = model.DaysToCancelUnpaidOrder;
            settings.DeleteGuestTaskOlderThanMinutes = model.DeleteGuestTaskOlderThanMinutes;
            await _settingService.SaveSetting(settings);


            //order ident
            if (model.OrderIdent.HasValue && model.OrderIdent.Value > 0)
            {
                await _mediator.Send(new MaxOrderNumberCommand() { OrderNumber = model.OrderIdent });
            }

            //admin area
            var adminAreaSettings = _settingService.LoadSetting<AdminAreaSettings>();
            adminAreaSettings.DefaultGridPageSize = model.DefaultGridPageSize;
            adminAreaSettings.GridPageSizes = model.GridPageSizes;
            adminAreaSettings.UseIsoDateTimeConverterInJson = model.UseIsoDateTimeConverterInJson;

            await _settingService.SaveSetting(adminAreaSettings);


            //language settings 
            var langsettings = _settingService.LoadSetting<LanguageSettings>();
            langsettings.IgnoreRtlPropertyForAdminArea = model.IgnoreRtlPropertyForAdminArea;
            langsettings.AutomaticallyDetectLanguage = model.AutomaticallyDetectLanguage;
            langsettings.DefaultAdminLanguageId = model.DefaultAdminLanguageId;
            await _settingService.SaveSetting(langsettings);

            //doc settings 
            var docsettings = _settingService.LoadSetting<DocumentSettings>();
            docsettings.PageSize = model.DocumentPageSizeSettings;
            await _settingService.SaveSetting(docsettings);

            //now clear cache
            await ClearCache();

            Success(_translationService.GetResource("Admin.Configuration.Updated"));

            return await SystemSetting();
        }
        #endregion

        #endregion
    }
}
