using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Inventory;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Admin.Extensions;

namespace Grand.Web.Admin.Controllers;

[Area(Constants.AreaAdmin)]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.InventoryDocuments)]
public class InventoryDocumentController(
    IInventoryDocumentService inventoryDocumentService,
    IInventoryDocumentViewModelService inventoryDocumentViewModelService,
    IWarehouseService warehouseService,
    IMerchandiseReturnService merchandiseReturnService,
    IRequestTranslationService translationService,
    IContextAccessor contextAccessor,
    IAdminDataScope<Warehouse> warehouseScope,
    IAdminDataScope<MerchandiseReturn> merchandiseReturnScope)
    : BaseInventoryDocumentController(inventoryDocumentService, inventoryDocumentViewModelService, warehouseService,
        merchandiseReturnService, translationService, contextAccessor, warehouseScope, merchandiseReturnScope)
{
    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public Task<IActionResult> List() => ListCore();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public Task<IActionResult> ListData(DataSourceRequest command, InventoryDocumentListModel model) =>
        ListDataCore(command, model);

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public Task<IActionResult> Create(InventoryDocumentType type) => CreateGetCore(type);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public Task<IActionResult> Create(InventoryDocumentModel model) => CreatePostCore(model);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public Task<IActionResult> CreateReturnReceipt(string merchandiseReturnId) =>
        CreateReturnReceiptCore(merchandiseReturnId);

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public Task<IActionResult> Edit(string id) => EditGetCore(id);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> Edit(InventoryDocumentModel model, bool continueEditing) =>
        EditPostCore(model, continueEditing);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public Task<IActionResult> Post(string id) => PostCore(id);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public Task<IActionResult> Delete(string id) => DeleteCore(id);
}
