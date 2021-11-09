﻿using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.CustomerTags)]
    public partial class CustomerTagController : BaseAdminController
    {
        #region Fields
        private readonly ICustomerTagViewModelService _customerTagViewModelService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerTagService _customerTagService;
        #endregion

        #region Constructors

        public CustomerTagController(
            ICustomerTagViewModelService customerTagViewModelService,
            ITranslationService translationService,
            ICustomerTagService customerTagService)
        {
            _customerTagViewModelService = customerTagViewModelService;
            _translationService = translationService;
            _customerTagService = customerTagService;
        }

        #endregion

        #region Customer Tags

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var customertags = await _customerTagService.GetAllCustomerTags();
            var items = new List<(string Id, string Name, int Count)>();
            foreach (var item in customertags)
            {
                items.Add((Id: item.Id, Name: item.Name, Count: await _customerTagService.GetCustomerCount(item.Id)));
            }
            var gridModel = new DataSourceResult
            {
                Data = items.Select(x => new { Id = x.Id, Name = x.Name, Count = x.Count }),
                Total = customertags.Count()
            };
            return Json(gridModel);
        }

        [HttpGet]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> Search(string term)
        {
            var customertags = (await _customerTagService.GetCustomerTagsByName(term)).Select(x => x.Name);
            return Json(customertags);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> Customers(string customerTagId, DataSourceRequest command)
        {
            var customers = await _customerTagService.GetCustomersByTag(customerTagId, command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = customers.Select(x => _customerTagViewModelService.PrepareCustomerModelForList(x)),
                Total = customers.TotalCount
            };
            return Json(gridModel);
        }
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public IActionResult Create()
        {
            var model = _customerTagViewModelService.PrepareCustomerTagModel();
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create(CustomerTagModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var customertag = await _customerTagViewModelService.InsertCustomerTagModel(model);
                Success(_translationService.GetResource("Admin.Customers.CustomerTags.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = customertag.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var customerTag = await _customerTagService.GetCustomerTagById(id);
            if (customerTag == null)
                //No customer group found with the specified id
                return RedirectToAction("List");

            var model = customerTag.ToModel();
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(CustomerTagModel model, bool continueEditing)
        {
            var customertag = await _customerTagService.GetCustomerTagById(model.Id);
            if (customertag == null)
                //No customer group found with the specified id
                return RedirectToAction("List");

            try
            {
                if (ModelState.IsValid)
                {
                    customertag = await _customerTagViewModelService.UpdateCustomerTagModel(customertag, model);
                    Success(_translationService.GetResource("Admin.Customers.CustomerTags.Updated"));
                    return continueEditing ? RedirectToAction("Edit", new { id = customertag.Id }) : RedirectToAction("List");
                }

                //If we got this far, something failed, redisplay form
                return View(model);
            }
            catch (Exception exc)
            {
                Error(exc);
                return RedirectToAction("Edit", new { id = customertag.Id });
            }
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> CustomerDelete(string Id, string customerTagId)
        {
            var customertag = await _customerTagService.GetCustomerTagById(customerTagId);
            if (customertag == null)
                throw new ArgumentException("No customertag found with the specified id");
            if (ModelState.IsValid)
            {
                await _customerTagService.DeleteTagFromCustomer(customerTagId, Id);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            var customerTag = await _customerTagService.GetCustomerTagById(id);
            if (customerTag == null)
                //No customer group found with the specified id
                return RedirectToAction("List");

            try
            {
                if (ModelState.IsValid)
                {
                    await _customerTagViewModelService.DeleteCustomerTag(customerTag);
                    Success(_translationService.GetResource("Admin.Customers.CustomerTags.Deleted"));
                    return RedirectToAction("List");
                }
                Error(ModelState);
                return RedirectToAction("Edit", new { id = customerTag.Id });
            }
            catch (Exception exc)
            {
                Error(exc.Message);
                return RedirectToAction("Edit", new { id = customerTag.Id });
            }
        }
        #endregion

        #region Products

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Products(string customerTagId, DataSourceRequest command, [FromServices] IProductService productService)
        {
            var products = await _customerTagService.GetCustomerTagProducts(customerTagId);
            var items = new List<CustomerGroupProductModel>();
            foreach (var x in products)
            {
                items.Add(new CustomerGroupProductModel
                {
                    Id = x.Id,
                    Name = (await productService.GetProductById(x.ProductId))?.Name,
                    ProductId = x.ProductId,
                    DisplayOrder = x.DisplayOrder
                });
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = products.Count()
            };
            return Json(gridModel);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ProductDelete(string id)
        {
            var ctp = await _customerTagService.GetCustomerTagProductById(id);
            if (ctp == null)
                throw new ArgumentException("No found the specified id");
            if (ModelState.IsValid)
            {
                await _customerTagService.DeleteCustomerTagProduct(ctp);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ProductUpdate(CustomerGroupProductModel model)
        {
            var ctp = await _customerTagService.GetCustomerTagProductById(model.Id);
            if (ctp == null)
                throw new ArgumentException("No customer tag product found with the specified id");
            if (ModelState.IsValid)
            {
                ctp.DisplayOrder = model.DisplayOrder;
                await _customerTagService.UpdateCustomerTagProduct(ctp);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        public async Task<IActionResult> ProductAddPopup(string customerTagId)
        {
            var model = await _customerTagViewModelService.PrepareProductModel(customerTagId);
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command, CustomerTagProductModel.AddProductModel model)
        {
            var products = await _customerTagViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = products.products,
                Total = products.totalCount
            };
            return Json(gridModel);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ProductAddPopup(CustomerTagProductModel.AddProductModel model)
        {
            if (model.SelectedProductIds != null)
            {
                await _customerTagViewModelService.InsertProductModel(model);
            }
            return Content("");
        }
        #endregion
    }
}
