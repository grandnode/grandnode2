using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Seo;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.ProductTags)]
    public partial class ProductTagsController : BaseAdminController
    {
        private readonly IProductTagService _productTagService;
        private readonly IProductService _productService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;

        private readonly SeoSettings _seoSettings;
        public ProductTagsController(
            IProductTagService productTagService, 
            IProductService productService, 
            ILanguageService languageService,
            ITranslationService translationService,
            SeoSettings seoSettings)
        {
            _productTagService = productTagService;
            _productService = productService;
            _languageService = languageService;
            _translationService = translationService;
            _seoSettings = seoSettings;
        }

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var tags = (await _productTagService.GetAllProductTags());
            var productTags = new List<ProductTagModel>();
            foreach (var item in tags)
            {
                var ptag = new ProductTagModel();
                ptag.Id = item.Id;
                ptag.Name = item.Name;
                ptag.ProductCount = await _productTagService.GetProductCount(item.Id, "");
                productTags.Add(ptag);
            }

            var gridModel = new DataSourceResult
            {
                Data = productTags.OrderByDescending(x => x.ProductCount).PagedForCommand(command),
                Total = tags.Count()
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> Products(string tagId, DataSourceRequest command)
        {
            var tag = await _productTagService.GetProductTagById(tagId);

            var products = (await _productService.SearchProducts(pageIndex: command.Page - 1, pageSize: command.PageSize, productTag: tag.Name, orderBy: Domain.Catalog.ProductSortingEnum.NameAsc)).products;
            var gridModel = new DataSourceResult
            {
                Data = products.Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name,
                }),
                Total = products.TotalCount
            };

            return Json(gridModel);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var productTag = await _productTagService.GetProductTagById(id);
            if (productTag == null)
                //No product tag found with the specified id
                return RedirectToAction("List");

            var model = new ProductTagModel
            {
                Id = productTag.Id,
                Name = productTag.Name,
                ProductCount = await _productTagService.GetProductCount(productTag.Id, "")
            };
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = productTag.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> Edit(ProductTagModel model)
        {
            var productTag = await _productTagService.GetProductTagById(model.Id);
            if (productTag == null)
                //No product tag found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                productTag.Name = model.Name;
                productTag.Locales = model.Locales.ToTranslationProperty();
                productTag.SeName = SeoExtensions.GetSeName(productTag.Name, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls, _seoSettings.SeoCharConversion);
                await _productTagService.UpdateProductTag(productTag);
                Success(_translationService.GetResource("Admin.Catalog.ProductTags.Updated"));
                
                return RedirectToAction("Edit", new { id = model.Id });
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var tag = await _productTagService.GetProductTagById(id);
            if (tag == null)
                throw new ArgumentException("No product tag found with the specified id");
            if (ModelState.IsValid)
            {
                await _productTagService.DeleteProductTag(tag);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }
    }
}
