﻿using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.ProductAttributes)]
    public partial class ProductAttributeController : BaseAdminController
    {
        #region Fields
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly SeoSettings _seoSettings;

        #endregion Fields

        #region Constructors

        public ProductAttributeController(IProductService productService,
            IProductAttributeService productAttributeService,
            ILanguageService languageService,
            ITranslationService translationService,
            ICustomerActivityService customerActivityService,
            IStoreService storeService,
            IWorkContext workContext,
            IGroupService groupService,
            SeoSettings seoSettings)
        {
            _productService = productService;
            _productAttributeService = productAttributeService;
            _languageService = languageService;
            _translationService = translationService;
            _customerActivityService = customerActivityService;
            _storeService = storeService;
            _workContext = workContext;
            _groupService = groupService;
            _seoSettings = seoSettings;
        }

        #endregion

        #region Methods

        #region Attribute list / create / edit / delete

        //list
        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var productAttributes = await _productAttributeService
                .GetAllProductAttributes(command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = productAttributes.Select(x => x.ToModel()),
                Total = productAttributes.TotalCount
            };

            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new ProductAttributeModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(ProductAttributeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var productAttribute = model.ToEntity();
                productAttribute.SeName = SeoExtensions.GetSeName(string.IsNullOrEmpty(productAttribute.SeName) ? productAttribute.Name : productAttribute.SeName, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls, _seoSettings.SeoCharConversion);
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }

                await _productAttributeService.InsertProductAttribute(productAttribute);

                //activity log
                _ = _customerActivityService.InsertActivity("AddNewProductAttribute", productAttribute.Id,
                    _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    _translationService.GetResource("ActivityLog.AddNewProductAttribute"), productAttribute.Name);

                Success(_translationService.GetResource("Admin.Catalog.Attributes.ProductAttributes.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = productAttribute.Id }) : RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeById(id);
            if (productAttribute == null)
                //No product attribute found with the specified id
                return RedirectToAction("List");

            var model = productAttribute.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = productAttribute.GetTranslation(x => x.Name, languageId, false);
                locale.Description = productAttribute.GetTranslation(x => x.Description, languageId, false);
            });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(ProductAttributeModel model, bool continueEditing)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeById(model.Id);
            if (productAttribute == null)
                //No product attribute found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                productAttribute = model.ToEntity(productAttribute);
                productAttribute.SeName = SeoExtensions.GetSeName(string.IsNullOrEmpty(productAttribute.SeName) ? productAttribute.Name : productAttribute.SeName, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls, _seoSettings.SeoCharConversion);
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }
                await _productAttributeService.UpdateProductAttribute(productAttribute);

                //activity log
                _ = _customerActivityService.InsertActivity("EditProductAttribute", productAttribute.Id,
                    _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    _translationService.GetResource("ActivityLog.EditProductAttribute"), productAttribute.Name);

                Success(_translationService.GetResource("Admin.Catalog.Attributes.ProductAttributes.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = productAttribute.Id });
                }
                return RedirectToAction("List");

            }

            //If we got this far, something failed, redisplay form
            // locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = productAttribute.GetTranslation(x => x.Name, languageId, false);
                locale.Description = productAttribute.GetTranslation(x => x.Description, languageId, false);
            });

            return View(model);
        }

        //delete
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeById(id);
            if (productAttribute == null)
                //No product attribute found with the specified id
                return RedirectToAction("List");
            if (ModelState.IsValid)
            {
                await _productAttributeService.DeleteProductAttribute(productAttribute);

                //activity log
                _ = _customerActivityService.InsertActivity("DeleteProductAttribute", productAttribute.Id,
                    _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    _translationService.GetResource("ActivityLog.DeleteProductAttribute"), productAttribute.Name);

                Success(_translationService.GetResource("Admin.Catalog.Attributes.ProductAttributes.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = productAttribute.Id });
        }

        #endregion

        #region Used by products

        //used by products
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> UsedByProducts(DataSourceRequest command, string productAttributeId)
        {
            var orders = await _productService.GetProductsByProductAtributeId(
                productAttributeId: productAttributeId,
                pageIndex: command.Page - 1,
                pageSize: command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = orders.Select(x =>
                {
                    return new ProductAttributeModel.UsedByProductModel
                    {
                        Id = x.Id,
                        ProductName = x.Name,
                        Published = x.Published
                    };
                }),
                Total = orders.TotalCount
            };
            return Json(gridModel);
        }

        #endregion

        #region Predefined values
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> PredefinedProductAttributeValueList(string productAttributeId, DataSourceRequest command)
        {
            var values = (await _productAttributeService.GetProductAttributeById(productAttributeId)).PredefinedProductAttributeValues;
            var gridModel = new DataSourceResult
            {
                Data = values.Select(x => x.ToModel()),
                Total = values.Count(),
            };

            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> PredefinedProductAttributeValueCreatePopup(string productAttributeId)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeById(productAttributeId);
            if (productAttribute == null)
                throw new ArgumentException("No product attribute found with the specified id");

            var model = new PredefinedProductAttributeValueModel
            {
                ProductAttributeId = productAttributeId
            };

            //locales
            await AddLocales(_languageService, model.Locales);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> PredefinedProductAttributeValueCreatePopup(PredefinedProductAttributeValueModel model)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeById(model.ProductAttributeId);
            if (productAttribute == null)
                throw new ArgumentException("No product attribute found with the specified id");

            if (ModelState.IsValid)
            {
                var ppav = model.ToEntity();
                productAttribute.PredefinedProductAttributeValues.Add(ppav);
                await _productAttributeService.UpdateProductAttribute(productAttribute);
                return Content("");
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> PredefinedProductAttributeValueEditPopup(string id, string productAttributeId)
        {
            var ppav = (await _productAttributeService.GetProductAttributeById(productAttributeId)).PredefinedProductAttributeValues.Where(x => x.Id == id).FirstOrDefault();
            if (ppav == null)
                throw new ArgumentException("No product attribute value found with the specified id");

            var model = ppav.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = ppav.GetTranslation(x => x.Name, languageId, false);
            });
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> PredefinedProductAttributeValueEditPopup(PredefinedProductAttributeValueModel model)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeById(model.ProductAttributeId);
            var ppav = productAttribute.PredefinedProductAttributeValues.Where(x => x.Id == model.Id).FirstOrDefault();
            if (ppav == null)
                throw new ArgumentException("No product attribute value found with the specified id");

            if (ModelState.IsValid)
            {
                ppav = model.ToEntity(ppav);
                await _productAttributeService.UpdateProductAttribute(productAttribute);
                return Content("");
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //delete
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> PredefinedProductAttributeValueDelete(string id)
        {
            if (ModelState.IsValid)
            {
                var productAttribute = (await _productAttributeService.GetAllProductAttributes()).FirstOrDefault(x => x.PredefinedProductAttributeValues.Any(y => y.Id == id));
                var ppav = productAttribute.PredefinedProductAttributeValues.Where(x => x.Id == id).FirstOrDefault();
                if (ppav == null)
                    throw new ArgumentException("No predefined product attribute value found with the specified id");
                productAttribute.PredefinedProductAttributeValues.Remove(ppav);
                await _productAttributeService.UpdateProductAttribute(productAttribute);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }
        #endregion

        #endregion
    }
}
