#nullable enable

using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseCustomerReviewController(
    ICustomerService customerService,
    IProductReviewService productReviewService,
    IProductReviewViewModelService productReviewViewModelService,
    IProductViewModelService productViewModelService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerScopedController(customerService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ReviewList(string customerId, DataSourceRequest command)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var productReviews = await productReviewService.GetAllProductReviews(customerId, null,
            null, null, "", Scope.DefaultStoreId, "", command.Page - 1, command.PageSize);
        var items = new List<ProductReviewModel>();
        foreach (var x in productReviews)
        {
            var m = new ProductReviewModel();
            await productViewModelService.PrepareProductReviewModel(m, x, false, true);
            items.Add(m);
        }

        var gridModel = new DataSourceResult { Data = items, Total = productReviews.TotalCount };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ReviewDelete(string id)
    {
        var productReview = await productReviewService.GetProductReviewById(id);
        if (productReview == null) throw new ArgumentException("No review found with the specified id", nameof(id));
        if (Scope.DefaultStoreId is not null && productReview.StoreId != Scope.DefaultStoreId)
            throw new ArgumentException("No review found with the specified id", nameof(id));

        await productReviewViewModelService.DeleteProductReview(productReview);
        return new JsonResult("");
    }
}
