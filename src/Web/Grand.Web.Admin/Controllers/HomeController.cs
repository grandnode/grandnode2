using Grand.Business.Authentication.Interfaces;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Queries.Models;
using Grand.Business.System.Interfaces.Reports;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Home;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    public partial class HomeController : BaseAdminController
    {
        #region Fields

        private readonly ITranslationService _translationService;
        private readonly GoogleAnalyticsSettings _googleAnalyticsSettings;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly IOrderReportService _orderReportService;
        private readonly IProductsReportService _productsReportService;
        private readonly ILogger _logger;
        private readonly IGrandAuthenticationService _authenticationService;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        public HomeController(
            ITranslationService translationService,
            GoogleAnalyticsSettings googleAnalyticsSettings,
            IWorkContext workContext,
            IGroupService groupService,
            IOrderReportService orderReportService,
            IProductsReportService productsReportService,
            ILogger logger,
            IGrandAuthenticationService authenticationService,
            IMediator mediator)
        {
            _translationService = translationService;
            _googleAnalyticsSettings = googleAnalyticsSettings;
            _workContext = workContext;
            _groupService = groupService;
            _orderReportService = orderReportService;
            _productsReportService = productsReportService;
            _logger = logger;
            _authenticationService = authenticationService;
            _mediator = mediator;
        }

        #endregion

        #region Utiliti

        private async Task<DashboardActivityModel> PrepareActivityModel()
        {
            var model = new DashboardActivityModel();
            string vendorId = "";
            if (_workContext.CurrentVendor != null)
                vendorId = _workContext.CurrentVendor.Id;

            var storeId = string.Empty;
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            model.OrdersPending = (await _orderReportService.GetOrderAverageReportLine(storeId: storeId, os: (int)OrderStatusSystem.Pending)).CountOrders;
            model.AbandonedCarts = (await _mediator.Send(new GetCustomerQuery() { StoreId = storeId, LoadOnlyWithShoppingCart = true })).Count();

            var lowStockProducts = await _productsReportService.LowStockProducts(vendorId, storeId);
            model.LowStockProducts = lowStockProducts.products.Count + lowStockProducts.combinations.Count;

            model.MerchandiseReturns = await _mediator.Send(new GetMerchandiseReturnCountQuery() { RequestStatusId = 0, StoreId = storeId });
            model.TodayRegisteredCustomers =
                (await _mediator.Send(new GetCustomerQuery() {
                    StoreId = storeId,
                    CustomerGroupIds = new string[] { (await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered)).Id },
                    CreatedFromUtc = DateTime.UtcNow.Date
                })).Count();
            return model;

        }

        #endregion

        #region Methods

        public async Task<IActionResult> Index()
        {
            var model = new DashboardModel {
                IsLoggedInAsVendor = _workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer)
            };
            if (string.IsNullOrEmpty(_googleAnalyticsSettings.gaprivateKey) ||
                string.IsNullOrEmpty(_googleAnalyticsSettings.gaserviceAccountEmail) ||
                string.IsNullOrEmpty(_googleAnalyticsSettings.gaviewID))
                model.HideReportGA = true;

            return View(model);
        }

        public async Task<IActionResult> Statistics()
        {
            var model = new DashboardModel {
                IsLoggedInAsVendor = _workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer)
            };
            return View(model);
        }

        public async Task<IActionResult> DashboardActivity()
        {
            var model = await PrepareActivityModel();
            return PartialView(model);
        }

        public async Task<IActionResult> SetLanguage(string langid, [FromServices] ILanguageService languageService, string returnUrl = "")
        {
            var language = await languageService.GetLanguageById(langid);
            if (language != null)
            {
                await _workContext.SetWorkingLanguage(language);
            }

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Action("Index", "Home", new { area = Constants.AreaAdmin });
            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                return RedirectToAction("Index", "Home", new { area = Constants.AreaAdmin });
            return Redirect(returnUrl);
        }

        [AcceptVerbs("Get")]
        public async Task<IActionResult> GetStatesByCountryId([FromServices] ICountryService countryService,
            string countryId, bool? addSelectStateItem, bool? addAsterisk)
        {
            // This action method gets called via an ajax request
            if (String.IsNullOrEmpty(countryId))
                return Json(new List<dynamic>() { new { id = "", name = _translationService.GetResource("Address.SelectState") } });

            var country = await countryService.GetCountryById(countryId);
            var states = country != null ? country.StateProvinces.ToList() : new List<StateProvince>();
            var result = (from s in states
                          select new { id = s.Id, name = s.Name }).ToList();
            if (addAsterisk.HasValue && addAsterisk.Value)
            {
                //asterisk
                result.Insert(0, new { id = "", name = "*" });
            }
            else
            {
                if (country == null)
                {
                    //country is not selected ("choose country" item)
                    if (addSelectStateItem.HasValue && addSelectStateItem.Value)
                    {
                        result.Insert(0, new { id = "", name = _translationService.GetResource("Admin.Address.SelectState") });
                    }
                }
                else
                {
                    //some country is selected
                    if (result.Any())
                    {
                        //country has some states
                        if (addSelectStateItem.HasValue && addSelectStateItem.Value)
                        {
                            result.Insert(0, new { id = "", name = _translationService.GetResource("Admin.Address.SelectState") });
                        }
                    }
                }
            }
            return Json(result);
        }

        public async Task<IActionResult> AccessDenied(string pageUrl)
        {
            var currentCustomer = _workContext.CurrentCustomer;
            if (currentCustomer == null || await _groupService.IsGuest(currentCustomer))
            {
                _logger.Information(string.Format("Access denied to anonymous request on {0}", pageUrl));
                return View();
            }

            _logger.Information(string.Format("Access denied to user #{0} '{1}' on {2}", currentCustomer.Email, currentCustomer.Email, pageUrl));


            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _authenticationService.SignOut();
            return RedirectToRoute("AdminLogin");
        }

        #endregion
    }
}
