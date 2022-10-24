using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.DataSource;
using Grand.Business.Core.Interfaces.Catalog.Products;

namespace Grand.Web.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialController : ControllerBase
    {
        //private IProductAttributeService _productAttributeService;
        //public MaterialController(IProductAttributeService productAttributeService)
        //{
        //    _productAttributeService = productAttributeService;
        //}

        //[PermissionAuthorizeAction(PermissionActionName.List)]
        //[HttpPost]
        //public async Task<IActionResult> List(DataSourceRequest command, string productAttributeValueId)
        //{
        //    var productAttributes = await _productAttributeService
        //        .GetProductAttributeById(productAttributeValueId);

        //    var gridModel = new DataSourceResult {
        //        Data = productAttributes.Select(x => x.ToModel()),
        //        Total = productAttributes.TotalCount
        //    };

        //    return Json(gridModel);
        //}

        ////create
        //[PermissionAuthorizeAction(PermissionActionName.Create)]
        //public async Task<IActionResult> Create()
        //{
        //    var model = new ProductAttributeModel();
        //    //locales
        //    await AddLocales(_languageService, model.Locales);
        //    return View(model);
        //}
    }
}
