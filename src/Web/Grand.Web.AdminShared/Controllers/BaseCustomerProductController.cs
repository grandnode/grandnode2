#nullable enable

using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>The customer's product prices and personalized products tabs.</summary>
public abstract class BaseCustomerProductController(
    ICustomerService customerService,
    ICustomerProductViewModelService customerProductViewModelService,
    ICustomerProductService customerProductService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerScopedController(customerService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductsPrice(DataSourceRequest command, string customerId)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var (productPriceModels, totalCount) =
            await customerProductViewModelService.PrepareProductPriceModel(customerId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = productPriceModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> PersonalizedProducts(DataSourceRequest command, string customerId)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var (productModels, totalCount) =
            await customerProductViewModelService.PreparePersonalizedProducts(customerId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = productModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAddPopup(string customerId)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return denied;
        }

        var model = await customerProductViewModelService.PrepareCustomerModelAddProductModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command, CustomerModel.AddProductModel model)
    {
        var products = await customerProductViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = products.products.ToList(), Total = products.totalCount };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopup(string customerId, bool personalized,
        CustomerModel.AddProductModel model)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Content("");
        }

        if (model.SelectedProductIds != null)
            await customerProductViewModelService.InsertCustomerAddProductModel(customerId, personalized, model);
        return Content("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> UpdateProductPrice(CustomerModel.ProductPriceModel model)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var productPrice = await customerProductService.GetCustomerProductPriceById(model.Id);
            if (productPrice == null || !await HasAccessToCustomer(productPrice.CustomerId))
                return new JsonResult("");
        }

        await customerProductViewModelService.UpdateProductPrice(model);
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteProductPrice(string id)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var productPrice = await customerProductService.GetCustomerProductPriceById(id);
            if (productPrice == null || !await HasAccessToCustomer(productPrice.CustomerId))
                return new JsonResult("");

            await customerProductService.DeleteCustomerProductPrice(productPrice);
            return new JsonResult("");
        }

        await customerProductViewModelService.DeleteProductPrice(id);
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> UpdatePersonalizedProduct(CustomerModel.ProductModel model)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var customerProduct = await customerProductService.GetCustomerProduct(model.Id);
            if (customerProduct == null || !await HasAccessToCustomer(customerProduct.CustomerId))
                return new JsonResult("");
        }

        await customerProductViewModelService.UpdatePersonalizedProduct(model);
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeletePersonalizedProduct(string id)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var customerProduct = await customerProductService.GetCustomerProduct(id);
            if (customerProduct == null || !await HasAccessToCustomer(customerProduct.CustomerId))
                return new JsonResult("");

            await customerProductService.DeleteCustomerProduct(customerProduct);
            return new JsonResult("");
        }

        // Admin's original convenience method, preserved for the global-scope branch (still present
        // on ICustomerProductViewModelService — no need to fall back to a two-step Get+Delete).
        await customerProductViewModelService.DeletePersonalizedProduct(id);
        return new JsonResult("");
    }
}
