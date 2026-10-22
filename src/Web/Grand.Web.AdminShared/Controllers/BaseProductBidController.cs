using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Products)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductBidController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IAuctionService auctionService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    #region Bids

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ListBids(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess: mirrors Store's CanAccessProduct and Vendor's HasAccessToProduct checks; Admin's
        // original had no check at all - normalized to scope.HasAccess for all three hosts.
        if (!await scope.CanView(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        //a product shared with other stores is shown read-only; its bids and reservations carry
        //other stores' customers and orders, so they are listed only for a product this store owns
        if (!await scope.HasAccess(product))
            return Json(new DataSourceResult { Data = Array.Empty<object>(), Total = 0 });

        var (bidModels, totalCount) =
            await productViewModelService.PrepareBidMode(productId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = bidModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BidDelete(ProductModel.BidModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var toDelete = await auctionService.GetBid(model.BidId);

        // Cross-product IDOR closed here, identical shape to ProductReservationDelete above: none of the
        // three original hosts verified that the bid being deleted (looked up purely by model.BidId)
        // actually belongs to the product just access-checked (model.ProductId). Both are independent,
        // attacker-supplied POST fields - Store and Vendor's original code let a caller who owns/has
        // access to *any* product pass the access check using that product's id while supplying a BidId
        // belonging to a different, unowned product, deleting a bid on a product they have no access to.
        if (toDelete != null && toDelete.ProductId != product.Id)
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (toDelete != null)
        {
            if (string.IsNullOrEmpty(toDelete.OrderId))
            {
                //delete bid
                await auctionService.DeleteBid(toDelete);
                return Json("");
            }

            return Json(new DataSourceResult {
                Errors = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Bids.CantDeleteWithOrder")
            });
        }

        return Json(new DataSourceResult { Errors = "Bid not exists" });
    }

    #endregion
}
