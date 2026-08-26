using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.MerchandiseReturns)]
[AutoValidateAntiforgeryToken]
public abstract class BaseMerchandiseReturnController(
    IMerchandiseReturnViewModelService merchandiseReturnViewModelService,
    ITranslationService translationService,
    IMerchandiseReturnService merchandiseReturnService,
    IOrderService orderService,
    IAdminDataScope<MerchandiseReturn> scope)
    : BaseController
{
    // Exposed for host subclasses: primary-constructor parameters aren't visible to derived classes
    // by name in C#.
    protected IMerchandiseReturnViewModelService MerchandiseReturnViewModelService => merchandiseReturnViewModelService;
    protected ITranslationService TranslationService => translationService;
    protected IMerchandiseReturnService MerchandiseReturnService => merchandiseReturnService;
    protected IOrderService OrderService => orderService;
    protected IAdminDataScope<MerchandiseReturn> Scope => scope;

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        var model = merchandiseReturnViewModelService.PrepareReturnRequestListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, MerchandiseReturnListModel model)
    {
        // Vendor needs no analogous model.StoreId forcing here (no store concept) - its
        // vendor-scoping happens inside the shared service call via scope.DefaultVendorId (Task 4).
        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;

        var (merchandiseReturnModels, totalCount) =
            await merchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, command.Page, command.PageSize);

        var gridModel = new DataSourceResult {
            Data = merchandiseReturnModels,
            Total = totalCount
        };
        return Json(gridModel);
    }

    #endregion
}
