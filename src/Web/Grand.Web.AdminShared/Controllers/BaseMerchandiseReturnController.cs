using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Extensions;
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

    #region GoToId / Products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GoToId(MerchandiseReturnListModel model)
    {
        if (model.GoDirectlyToId == null)
            return RedirectToAction("List");

        int.TryParse(model.GoDirectlyToId, out var id);

        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null || !await Scope.HasAccess(merchandiseReturn))
            return RedirectToAction("List");

        return RedirectToAction("Edit", new { id = merchandiseReturn.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductsForMerchandiseReturn(string merchandiseReturnId, DataSourceRequest command)
    {
        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null || !await Scope.HasAccess(merchandiseReturn))
            return ErrorForKendoGridJson("Merchandise return not found");

        var items = await MerchandiseReturnViewModelService.PrepareMerchandiseReturnItemModel(merchandiseReturnId);
        var gridModel = new DataSourceResult {
            Data = items,
            Total = items.Count
        };
        return Json(gridModel);
    }

    #endregion

    #region Edit

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null) return RedirectToAction("List");
        if (!await Scope.CanView(merchandiseReturn)) return RedirectToAction("List");

        var model = new MerchandiseReturnModel();
        await MerchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, merchandiseReturn, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(MerchandiseReturnModel model, bool continueEditing,
        [FromServices] Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeService addressAttributeService,
        [FromServices] Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeParser addressAttributeParser,
        [FromServices] Grand.Domain.Orders.OrderSettings orderSettings)
    {
        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(model.Id);
        if (merchandiseReturn == null) return RedirectToAction("List");
        if (!await Scope.HasAccess(merchandiseReturn)) return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            var customAddressAttributes = new List<Grand.Domain.Common.CustomAttribute>();
            if (orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
                customAddressAttributes = await model.PickupAddress.ParseCustomAddressAttributes(
                    addressAttributeParser, addressAttributeService);

            merchandiseReturn = await MerchandiseReturnViewModelService.UpdateMerchandiseReturnModel(
                merchandiseReturn, model, customAddressAttributes);

            Success(TranslationService.GetResource($"{Scope.ResourceKeyPrefix}.Orders.MerchandiseReturns.Updated"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = merchandiseReturn.Id })
                : RedirectToAction("List");
        }

        await MerchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, merchandiseReturn, false);
        return View(model);
    }

    #endregion
}
