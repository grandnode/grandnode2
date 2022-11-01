using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Web.Admin.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers
{
    public class CustomizeController : Controller
    {
        private IProductService _productService;

        public CustomizeController(IProductViewModelService productViewModelService, IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Index(string originalProductId, string customizedLinkedProductId)
        {
            var product = await _productService.GetProductById(customizedLinkedProductId);
            if(product == null)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
