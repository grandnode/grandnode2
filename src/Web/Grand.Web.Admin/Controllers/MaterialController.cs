using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.DataSource;
using Grand.Business.Core.Interfaces.Catalog.Products;
using static Grand.Web.Admin.Models.Catalog.ProductModel;
using Grand.Web.Common.Filters;

namespace Grand.Web.Admin.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    public class MaterialController : BaseAdminController
    {
        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public IActionResult List(DataSourceRequest command, string productId, string productAttributeMappingId, string productAttributeValueId)
        {
            // Test data
            var materialsList = new List<MaterialModel>() {
                new MaterialModel() {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Material 1",
                    FilePath = "Material 1 file path",
                    Cost = 100,
                    Price = 120
                },
                new MaterialModel() {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Material 2",
                    FilePath = "Material 2 file path",
                    Cost = 110,
                    Price = 130
                },
                new MaterialModel() {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Material 3",
                    FilePath = "Material 3 file path",
                    Cost = 120,
                    Price = 140
                }
            };

            var gridModel = new DataSourceResult {
                Data = materialsList,
                Total = materialsList.Count
            };

            return Json(gridModel);
        }

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
