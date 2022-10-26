﻿using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.DataSource;
using Grand.Business.Core.Interfaces.Catalog.Products;
using static Grand.Web.Admin.Models.Catalog.ProductModel;
using Grand.Web.Common.Filters;
using System.ComponentModel.DataAnnotations;
using Grand.Web.Admin.Interfaces;

namespace Grand.Web.Admin.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    public class MaterialController : BaseAdminController
    {
        private IProductService _productService;
        private IMaterialViewModelService _materialViewModelService;

        public MaterialController(IProductService productService, IMaterialViewModelService materialViewModelService)
        {
            _productService = productService;
            _materialViewModelService = materialViewModelService;
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, [Required] string productId, [Required] string productAttributeMappingId, [Required] string productAttributeValueId)
        {
            var product = await _productService.GetProductById(productId);            

            if(product == null)
            {
                return BadRequest("Product Not found");
            }
            var pam = product.ProductAttributeMappings.Where(x => x.Id == productAttributeMappingId).FirstOrDefault();

            if (pam is null)
            {
                return BadRequest("Product Attribute Mapping not found");
            }
            
            var pav = pam.ProductAttributeValues.Where(x => x.Id == productAttributeValueId).FirstOrDefault();
            if(pav is null)
            {
                return BadRequest("Product Attribute Value not found");
            }

            IList<MaterialModel> materialModels = _materialViewModelService.PrepareMaterialViewModel(pav.Materials);

            var gridModel = new DataSourceResult {
                Data = materialModels,
                Total = materialModels.Count
            };

            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public IActionResult Edit(MaterialModel materialModel, string productId, string productAttributeMappingId, string productAttributeValueId)
        {
            
            return RedirectToAction("List", new { productId = productId, productAttributeMappingId = productAttributeMappingId, productAttributeValueId = productAttributeValueId});
        }
    }
}
