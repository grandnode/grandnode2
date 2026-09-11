using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.System.Reports;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Queries.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Models.Home;
using Grand.Mediator;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseHomeControllerWithSetLanguage (ARCH-001 Phase 28).
// GetStatesByCountryId/Logout/SetLanguage live in the shared base; DashboardActivity/ChangeStore
// (Admin-only) and Index/Statistics/AccessDenied (real per-host dashboards/views) stay here.
// BaseHomeControllerWithSetLanguage can't inherit any single host's base controller (it's shared
// across Admin/Store), so this subclass restates its own host's attribute set explicitly - same
// pattern as ProductController/EmailAccountController/PictureController.
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class HomeController : BaseHomeControllerWithSetLanguage
{
    #region Ctor

    public HomeController(
        ICountryService countryService,
        ITranslationService translationService,
        IGrandAuthenticationService authenticationService,
        IContextAccessor contextAccessor,
        IStoreService storeService,
        ICustomerService customerService,
        IGroupService groupService,
        IOrderReportService orderReportService,
        IProductsReportService productsReportService,
        ILogger<HomeController> logger,
        IMediator mediator)
        : base(countryService, translationService, authenticationService, contextAccessor)
    {
        _contextAccessor = contextAccessor;
        _storeService = storeService;
        _customerService = customerService;
        _groupService = groupService;
        _orderReportService = orderReportService;
        _productsReportService = productsReportService;
        _logger = logger;
        _mediator = mediator;
    }

    #endregion

    #region Utiliti

    private async Task<DashboardActivityModel> PrepareActivityModel()
    {
        var model = new DashboardActivityModel();

        var storeId = string.Empty;

        model.OrdersPending =
            (await _orderReportService.GetOrderAverageReportLine(storeId, os: (int)OrderStatusSystem.Pending))
            .CountOrders;
        model.AbandonedCarts = (await _mediator.Send(new GetCustomerQuery
            { StoreId = storeId, LoadOnlyWithShoppingCart = true })).Count();

        var lowStockProducts = await _productsReportService.LowStockProducts(storeId: storeId);
        model.LowStockProducts = lowStockProducts.products.Count + lowStockProducts.combinations.Count;

        model.MerchandiseReturns = await _mediator.Send(new GetMerchandiseReturnCountQuery
            { RequestStatusId = 0, StoreId = storeId });
        model.TodayRegisteredCustomers =
            (await _mediator.Send(new GetCustomerQuery {
                StoreId = storeId,
                CustomerGroupIds = [
                    (await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered)).Id
                ],
                CreatedFromUtc = DateTime.UtcNow.Date
            })).Count();
        return model;
    }

    #endregion

    #region Fields

    private readonly IContextAccessor _contextAccessor;
    private readonly IStoreService _storeService;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly IOrderReportService _orderReportService;
    private readonly IProductsReportService _productsReportService;
    private readonly ILogger<HomeController> _logger;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    protected override string SelectStateResourceKey => "Admin.Address.SelectState";
    protected override string LogoutRouteName => "AdminLogin";
    protected override string AreaName => Constants.AreaAdmin;

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Statistics()
    {
        return View();
    }

    public async Task<IActionResult> DashboardActivity()
    {
        var model = await PrepareActivityModel();
        return PartialView(model);
    }

    public async Task<IActionResult> ChangeStore(string storeid, string returnUrl = "")
    {
        if (storeid != null)
            storeid = storeid.Trim();

        var store = await _storeService.GetStoreById(storeid);
        if (store != null || storeid == "")
            await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                SystemCustomerFieldNames.AdminAreaStoreScopeConfiguration, storeid);
        else
            await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                SystemCustomerFieldNames.AdminAreaStoreScopeConfiguration, "");

        //home page
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = Url.Action("Index", "Home", new { area = Constants.AreaAdmin });

        return Redirect(returnUrl);
    }

    public IActionResult AccessDenied()
    {
        _logger.LogInformation("Access denied");
        return View();
    }

    #endregion
}
