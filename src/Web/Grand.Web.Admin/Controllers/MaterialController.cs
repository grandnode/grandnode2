using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.DataSource;
using Grand.Business.Core.Interfaces.Catalog.Products;
using static Grand.Web.Admin.Models.Catalog.ProductModel;
using Grand.Web.Common.Filters;
using System.ComponentModel.DataAnnotations;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Extensions.Mapping;

namespace Grand.Web.Admin.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    public class MaterialController : BaseAdminController
    {
        private IProductService _productService;
        private IMaterialViewModelService _materialViewModelService;
        private IMaterialService _materialService;

        public MaterialController(IProductService productService, IMaterialViewModelService materialViewModelService, IMaterialService materialService)
        {
            _productService = productService;
            _materialViewModelService = materialViewModelService;
            _materialService = materialService;
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

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> Edit(MaterialModel materialModel, string productId, string productAttributeMappingId, string productAttributeValueId)
        {

            var material = await _materialService.UpdateMaterial(materialModel.ToEntity(), productId, productAttributeMappingId, productAttributeValueId);

            return RedirectToAction("List", new { productId = productId, productAttributeMappingId = productAttributeMappingId, productAttributeValueId = productAttributeValueId});
        }



        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> Insert(MaterialModel materialModel, string productId, string productAttributeMappingId, string productAttributeValueId)
        {

            var material = await _materialService.InsertMaterial(materialModel.ToEntity(), productId, productAttributeMappingId, productAttributeValueId);

            return RedirectToAction("List", new { productId = productId, productAttributeMappingId = productAttributeMappingId, productAttributeValueId = productAttributeValueId });
        }
    }
}
