using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Products)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductPictureController(
    IProductPictureViewModelService productPictureViewModelService,
    IProductService productService,
    ILanguageService languageService,
    IRequestTranslationService translationService,
    IRequestPermissionService permissionService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    #region Product pictures

    [HttpPost]
    public async Task<IActionResult> ProductPictureAdd(
        IFormFileCollection files,
        Reference reference, string objectId,
        [FromServices] IPictureService pictureService,
        [FromServices] MediaSettings mediaSettings)
    {
        if (!await permissionService.Authorize(PermissionSystemName.Pictures))
            return Json(new {
                success = false,
                message = "Access denied - picture permissions"
            });

        if (reference != Reference.Product || string.IsNullOrEmpty(objectId))
            return Json(new {
                success = false,
                message = "Please save form before upload new pictures"
            });

        if (!files.Any())
            return Json(new {
                success = false,
                message = "No files uploaded"
            });

        var product = await productService.GetProductById(objectId);

        if (!await scope.HasAccess(product))
            return Json(new {
                success = false,
                message = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions")
            });

        var values = new List<(string pictureUrl, string pictureId)>();
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file.FileName);
            var contentType = file.ContentType;
            var fileExtension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(contentType))
                _ = new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);

            if (FileExtensions.GetAllowedMediaFileTypes(mediaSettings.AllowedFileTypes).IsAllowedMediaFileType(fileExtension))
            {
                var fileBinary = file.GetDownloadBits();
                //insert picture
                var picture = await pictureService.InsertPicture(fileBinary, contentType, null, reference: reference,
                    objectId: objectId);
                var pictureUrl = await pictureService.GetPictureUrl(picture);

                values.Add((pictureUrl, picture.Id));
                //assign picture to the product
                await productPictureViewModelService.InsertProductPicture(product, picture, 0);
            }
        }

        return Json(new { success = values.Any(), data = values });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductPictureList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating
        // this action on both hosts. Admin's original had no check at all. Vendor's original signature
        // also lacked the unused `DataSourceRequest command` parameter that Admin/Store both bind (Kendo
        // posts it, but no host ever reads it) - kept here to match the two-of-three shape; harmless for
        // Vendor since an unused extra bound parameter changes nothing about the response.
        if (!await scope.CanView(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productPicturesModel = await productPictureViewModelService.PrepareProductPicturesModel(product);
        var gridModel = new DataSourceResult {
            Data = productPicturesModel,
            Total = productPicturesModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> ProductPicturePopup(string productId, string id)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            return Content("Product not exist");

        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating
        // this action on both hosts. Admin's original had no check at all.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var pp = product.ProductPictures.FirstOrDefault(x => x.Id == id);
        if (pp == null)
            return Content("Product picture not exist");

        var (model, picture) = await productPictureViewModelService.PrepareProductPictureModel(product, pp);
        //locales
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.AltAttribute = picture?.GetTranslation(x => x.AltAttribute, languageId, false);
            locale.TitleAttribute = picture?.GetTranslation(x => x.TitleAttribute, languageId, false);
        });

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPicturePopup(ProductModel.ProductPictureModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await productService.GetProductById(model.ProductId);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
            // ProductPicturePopup(POST) had no check at all, letting any vendor rename/re-alt-text another
            // vendor's product picture by posting its productId/model.Id - closed here the same way as the
            // other rows in this task.
            if (!await scope.HasAccess(product))
                throw new ArgumentException(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

            if (product.ProductPictures.FirstOrDefault(x => x.Id == model.Id) == null)
                throw new ArgumentException("No product picture found with the specified id");

            await productPictureViewModelService.UpdateProductPicture(model);

            return Content("");
        }

        Error(ModelState);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPictureDelete(ProductModel.ProductPictureModel model)
    {
        var product = await productService.GetProductById(model.ProductId);

        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
        // ProductPictureDelete had no check at all, letting any vendor delete another vendor's product
        // picture by posting its productId/model.Id - closed here the same way as ProductPicturePopup(POST)
        // above.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productPictureViewModelService.DeleteProductPicture(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
