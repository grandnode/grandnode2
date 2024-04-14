using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Vendor.Extensions;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Models.MerchandiseReturn;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

[PermissionAuthorize(PermissionSystemName.MerchandiseReturns)]
public class MerchandiseReturnController : BaseVendorController
{
    #region Constructors

    public MerchandiseReturnController(
        IMerchandiseReturnViewModelService merchandiseReturnViewModelService,
        ITranslationService translationService,
        IMerchandiseReturnService merchandiseReturnService,
        IWorkContext workContext)
    {
        _merchandiseReturnViewModelService = merchandiseReturnViewModelService;
        _translationService = translationService;
        _merchandiseReturnService = merchandiseReturnService;
        _workContext = workContext;
    }

    #endregion

    #region Fields

    private readonly IMerchandiseReturnViewModelService _merchandiseReturnViewModelService;
    private readonly ITranslationService _translationService;
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IWorkContext _workContext;

    #endregion Fields

    #region Methods

    //list
    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        var model = _merchandiseReturnViewModelService.PrepareReturnRequestListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, MerchandiseReturnListModel model)
    {
        var merchandiseReturnModels =
            await _merchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, command.Page,
                command.PageSize);
        var gridModel = new DataSourceResult {
            Data = merchandiseReturnModels.merchandiseReturnModels,
            Total = merchandiseReturnModels.totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GoToId(MerchandiseReturnListModel model)
    {
        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(model.GoDirectlyToId);
        if (merchandiseReturn == null || merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
            //not found
            return RedirectToAction("List", "MerchandiseReturn");

        return RedirectToAction("Edit", "MerchandiseReturn", new { id = merchandiseReturn.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductsForMerchandiseReturn(string merchandiseReturnId)
    {
        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null || merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
            return ErrorForKendoGridJson("Merchandise return not found");

        var items = await _merchandiseReturnViewModelService.PrepareMerchandiseReturnItemModel(merchandiseReturnId);
        var gridModel = new DataSourceResult {
            Data = items,
            Total = items.Count
        };

        return Json(gridModel);
    }

    //edit
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null || merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
            //No merchandise return found with the specified id
            return RedirectToAction("List");

        var model = new MerchandiseReturnModel();
        await _merchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, merchandiseReturn, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(MerchandiseReturnModel model, bool continueEditing,
        [FromServices] IAddressAttributeService addressAttributeService,
        [FromServices] IAddressAttributeParser addressAttributeParser,
        [FromServices] OrderSettings orderSettings
    )
    {
        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(model.Id);
        if (merchandiseReturn == null || merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
            //No merchandise return found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            var customAddressAttributes = new List<CustomAttribute>();
            if (orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
                customAddressAttributes =
                    await model.PickupAddress.ParseCustomAddressAttributes(addressAttributeParser,
                        addressAttributeService);
            merchandiseReturn =
                await _merchandiseReturnViewModelService.UpdateMerchandiseReturnModel(merchandiseReturn, model,
                    customAddressAttributes);

            Success(_translationService.GetResource("Vendor.Orders.MerchandiseReturns.Updated"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = merchandiseReturn.Id })
                : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        await _merchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, merchandiseReturn, false);
        return View(model);
    }

    //delete
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null || merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
            //No merchandise return found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _merchandiseReturnViewModelService.DeleteMerchandiseReturn(merchandiseReturn);
            Success(_translationService.GetResource("Vendor.Orders.MerchandiseReturns.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id = merchandiseReturn.Id });
    }

    #endregion

    #region Merchandise return notes

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnNotesSelect(string merchandiseReturnId)
    {
        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null || merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No merchandise return found with the specified id");

        //merchandise return notes
        var merchandiseReturnNoteModels =
            await _merchandiseReturnViewModelService.PrepareMerchandiseReturnNotes(merchandiseReturn);
        var gridModel = new DataSourceResult {
            Data = merchandiseReturnNoteModels,
            Total = merchandiseReturnNoteModels.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> MerchandiseReturnNoteAdd(string merchandiseReturnId, bool displayToCustomer,
        string message)
    {
        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null || merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
            return Json(new { Result = false });

        await _merchandiseReturnViewModelService.InsertMerchandiseReturnNote(merchandiseReturn, displayToCustomer,
            message);

        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnNoteDelete(string id, string merchandiseReturnId)
    {
        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null || merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No merchandise return found with the specified id");

        await _merchandiseReturnViewModelService.DeleteMerchandiseReturnNote(merchandiseReturn, id);

        return new JsonResult("");
    }

    #endregion
}