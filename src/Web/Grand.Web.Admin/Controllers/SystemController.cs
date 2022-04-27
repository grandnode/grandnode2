using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Business.Core.Interfaces.System.MachineNameProvider;
using Grand.Domain.Directory;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Roslyn;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.System)]
    public partial class SystemController : BaseAdminController
    {
        #region Fields

        private readonly IPaymentService _paymentService;
        private readonly IShippingService _shippingService;
        private readonly ICurrencyService _currencyService;
        private readonly IMeasureService _measureService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;
        private readonly IMachineNameProvider _machineNameProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger _logger;

        private readonly CurrencySettings _currencySettings;
        private readonly MeasureSettings _measureSettings;
        private readonly ExtensionsConfig _extConfig;
        #endregion

        #region Constructors

        public SystemController(IPaymentService paymentService,
            IShippingService shippingService,
            ICurrencyService currencyService,
            IMeasureService measureService,
            IDateTimeService dateTimeService,
            IWorkContext workContext,
            ITranslationService translationService,
            IMachineNameProvider machineNameProvider,
            IHostApplicationLifetime applicationLifetime,
            IWebHostEnvironment webHostEnvironment,
            ILogger logger,
            CurrencySettings currencySettings,
            MeasureSettings measureSettings,
            ExtensionsConfig extConfig)
        {
            _paymentService = paymentService;
            _shippingService = shippingService;
            _currencyService = currencyService;
            _measureService = measureService;
            _currencySettings = currencySettings;
            _measureSettings = measureSettings;
            _dateTimeService = dateTimeService;
            _workContext = workContext;
            _translationService = translationService;
            _applicationLifetime = applicationLifetime;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _extConfig = extConfig;
            _machineNameProvider = machineNameProvider;
        }

        #endregion

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
            model.WebRootPath = _webHostEnvironment.WebRootPath;
            model.ContentRootPath = _webHostEnvironment.ContentRootPath;
            model.EnvironmentName = _webHostEnvironment.EnvironmentName;
            model.ServerTimeZone = TimeZoneInfo.Local.StandardName;
            model.ServerLocalTime = DateTime.Now;
            model.ApplicationTime = _dateTimeService.ConvertToUserTime(DateTime.UtcNow, TimeZoneInfo.Utc, _dateTimeService.CurrentTimeZone);
            model.UtcTime = DateTime.UtcNow;
            foreach (var header in HttpContext.Request.Headers)
            {
                model.ServerVariables.Add(new SystemInfoModel.ServerVariableModel {
                    Name = header.Key,
                    Value = header.Value
                });
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().ToList().OrderBy(x => x.FullName))
            {
                model.LoadedAssemblies.Add(new SystemInfoModel.LoadedAssembly {
                    FullName = assembly.FullName,
                });
            }

            //current host
            var currenthostName = _workContext.CurrentHost.HostName;
            if (!string.IsNullOrEmpty(currenthostName) && (currenthostName.Equals(HttpContext.Request.Host.Value, StringComparison.OrdinalIgnoreCase)))
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.URL.Match")
                });
            else
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Warning,
                    Text = string.Format(_translationService.GetResource("Admin.System.Warnings.URL.NoMatch"), currenthostName, HttpContext.Request.Host.Host)
                });


            //primary exchange rate currency
            var perCurrency = await _currencyService.GetCurrencyById(_currencySettings.PrimaryExchangeRateCurrencyId);
            if (perCurrency != null)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.ExchangeCurrency.Set"),
                });
                if (perCurrency.Rate != 1)
                {
                    model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                        Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                        Text = _translationService.GetResource("Admin.System.Warnings.ExchangeCurrency.Rate1")
                    });
                }
            }
            else
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.ExchangeCurrency.NotSet")
                });
            }

            //primary store currency
            var pscCurrency = await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            if (pscCurrency != null)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.PrimaryCurrency.Set"),
                });
            }
            else
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.PrimaryCurrency.NotSet")
                });
            }


            //base measure weight
            var bWeight = await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId);
            if (bWeight != null)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.DefaultWeight.Set"),
                });

                if (bWeight.Ratio != 1)
                {
                    model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                        Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                        Text = _translationService.GetResource("Admin.System.Warnings.DefaultWeight.Ratio1")
                    });
                }
            }
            else
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.DefaultWeight.NotSet")
                });
            }


            //base dimension weight
            var bDimension = await _measureService.GetMeasureDimensionById(_measureSettings.BaseDimensionId);
            if (bDimension != null)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.DefaultDimension.Set"),
                });

                if (bDimension.Ratio != 1)
                {
                    model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                        Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                        Text = _translationService.GetResource("Admin.System.Warnings.DefaultDimension.Ratio1")
                    });
                }
            }
            else
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.DefaultDimension.NotSet")
                });
            }

            //shipping rate coputation methods
            var srcMethods = await _shippingService.LoadActiveShippingRateCalculationProviders();
            if (srcMethods.Count == 0)
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.Shipping.NoComputationMethods")
                });
            if (srcMethods.Count(x => x.ShippingRateCalculationType == ShippingRateCalculationType.Off) > 1)
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Warning,
                    Text = _translationService.GetResource("Admin.System.Warnings.Shipping.OnlyOneOffline")
                });

            //payment methods
            if ((await _paymentService.LoadActivePaymentMethods()).Any())
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Pass,
                    Text = _translationService.GetResource("Admin.System.Warnings.PaymentMethods.OK")
                });
            else
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Fail,
                    Text = _translationService.GetResource("Admin.System.Warnings.PaymentMethods.NoActive")
                });

            //performance settings
            if (CommonHelper.IgnoreStoreLimitations)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Warning,
                    Text = _translationService.GetResource("Admin.System.Warnings.Performance.IgnoreStoreLimitations")
                });
            }
            if (CommonHelper.IgnoreAcl)
            {
                model.SystemWarnings.Add(new SystemInfoModel.SystemWarningModel {
                    Level = SystemInfoModel.SystemWarningModel.SystemWarningLevel.Warning,
                    Text = _translationService.GetResource("Admin.System.Warnings.Performance.IgnoreAcl")
                });
            }

            return View(model);
        }


        public async Task<IActionResult> ClearCache(string returnUrl, [FromServices] ICacheBase cacheBase)
        {
            _ = _logger.InsertLog(Domain.Logging.LogLevel.Information, $"Clear cache has been done by the user: {_workContext.CurrentCustomer.Email}");

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
            _ = _logger.InsertLog(Domain.Logging.LogLevel.Information, $"The application has been restarted by the user {_workContext.CurrentCustomer.Email}");

            //stop application
            _applicationLifetime.StopApplication();

            //home page
            if (string.IsNullOrEmpty(returnUrl))
                return RedirectToAction("Index", "Home", new { area = Constants.AreaAdmin });
            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                return RedirectToAction("Index", "Home", new { area = Constants.AreaAdmin });
            return Redirect(returnUrl);
        }

        public IActionResult Roslyn()
        {
            return View(_extConfig.UseRoslynScripts);
        }

        [HttpPost]
        public IActionResult Roslyn(DataSourceRequest command)
        {
            var scripts = RoslynCompiler.ReferencedScripts != null ? RoslynCompiler.ReferencedScripts.ToList() : new List<ResultCompiler>();

            var gridModel = new DataSourceResult {
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

        #endregion

    }
}