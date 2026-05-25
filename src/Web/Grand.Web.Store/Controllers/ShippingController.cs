using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
public class ShippingController(
    IDeliveryDateService deliveryDateService,
    IWarehouseService warehouseService,
    IPickupPointService pickupPointService,
    ITranslationService translationService,
    IContextAccessor contextAccessor) : BaseStoreController
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    #region Delivery dates

    public IActionResult DeliveryDates()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> DeliveryDatesListData()
    {
        var storeId = CurrentStoreId;

        var deliveryDates = await deliveryDateService.GetAllDeliveryDates();

        var items = deliveryDates
            .Select(d => new StoreDeliveryDateModel {
                Id = d.Id,
                Name = d.Name,
                DisplayOrder = d.DisplayOrder,
                LimitedToStores = d.LimitedToStores,
                IsAssignedToCurrentStore = !d.LimitedToStores || d.Stores.Contains(storeId),
                CanManage = d.LimitedToStores
            })
            .ToList();

        var gridModel = new DataSourceResult {
            Data = items,
            Total = items.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AssignDeliveryDate(string id)
    {
        var deliveryDate = await deliveryDateService.GetDeliveryDateById(id);
        if (deliveryDate == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.NotFound") });

        if (!deliveryDate.LimitedToStores)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.CannotModifyGlobal") });

        var storeId = CurrentStoreId;
        if (!deliveryDate.Stores.Contains(storeId))
        {
            deliveryDate.Stores.Add(storeId);
            await deliveryDateService.UpdateDeliveryDate(deliveryDate);
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UnassignDeliveryDate(string id)
    {
        var deliveryDate = await deliveryDateService.GetDeliveryDateById(id);
        if (deliveryDate == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.NotFound") });

        if (!deliveryDate.LimitedToStores)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.CannotModifyGlobal") });

        var storeId = CurrentStoreId;
        if (deliveryDate.Stores.Remove(storeId))
            await deliveryDateService.UpdateDeliveryDate(deliveryDate);

        return Json(new { success = true });
    }

    #endregion

    #region Warehouses

    public IActionResult Warehouses()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> WarehousesListData()
    {
        var storeId = CurrentStoreId;

        var warehouses = await warehouseService.GetAllWarehouses();

        var items = warehouses
            .Select(w => new StoreWarehouseModel {
                Id = w.Id,
                Name = w.Name,
                Code = w.Code,
                DisplayOrder = w.DisplayOrder,
                LimitedToStores = w.LimitedToStores,
                IsAssignedToCurrentStore = !w.LimitedToStores || w.Stores.Contains(storeId),
                CanManage = w.LimitedToStores
            })
            .ToList();

        var gridModel = new DataSourceResult {
            Data = items,
            Total = items.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AssignWarehouse(string id)
    {
        var warehouse = await warehouseService.GetWarehouseById(id);
        if (warehouse == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.Warehouses.NotFound") });

        if (!warehouse.LimitedToStores)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.Warehouses.CannotModifyGlobal") });

        var storeId = CurrentStoreId;
        if (!warehouse.Stores.Contains(storeId))
        {
            warehouse.Stores.Add(storeId);
            await warehouseService.UpdateWarehouse(warehouse);
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UnassignWarehouse(string id)
    {
        var warehouse = await warehouseService.GetWarehouseById(id);
        if (warehouse == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.Warehouses.NotFound") });

        if (!warehouse.LimitedToStores)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.Warehouses.CannotModifyGlobal") });

        var storeId = CurrentStoreId;
        if (warehouse.Stores.Remove(storeId))
            await warehouseService.UpdateWarehouse(warehouse);

        return Json(new { success = true });
    }

    #endregion

    #region Pickup points

    public IActionResult PickupPoints()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> PickupPointsListData()
    {
        var storeId = CurrentStoreId;

        var pickupPoints = await pickupPointService.GetAllPickupPoints();

        // Only show pickup points that are global (empty StoreId) or belong to this store.
        // Pickup points assigned to other stores are not visible to this store's manager.
        var items = pickupPoints
            .Where(p => string.IsNullOrEmpty(p.StoreId) || p.StoreId == storeId)
            .Select(p => new StorePickupPointModel {
                Id = p.Id,
                Name = p.Name,
                DisplayOrder = p.DisplayOrder,
                IsAssignedToCurrentStore = p.StoreId == storeId,
                // Unlike delivery dates and warehouses (which use LimitedToStores/Stores),
                // pickup points use a single StoreId: empty = global, non-empty = store-specific.
                CanManage = true
            })
            .ToList();

        var gridModel = new DataSourceResult {
            Data = items,
            Total = items.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AssignPickupPoint(string id)
    {
        var pickupPoint = await pickupPointService.GetPickupPointById(id);
        if (pickupPoint == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.NotFound") });

        var storeId = CurrentStoreId;
        if (!string.IsNullOrEmpty(pickupPoint.StoreId) && pickupPoint.StoreId != storeId)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.AlreadyAssignedToOtherStore") });

        if (pickupPoint.StoreId != storeId)
        {
            pickupPoint.StoreId = storeId;
            await pickupPointService.UpdatePickupPoint(pickupPoint);
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UnassignPickupPoint(string id)
    {
        var pickupPoint = await pickupPointService.GetPickupPointById(id);
        if (pickupPoint == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.NotFound") });

        var storeId = CurrentStoreId;
        if (pickupPoint.StoreId != storeId)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.NotAssignedToStore") });

        pickupPoint.StoreId = string.Empty;
        await pickupPointService.UpdatePickupPoint(pickupPoint);

        return Json(new { success = true });
    }

    #endregion
}
