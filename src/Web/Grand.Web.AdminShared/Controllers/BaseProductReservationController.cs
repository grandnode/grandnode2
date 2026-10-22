using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Products)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductReservationController(
    IProductService productService,
    IRequestTranslationService translationService,
    IProductReservationService productReservationService,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    #region Reservation

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ListReservations(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct checks;
        // Admin's original had no check at all - normalized to scope.HasAccess for all three hosts.
        if (!await scope.CanView(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        //a product shared with other stores is shown read-only; its bids and reservations carry
        //other stores' customers and orders, so they are listed only for a product this store owns
        if (!await scope.HasAccess(product))
            return Json(new DataSourceResult { Data = Array.Empty<object>(), Total = 0 });

        var reservations =
            await productReservationService.GetProductReservationsByProductId(productId, null, null,
                command.Page - 1, command.PageSize);
        var reservationModel = reservations
            .Select(x => new ProductModel.ReservationModel {
                ReservationId = x.Id,
                Date = x.Date,
                OrderId = x.OrderId,
                ProductId = x.ProductId,
                Parameter = x.Parameter,
                Resource = x.Resource,
                Duration = x.Duration
            }).ToList();

        var gridModel = new DataSourceResult {
            Data = reservationModel,
            Total = reservations.TotalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> GenerateCalendar(string productId, ProductModel.GenerateCalendarModel model)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // HasAccess (strict), returning the same {errors:...} JSON shape as Store's CanAccessProduct
        // check (the client's ajax success handler reads data.errors on denial). Vendor's original threw
        // ArgumentException for the combined null-or-access-denied case instead, which the client's
        // Kendo/ajax error callback only surfaces as a generic "Error" alert - returning the JSON errors
        // message here is strictly more informative and closes Admin's original gap (no check at all).
        if (!await scope.HasAccess(product))
            return Json(new { errors = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions") });

        var reservations = await productReservationService.GetProductReservationsByProductId(productId, null, null);
        if (reservations.Any())
            if (((product.IntervalUnitId == IntervalUnit.Minute || product.IntervalUnitId == IntervalUnit.Hour) &&
                 (IntervalUnit)model.Interval == IntervalUnit.Day) ||
                (product.IntervalUnitId == IntervalUnit.Day &&
                 ((IntervalUnit)model.IntervalUnit == IntervalUnit.Minute ||
                  (IntervalUnit)model.IntervalUnit == IntervalUnit.Hour)))
                return Json(new {
                    errors = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Calendar.CannotChangeInterval")
                });

        if (!ModelState.IsValid)
        {
            var error = (Dictionary<string, Dictionary<string, object>>)ModelState.SerializeErrors();
            var s = "";
            foreach (var error1 in error)
                foreach (var error2 in error1.Value)
                {
                    var v = (string[])error2.Value;
                    s += v[0] + "\n";
                }

            return Json(new { errors = s });
        }

        //update fields on product
        await productService.UpdateProductField(product, x => x.Interval, model.Interval);
        await productService.UpdateProductField(product, x => x.IntervalUnitId, (IntervalUnit)model.IntervalUnit);
        await productService.UpdateProductField(product, x => x.IncBothDate, model.IncBothDate);

        var minutesToAdd = (IntervalUnit)model.IntervalUnit switch {
            IntervalUnit.Minute => model.Interval,
            IntervalUnit.Hour => model.Interval * 60,
            IntervalUnit.Day => model.Interval * 60 * 24,
            _ => 0
        };

        var _hourFrom = model.StartTime.Hour;
        var _minutesFrom = model.StartTime.Minute;
        var _hourTo = model.EndTime.Hour;
        var _minutesTo = model.EndTime.Minute;
        var _dateFrom = new DateTime(model.StartDate.Value.Year, model.StartDate.Value.Month, model.StartDate.Value.Day,
            0, 0, 0, 0);
        var _dateTo = new DateTime(model.EndDate.Value.Year, model.EndDate.Value.Month, model.EndDate.Value.Day, 23, 59,
            59, 999);
        if ((IntervalUnit)model.IntervalUnit == IntervalUnit.Day)
        {
            model.Quantity = 1;
            model.Parameter = "";
        }
        else
        {
            model.Resource = "";
        }

        var dates = new List<DateTime>();
        var counter = 0;
        for (var iterator = _dateFrom; iterator <= _dateTo; iterator += new TimeSpan(0, minutesToAdd, 0))
        {
            if ((IntervalUnit)model.IntervalUnit != IntervalUnit.Day)
            {
                if (iterator.Hour >= _hourFrom && iterator.Hour <= _hourTo)
                {
                    if (iterator.Hour == _hourTo)
                        if (iterator.Minute > _minutesTo)
                            continue;
                    if (iterator.Hour == _hourFrom)
                        if (iterator.Minute < _minutesFrom)
                            continue;
                }
                else
                {
                    continue;
                }
            }

            if ((iterator.DayOfWeek == DayOfWeek.Monday && !model.Monday) ||
                (iterator.DayOfWeek == DayOfWeek.Tuesday && !model.Tuesday) ||
                (iterator.DayOfWeek == DayOfWeek.Wednesday && !model.Wednesday) ||
                (iterator.DayOfWeek == DayOfWeek.Thursday && !model.Thursday) ||
                (iterator.DayOfWeek == DayOfWeek.Friday && !model.Friday) ||
                (iterator.DayOfWeek == DayOfWeek.Saturday && !model.Saturday) ||
                (iterator.DayOfWeek == DayOfWeek.Sunday && !model.Sunday))
                continue;

            for (var i = 0; i < model.Quantity.MaxQuantity(); i++)
            {
                dates.Add(iterator);
                try
                {
                    var insert = true;
                    if ((IntervalUnit)model.IntervalUnit == IntervalUnit.Day)
                        if (reservations.Any(x => x.Resource == model.Resource && x.Date == iterator))
                            insert = false;
                    if (insert)
                    {
                        if (counter++ > 1000)
                            break;

                        await productReservationService.InsertProductReservation(new ProductReservation {
                            OrderId = "",
                            Date = iterator,
                            ProductId = productId,
                            Resource = model.Resource,
                            Parameter = model.Parameter,
                            Duration = model.Interval + " " + enumTranslationService.GetTranslationEnum((IntervalUnit)model.IntervalUnit)
                        });
                    }
                }
                catch { }
            }
        }

        return Json(new { success = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ClearCalendar(string productId)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // Throw, not Json({errors}): unlike GenerateCalendar, this view's success callback never reads
        // the response body at all (see CreateOrUpdate.Calendar.cshtml on all three hosts - identical,
        // no else branch) - a 200 here would silently refresh the grid as if the clear had succeeded. A
        // thrown exception at least surfaces a generic error via the ajax error callback. The view not
        // reading a denial message is a pre-existing gap, out of scope for this migration.
        if (!await scope.HasAccess(product))
            throw new ArgumentException(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var toDelete = await productReservationService.GetProductReservationsByProductId(productId, true, null);
        foreach (var record in toDelete) await productReservationService.DeleteProductReservation(record);

        return Json("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ClearOld(string productId)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // Throw, not Json({errors}): see the comment on ClearCalendar above - this view's success
        // callback doesn't read the response body either.
        if (!await scope.HasAccess(product))
            throw new ArgumentException(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var toDelete =
            (await productReservationService.GetProductReservationsByProductId(productId, true, null)).Where(x =>
                x.Date < DateTime.UtcNow);
        foreach (var record in toDelete) await productReservationService.DeleteProductReservation(record);

        return Json("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductReservationDelete(ProductModel.ReservationModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var toDelete = await productReservationService.GetProductReservation(model.ReservationId);

        // Cross-product IDOR closed here: none of the three original hosts verified that the reservation
        // being deleted (looked up purely by model.ReservationId) actually belongs to the product just
        // access-checked (model.ProductId). Both are independent, attacker-supplied POST fields - Store
        // and Vendor's original code let a caller who owns/has-access-to *any* product pass the access
        // check with that product's id while supplying a ReservationId belonging to a different, unowned
        // product, deleting a reservation on a product they have no access to.
        if (toDelete != null && toDelete.ProductId != product.Id)
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (toDelete != null)
        {
            if (string.IsNullOrEmpty(toDelete.OrderId))
                await productReservationService.DeleteProductReservation(toDelete);
            else
                return Json(new DataSourceResult {
                    Errors = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.ProductReservations.CantDeleteWithOrder")
                });
        }

        return Json("");
    }

    #endregion
}
