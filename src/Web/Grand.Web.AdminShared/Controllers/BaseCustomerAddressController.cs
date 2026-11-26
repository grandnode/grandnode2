#nullable enable

using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseCustomerAddressController(
    ICustomerService customerService,
    ICustomerAddressViewModelService customerAddressViewModelService,
    IAddressAttributeParser addressAttributeParser,
    IAddressAttributeService addressAttributeService,
    IRequestTranslationService translationService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerScopedController(customerService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> AddressesSelect(string customerId, DataSourceRequest command)
    {
        var customer = (await LoadAuthorizedCustomer(customerId)).Entity;
        if (customer is null) throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        var addresses = (await customerAddressViewModelService.PrepareAddressModel(customer)).ToList();
        var gridModel = new DataSourceResult { Data = addresses, Total = addresses.Count };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressDelete(string id, string customerId)
    {
        var customer = (await LoadAuthorizedCustomer(customerId)).Entity;
        if (customer is null) throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        var address = customer.Addresses.FirstOrDefault(a => a.Id == id);
        if (address == null) return Content("No customer found with the specified id");
        if (ModelState.IsValid)
        {
            await customerAddressViewModelService.DeleteAddress(customer, address);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddressCreate(string customerId)
    {
        var customerAccess = await LoadAuthorizedCustomer(customerId);
        if (customerAccess.IsDenied) return customerAccess.Denied;
        var customer = customerAccess.Entity;

        var model = new CustomerAddressModel();
        await customerAddressViewModelService.PrepareAddressModel(model, null, customer, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressCreate(CustomerAddressModel model)
    {
        var customerAccess = await LoadAuthorizedCustomer(model.CustomerId);
        if (customerAccess.IsDenied) return customerAccess.Denied;
        var customer = customerAccess.Entity;

        if (ModelState.IsValid)
        {
            var customAttributes =
                await model.Address.ParseCustomAddressAttributes(addressAttributeParser, addressAttributeService);
            var address = await customerAddressViewModelService.InsertAddressModel(customer, model, customAttributes);
            Success(translationService.GetResource("Admin.Customers.Customers.Addresses.Added"));
            return RedirectToAction("AddressEdit", new { addressId = address.Id, customerId = model.CustomerId });
        }

        await customerAddressViewModelService.PrepareAddressModel(model, null, customer, true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddressEdit(string addressId, string customerId)
    {
        var customerAccess = await LoadAuthorizedCustomer(customerId);
        if (customerAccess.IsDenied) return customerAccess.Denied;
        var customer = customerAccess.Entity;

        var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
        if (address == null) return RedirectToAction("Edit", "Customer", new { id = customer.Id });

        var model = new CustomerAddressModel();
        await customerAddressViewModelService.PrepareAddressModel(model, address, customer, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressEdit(CustomerAddressModel model)
    {
        var customerAccess = await LoadAuthorizedCustomer(model.CustomerId);
        if (customerAccess.IsDenied) return customerAccess.Denied;
        var customer = customerAccess.Entity;

        var address = customer.Addresses.FirstOrDefault(x => x.Id == model.Address.Id);
        if (address == null) return RedirectToAction("Edit", "Customer", new { id = customer.Id });

        if (ModelState.IsValid)
        {
            var customAttributes =
                await model.Address.ParseCustomAddressAttributes(addressAttributeParser, addressAttributeService);
            await customerAddressViewModelService.UpdateAddressModel(customer, address, model, customAttributes);
            Success(translationService.GetResource("Admin.Customers.Customers.Addresses.Updated"));
            return RedirectToAction("AddressEdit", new { addressId = model.Address.Id, customerId = model.CustomerId });
        }

        await customerAddressViewModelService.PrepareAddressModel(model, address, customer, true);
        return View(model);
    }
}
