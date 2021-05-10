using Grand.Business.Checkout.Enum;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Business.System.Commands.Models.Common;
using Grand.Business.System.Interfaces.MachineNameProvider;
using Grand.Domain.Data;
using Grand.Domain.Directory;
using Grand.Domain.Media;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Roslyn;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Maintenance)]
    public partial class CommonController : BaseAdminController
    {
        #region Fields

        private readonly IPaymentService _paymentService;
        private readonly IShippingService _shippingService;
        private readonly ICurrencyService _currencyService;
        private readonly IMeasureService _measureService;
        private readonly ICustomerService _customerService;
        private readonly ISlugService _slugService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILanguageService _languageService;
        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;
        private readonly IStoreService _storeService;
        private readonly IMongoDBContext _mongoDBContext;
        private readonly IMachineNameProvider _machineNameProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IMediator _mediator;
        private readonly IMediaFileStore _mediaFileStore;

        private readonly CurrencySettings _currencySettings;
        private readonly MeasureSettings _measureSettings;
        private readonly AppConfig _appConfig;
        #endregion

        #region Constructors

        public CommonController(IPaymentService paymentService,
            IShippingService shippingService,
            ICurrencyService currencyService,
            IMeasureService measureService,
            ICustomerService customerService,
            ISlugService slugService,
            IDateTimeService dateTimeService,
            ILanguageService languageService,
            IWorkContext workContext,
            ITranslationService translationService,
            IStoreService storeService,
            IMongoDBContext mongoDBContext,
            IMachineNameProvider machineNameProvider,
            IHostApplicationLifetime applicationLifetime,
            IMediator mediator,
            IMediaFileStore mediaFileStore,
            CurrencySettings currencySettings,
            MeasureSettings measureSettings,
            AppConfig appConfig)
        {
            _paymentService = paymentService;
            _shippingService = shippingService;
            _currencyService = currencyService;
            _measureService = measureService;
            _customerService = customerService;
            _slugService = slugService;
            _currencySettings = currencySettings;
            _measureSettings = measureSettings;
            _dateTimeService = dateTimeService;
            _languageService = languageService;
            _workContext = workContext;
            _translationService = translationService;
            _storeService = storeService;
            _applicationLifetime = applicationLifetime;
            _appConfig = appConfig;
            _mongoDBContext = mongoDBContext;
            _machineNameProvider = machineNameProvider;
            _mediaFileStore = mediaFileStore;
            _mediator = mediator;
        }

        #endregion

        protected virtual IEnumerable<Dictionary<string, object>> Serialize(List<BsonValue> collection)
        {
            var results = new List<Dictionary<string, object>>();
            var columns = new List<string>();
            var document = collection.FirstOrDefault()?.AsBsonDocument;
            if (document != null)
            {
                foreach (var item in document.Names)
                {
                    columns.Add(item);
                }
                foreach (var row in collection)
                {
                    var myObject = new Dictionary<string, object>();
                    foreach (var col in columns)
                    {
                        myObject.Add(col, row[col].ToString());
                    }
                    results.Add(myObject);
                }
            }
            return results;
        }

        #region Methods

        public async Task<IActionResult> SystemInfo()
        {
            var model = new SystemInfoModel();
            model.GrandVersion = GrandVersion.FullVersion;
            try
            {
                model.OperatingSystem = RuntimeInformation.OSDescription;
            }
            catch (Exception) { }
            try
            {
                model.AspNetInfo = RuntimeEnvironment.GetSystemVersion();
            }
            catch (Exception) { }

            model.MachineName = _machineNameProvider.GetMachineName();

            model.ServerTimeZone = TimeZoneInfo.Local.StandardName;
            model.ServerLocalTime = DateTime.Now;
            model.ApplicationTime = _dateTimeService.ConvertToUserTime(DateTime.UtcNow, TimeZoneInfo.Utc, _dateTimeService.CurrentTimeZone);
            model.UtcTime = DateTime.UtcNow;
            foreach (var header in HttpContext.Request.Headers)
            {
                model.ServerVariables.Add(new SystemInfoModel.ServerVariableModel
                {
                    Name = header.Key,
                    Value = header.Value
                });
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().ToList().OrderBy(x => x.FullName))
            {
                model.LoadedAssemblies.Add(new SystemInfoModel.LoadedAssembly
                {
                    FullName = assembly.FullName,
                });
            }
            //store URL
            var currentStoreUrl = _workContext.CurrentStore.Url;
            if (!String.IsNullOrEmpty(currentStoreUrl) && (currentStoreUrl.Equals(HttpContext.Request.Host.Host, StringComparison.OrdinalIgnoreCase)))
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.URL.Match")
                });
            else
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Warning,
                    Text = string.Format(_translationService.GetResource("Admin.System.Warnings.URL.NoMatch"), currentStoreUrl, HttpContext.Request.Host.Host)
                });


            //primary exchange rate currency
            var perCurrency = await _currencyService.GetCurrencyById(_currencySettings.PrimaryExchangeRateCurrencyId);
            if (perCurrency != null)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.ExchangeCurrency.Set"),
                });
                if (perCurrency.Rate != 1)
                {
                    model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                    {
                        Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                        Text = _translationService.GetResource("Admin.System.Warnings.ExchangeCurrency.Rate1")
                    });
                }
            }
            else
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.ExchangeCurrency.NotSet")
                });
            }

            //primary store currency
            var pscCurrency = await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            if (pscCurrency != null)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.PrimaryCurrency.Set"),
                });
            }
            else
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.PrimaryCurrency.NotSet")
                });
            }


            //base measure weight
            var bWeight = await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId);
            if (bWeight != null)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.DefaultWeight.Set"),
                });

                if (bWeight.Ratio != 1)
                {
                    model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                    {
                        Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                        Text = _translationService.GetResource("Admin.System.Warnings.DefaultWeight.Ratio1")
                    });
                }
            }
            else
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.DefaultWeight.NotSet")
                });
            }


            //base dimension weight
            var bDimension = await _measureService.GetMeasureDimensionById(_measureSettings.BaseDimensionId);
            if (bDimension != null)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.DefaultDimension.Set"),
                });

                if (bDimension.Ratio != 1)
                {
                    model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                    {
                        Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                        Text = _translationService.GetResource("Admin.System.Warnings.DefaultDimension.Ratio1")
                    });
                }
            }
            else
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.DefaultDimension.NotSet")
                });
            }

            //shipping rate coputation methods
            var srcMethods = await _shippingService.LoadActiveShippingRateCalculationProviders();
            if (srcMethods.Count == 0)
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.Shipping.NoComputationMethods")
                });
            if (srcMethods.Count(x => x.ShippingRateCalculationType == ShippingRateCalculationType.Off) > 1)
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Warning,
                    Text = _translationService.GetResource("Admin.System.Warnings.Shipping.OnlyOneOffline")
                });

            //payment methods
            if ((await _paymentService.LoadActivePaymentMethods()).Any())
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.PaymentMethods.OK")
                });
            else
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.PaymentMethods.NoActive")
                });

            //performance settings
            if (!CommonHelper.IgnoreStoreLimitations && (await _storeService.GetAllStores()).Count == 1)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Warning,
                    Text = _translationService.GetResource("Admin.System.Warnings.Performance.IgnoreStoreLimitations")
                });
            }
            if (!CommonHelper.IgnoreAcl)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel
                {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Warning,
                    Text = _translationService.GetResource("Admin.System.Warnings.Performance.IgnoreAcl")
                });
            }

            return View(model);
        }

        public IActionResult Maintenance()
        {
            var model = new MaintenanceModel();
            model.DeleteGuests.EndDate = DateTime.UtcNow.AddDays(-7);
            model.DeleteGuests.OnlyWithoutShoppingCart = true;
            model.DeleteAbandonedCarts.OlderThan = DateTime.UtcNow.AddDays(-182);
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> MaintenanceDeleteGuests(MaintenanceModel model)
        {
            DateTime? startDateValue = (model.DeleteGuests.StartDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.DeleteGuests.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.DeleteGuests.EndDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.DeleteGuests.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            model.DeleteGuests.NumberOfDeletedCustomers = await _customerService.DeleteGuestCustomers(startDateValue, endDateValue, model.DeleteGuests.OnlyWithoutShoppingCart);

            return View("Maintenance", model);
        }
        [HttpPost]
        public async Task<IActionResult> MaintenanceClearMostViewed(MaintenanceModel model)
        {
            await _mediator.Send(new ClearMostViewedCommand());
            return View("Maintenance", model);
        }
        [HttpPost]
        public IActionResult MaintenanceDeleteFiles(MaintenanceModel model)
        {
            //TO DO
            model.DeleteExportedFiles.NumberOfDeletedFiles = 0;
            return View("Maintenance", model);
        }


        [HttpPost]
        public async Task<IActionResult> MaintenanceDeleteActivitylog(MaintenanceModel model)
        {
            await _mediator.Send(new DeleteActivitylogCommand());
            model.DeleteActivityLog = true;
            return View("Maintenance", model);
        }

        [HttpPost]
        public async Task<IActionResult> MaintenanceConvertPicture([FromServices] IPictureService pictureService, [FromServices] MediaSettings mediaSettings, [FromServices] ILogger logger)
        {
            var model = new MaintenanceModel();
            model.ConvertedPictureModel.NumberOfConvertItems = 0;
            if (mediaSettings.StoreInDb)
            {
                var pictures = pictureService.GetPictures();
                foreach (var picture in pictures)
                {
                    if (!picture.MimeType.Contains("webp"))
                    {
                        try
                        {
                            using var image = SKBitmap.Decode(picture.PictureBinary);
                            SKData d = SKImage.FromBitmap(image).Encode(SKEncodedImageFormat.Webp, 100);
                            await pictureService.UpdatePicture(picture.Id, d.ToArray(), "image/webp", picture.SeoFilename, picture.AltAttribute, picture.TitleAttribute, true, false);
                            model.ConvertedPictureModel.NumberOfConvertItems += 1;
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Error on converting picture with id {picture.Id} to webp format", ex);
                        }
                    }
                }
            }
            return View("Maintenance", model);
        }

        public async Task<IActionResult> ClearCache(string returnUrl, [FromServices] ICacheBase cacheBase)
        {
            await cacheBase.Clear();

            //home page
            if (string.IsNullOrEmpty(returnUrl))
                return RedirectToAction("Index", "Home", new { area = Constants.AreaAdmin });
            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                return RedirectToAction("Index", "Home", new { area = Constants.AreaAdmin });
            return Redirect(returnUrl);
        }


        public IActionResult RestartApplication(string returnUrl = "")
        {
            //stop application
            _applicationLifetime.StopApplication();

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                return RedirectToAction("Index", "Home", new { area = Constants.AreaAdmin });
            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                return RedirectToAction("Index", "Home", new { area = Constants.AreaAdmin });
            return Redirect(returnUrl);
        }

        public IActionResult Roslyn()
        {
            return View(_appConfig.UseRoslynScripts);
        }

        [HttpPost]
        public IActionResult Roslyn(DataSourceRequest command)
        {
            var scripts = RoslynCompiler.ReferencedScripts != null ? RoslynCompiler.ReferencedScripts.ToList() : new List<ResultCompiler>();

            var gridModel = new DataSourceResult
            {
                Data = scripts.Select(x =>
                {
                    return new
                    {
                        FileName = x.OriginalFile,
                        IsCompiled = x.IsCompiled,
                        Errors = string.Join(",", x.ErrorInfo)
                    };
                }),
                Total = scripts.Count
            };
            return Json(gridModel);
        }

        public IActionResult QueryEditor()
        {
            var model = new QueryEditor();
            return View(model);
        }

        [HttpPost]
        public IActionResult QueryEditor(string query)
        {
            //https://docs.mongodb.com/manual/reference/command/
            if (string.IsNullOrEmpty(query))
                return ErrorForKendoGridJson("Empty query");
            try
            {
                var result = _mongoDBContext.RunCommand<BsonDocument>(query);
                var ok = result.Where(x => x.Name == "ok").FirstOrDefault().Value.ToBoolean();
                var gridModel = new DataSourceResult();
                if (result.Where(x => x.Name == "cursor").ToList().Any())
                {
                    var resultCollection = result["cursor"]["firstBatch"].AsBsonArray.ToList();
                    var response = Serialize(resultCollection);
                    gridModel = new DataSourceResult
                    {
                        Data = response,
                        Total = response.Count()
                    };
                }
                else if (result.Where(x => x.Name == "n").ToList().Any())
                {
                    List<dynamic> n = new List<dynamic>();
                    var number = result["n"].ToInt64();
                    n.Add(new { Number = number });
                    gridModel = new DataSourceResult
                    {
                        Data = n
                    };
                }
                return Json(gridModel);
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult RunScript(string query)
        {
            if (string.IsNullOrEmpty(query))
                return Json(new { Result = false, Message = "Empty query!" });

            try
            {
                var bscript = new BsonJavaScript(query);
                var operation = new EvalOperation(_mongoDBContext.Database().DatabaseNamespace, bscript, null);
                var writeBinding = new WritableServerBinding(_mongoDBContext.Database().Client.Cluster, NoCoreSession.NewHandle());
                var result = operation.Execute(writeBinding, CancellationToken.None);
                var xx = result["_ns"];
                return Json(new { Result = true, Message = result.ToString() });
            }
            catch (Exception ex)
            {
                return Json(new { Result = false, Message = ex.Message });
            }
        }
        public IActionResult SeNames()
        {
            var model = new UrlEntityListModel();
            //"Active" property
            //0 - all (according to "IsActive" parameter)
            //1 - active only
            //2 - inactive only
            model.AvailableActiveOptions.Add(new SelectListItem { Text = _translationService.GetResource("admin.configuration.senames.Search.All"), Value = "0" });
            model.AvailableActiveOptions.Add(new SelectListItem { Text = _translationService.GetResource("admin.configuration.senames.Search.ActiveOnly"), Value = "1" });
            model.AvailableActiveOptions.Add(new SelectListItem { Text = _translationService.GetResource("admin.configuration.senames.Search.InActiveOnly"), Value = "2" });

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SeNames(DataSourceRequest command, UrlEntityListModel model)
        {
            bool? active = null;
            switch (model.SearchActiveId)
            {
                case 1:
                    active = true;
                    break;
                case 2:
                    active = false;
                    break;
                default:
                    break;
            }
            var entityUrls = await _slugService.GetAllEntityUrl(model.SeName, active, command.Page - 1, command.PageSize);
            var items = new List<UrlEntityModel>();
            foreach (var x in entityUrls)
            {
                //language
                string languageName;
                if (String.IsNullOrEmpty(x.LanguageId))
                {
                    languageName = _translationService.GetResource("admin.configuration.senames.Language.Standard");
                }
                else
                {
                    var language = await _languageService.GetLanguageById(x.LanguageId);
                    languageName = language != null ? language.Name : "Unknown";
                }

                //details URL
                string detailsUrl = "";
                var entityName = x.EntityName != null ? x.EntityName.ToLowerInvariant() : "";
                switch (entityName)
                {
                    case "blogpost":
                        detailsUrl = Url.Action("Edit", "Blog", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "category":
                        detailsUrl = Url.Action("Edit", "Category", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "collection":
                        detailsUrl = Url.Action("Edit", "Collection", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "product":
                        detailsUrl = Url.Action("Edit", "Product", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "newsitem":
                        detailsUrl = Url.Action("Edit", "News", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "page":
                        detailsUrl = Url.Action("Edit", "Page", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "vendor":
                        detailsUrl = Url.Action("Edit", "Vendor", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "course":
                        detailsUrl = Url.Action("Edit", "Course", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "knowledgebasecategory":
                        detailsUrl = Url.Action("EditCategory", "Knowledgebase", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    case "knowledgebasearticle":
                        detailsUrl = Url.Action("EditArticle", "Knowledgebase", new { id = x.EntityId, area = Constants.AreaAdmin });
                        break;
                    default:
                        break;
                }

                items.Add(new UrlEntityModel
                {
                    Id = x.Id,
                    Name = x.Slug,
                    EntityId = x.EntityId,
                    EntityName = x.EntityName,
                    IsActive = x.IsActive,
                    Language = languageName,
                    DetailsUrl = detailsUrl
                });

            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = entityUrls.TotalCount
            };
            return Json(gridModel);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteSelectedSeNames(ICollection<string> selectedIds)
        {
            if (selectedIds != null)
            {
                var entityUrls = new List<EntityUrl>();
                foreach (var id in selectedIds)
                {
                    var entityUrl = await _slugService.GetEntityUrlById(id);
                    if (entityUrl != null)
                        entityUrls.Add(entityUrl);
                }
                foreach (var entityUrl in entityUrls)
                    await _slugService.DeleteEntityUrl(entityUrl);
            }

            return Json(new { Result = true });
        }


        #endregion

        #region Custom css/js/robots.txt

        public async Task<IActionResult> CustomCss()
        {
            var model = new Editor();
            var pathFile = _mediaFileStore.Combine("assets", "custom", "style.css");
            var file = await _mediaFileStore.GetFileInfo(pathFile);
            if (file != null)
            {
                model.Content = await _mediaFileStore.ReadAllText(pathFile);
            }

            if (string.IsNullOrEmpty(model.Content))
                model.Content = "/* my custom style */";

            return View(model);
        }

        public async Task<IActionResult> CustomJs()
        {
            var model = new Editor();
            var pathFile = _mediaFileStore.Combine("assets", "custom", "script.js");
            var file = await _mediaFileStore.GetFileInfo(pathFile);
            if (file != null)
            {
                model.Content = await _mediaFileStore.ReadAllText(pathFile);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEditor(string content = "", bool css = true)
        {
            try
            {
                var pathFile = _mediaFileStore.Combine("assets", "custom", css ? "style.css" : "script.js");
                await _mediaFileStore.WriteAllText(pathFile, content);

                return Json(_translationService.GetResource("Admin.Common.Content.Saved"));
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        public async Task<IActionResult> AdditionsRobotsTxt()
        {
            var model = new Editor();
            var pathFile = _mediaFileStore.Combine("robots.additions.txt");
            var file = await _mediaFileStore.GetFileInfo(pathFile);
            if (file != null)
            {
                model.Content = await _mediaFileStore.ReadAllText(pathFile);
            }

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> SaveRobotsTxt(string content = "")
        {
            try
            {
                var pathFile = _mediaFileStore.Combine("robots.additions.txt");
                await _mediaFileStore.WriteAllText(pathFile, content);

                return Json(_translationService.GetResource("Admin.Common.Content.Saved"));
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }



        #endregion
    }
}