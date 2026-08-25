using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Orders)]
[AutoValidateAntiforgeryToken]
public abstract class BaseOrderController(
    IOrderViewModelService orderViewModelService,
    IOrderService orderService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IAdminDataScope<Order> scope)
    : BaseController
{
    // Exposed for BaseOrderManagementController (primary-constructor parameters aren't visible to
    // derived classes by name in C#).
    protected IOrderViewModelService OrderViewModelService => orderViewModelService;
    protected IOrderService OrderService => orderService;
    protected ITranslationService TranslationService => translationService;
    protected IContextAccessor ContextAccessor => contextAccessor;
    protected IPdfService PdfService => pdfService;
    protected IAdminDataScope<Order> Scope => scope;

    /// <summary>DRY replacement for the ~20x-duplicated
    /// "load order, redirect to List if not found or not authorized" pattern found identically in
    /// both Admin's and Store's original controllers (every action in both files redirects to
    /// "List", never "Edit", on either condition). Not a behavior change — every call site below
    /// still individually returns RedirectToAction("List") exactly as both originals did.</summary>
    protected async Task<(Order order, IActionResult denied)> LoadAuthorizedOrder(string id)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null) return (null, RedirectToAction("List"));
        if (!await scope.HasAccess(order)) return (null, RedirectToAction("List"));
        return (order, null);
    }

    #region Order list

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List(int? orderStatusId = null, int? paymentStatusId = null,
        int? shippingStatusId = null, DateTime? startDate = null, string code = null)
    {
        var model = await orderViewModelService.PrepareOrderListModel(orderStatusId, paymentStatusId,
            shippingStatusId, startDate, scope.DefaultStoreId ?? "", code);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> OrderList(DataSourceRequest command, OrderListModel model)
    {
        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        if (scope.DefaultVendorId is not null) model.VendorId = scope.DefaultVendorId;

        var (orderModels, totalCount) =
            await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);

        var gridModel = new DataSourceResult {
            Data = orderModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    #endregion
}
