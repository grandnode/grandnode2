using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Vendor.Extensions;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Models.Catalog;
using Grand.Web.Vendor.Models.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Web.Vendor.Controllers;

[PermissionAuthorize(PermissionSystemName.Products)]
public class ProductController : BaseVendorController
{
    #region Constructors

    public ProductController(
        IProductViewModelService productViewModelService,
        IProductService productService,
        IInventoryManageService inventoryManageService,
        IWorkContext workContext,
        ILanguageService languageService,
        ITranslationService translationService,
        IProductReservationService productReservationService,
        IAuctionService auctionService,
        IDateTimeService dateTimeService,
        IPermissionService permissionService)
    {
        _productViewModelService = productViewModelService;
        _productService = productService;
        _inventoryManageService = inventoryManageService;
        _workContext = workContext;
        _languageService = languageService;
        _translationService = translationService;
        _productReservationService = productReservationService;
        _auctionService = auctionService;
        _dateTimeService = dateTimeService;
        _permissionService = permissionService;
    }

    #endregion

    #region Fields

    private readonly IProductViewModelService _productViewModelService;
    private readonly IProductService _productService;
    private readonly IInventoryManageService _inventoryManageService;
    private readonly IWorkContext _workContext;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly IProductReservationService _productReservationService;
    private readonly IAuctionService _auctionService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPermissionService _permissionService;

    #endregion

    #region Methods

    private Task<(bool allow, string message)> CheckAccessToProduct(Product product)
    {
        if (product == null) return Task.FromResult((false, "Product not exists"));

        //a vendor should have access only to his products
        return product.VendorId != _workContext.CurrentVendor.Id
            ? Task.FromResult((false, "This is not your product"))
            : Task.FromResult<(bool allow, string message)>((true, null));
    }

    #region Product list / create / edit / delete

    //list products
    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public async Task<IActionResult> List()
    {
        var model = await _productViewModelService.PrepareProductListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ProductList(DataSourceRequest command, ProductListModel model)
    {
        var (productModels, totalCount) =
            await _productViewModelService.PrepareProductsModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = productModels.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GoToSku(ProductListModel model)
    {
        var sku = model.GoDirectlyToSku;

        //try to load a product entity
        var product = await _productService.GetProductBySku(sku);
        if (product != null) return RedirectToAction("Edit", "Product", new { id = product.Id });

        //not found
        Warning(_translationService.GetResource("Vendor.Catalog.Products.List.SkuNotFound"));
        return RedirectToAction("List", "Product");
    }

    //create product
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new ProductModel();
        await _productViewModelService.PrepareProductModel(model, null, true);
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(ProductModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var product = await _productViewModelService.InsertProductModel(model);
            Success(_translationService.GetResource("Vendor.Catalog.Products.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = product.Id }) : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        await _productViewModelService.PrepareProductModel(model, null, false);
        return View(model);
    }

    //edit product
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var product = await _productService.GetProductById(id, true);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            //No product found with the specified id
            return RedirectToAction("List");

        var model = product.ToModel(_dateTimeService);
        //model.Ticks = product.UpdatedOnUtc.Ticks;

        await _productViewModelService.PrepareProductModel(model, product, false);
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = product.GetTranslation(x => x.Name, languageId, false);
            locale.ShortDescription = product.GetTranslation(x => x.ShortDescription, languageId, false);
            locale.FullDescription = product.GetTranslation(x => x.FullDescription, languageId, false);
            locale.MetaKeywords = product.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = product.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = product.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = product.GetSeName(languageId, false);
        });

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(ProductModel model, bool continueEditing)
    {
        var product = await _productService.GetProductById(model.Id, true);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            //No product found with the specified id
            return RedirectToAction("List");

        if (model.Ticks != product.Ticks)
        {
            Error(_translationService.GetResource("Vendor.Catalog.Products.Fields.ChangedWarning"));
            return RedirectToAction("Edit", new { id = product.Id });
        }

        if (ModelState.IsValid)
        {
            product = await _productViewModelService.UpdateProductModel(product, model);

            Success(_translationService.GetResource("Vendor.Catalog.Products.Updated"));
            if (continueEditing)
            {
                //selected tab
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = product.Id });
            }

            return RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        await _productViewModelService.PrepareProductModel(model, product, false);

        return View(model);
    }

    //delete product
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var product = await _productService.GetProductById(id, true);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            //No product found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _productViewModelService.DeleteProduct(product);
            Success(_translationService.GetResource("Vendor.Catalog.Products.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteSelected(ICollection<string> selectedIds)
    {
        if (selectedIds != null) await _productViewModelService.DeleteSelected(selectedIds.ToList());

        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    public async Task<IActionResult> CopyProduct(ProductModel model,
        [FromServices] ICopyProductService copyProductService, [FromServices] IPictureService pictureService)
    {
        var copyModel = model.CopyProductModel;
        try
        {
            var originalProduct = await _productService.GetProductById(copyModel.Id, true);
            //a vendor should have access only to his products
            if (originalProduct == null || originalProduct.VendorId != _workContext.CurrentVendor.Id)
                return RedirectToAction("List");

            var newProduct = await copyProductService.CopyProduct(originalProduct,
                copyModel.Name, copyModel.Published);

            if (copyModel.CopyImages) await CopyImages(originalProduct, newProduct, pictureService);

            Success("The product has been copied successfully");
            return RedirectToAction("Edit", new { id = newProduct.Id });
        }
        catch (Exception exc)
        {
            Error(exc.Message);
            return RedirectToAction("Edit", new { id = copyModel.Id });
        }
    }

    private async Task CopyImages(Product originalProduct, Product newProduct, IPictureService pictureService)
    {
        foreach (var productPicture in originalProduct.ProductPictures)
        {
            var picture = await pictureService.GetPictureById(productPicture.PictureId);
            var pictureCopy = await pictureService.InsertPicture(
                await pictureService.LoadPictureBinary(picture),
                picture.MimeType,
                pictureService.GetPictureSeName(newProduct.Name),
                picture.AltAttribute,
                picture.TitleAttribute,
                false,
                Reference.Product,
                newProduct.Id);

            await _productService.InsertProductPicture(new ProductPicture {
                PictureId = pictureCopy.Id,
                DisplayOrder = productPicture.DisplayOrder
            }, newProduct.Id);
        }
    }

    #endregion

    #region Required products

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> LoadProductFriendlyNames(string productIds)
    {
        var result = "";

        if (!string.IsNullOrWhiteSpace(productIds))
        {
            var rangeArray = productIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var products = await _productService.GetProductsByIds(Enumerable.ToArray(rangeArray), true);
            for (var i = 0; i <= products.Count - 1; i++)
            {
                if (products[i].VendorId != _workContext.CurrentVendor.Id) continue;

                result += products[i].Name;
                if (i != products.Count - 1)
                    result += ", ";
            }
        }

        return Json(new { Text = result });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> RequiredProductAddPopup(string productIdsInput)
    {
        var model = await _productViewModelService.PrepareAddRequiredProductModel();
        ViewBag.productIdsInput = productIdsInput;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RequiredProductAddPopupList(DataSourceRequest command,
        ProductModel.AddRequiredProductModel model)
    {
        var (products, totalCount) =
            await _productViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    #endregion

    #region Product categories

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductCategoryList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var productCategoriesModel = await _productViewModelService.PrepareProductCategoryModel(product);
        var gridModel = new DataSourceResult {
            Data = productCategoriesModel,
            Total = productCategoriesModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCategoryInsert(ProductModel.ProductCategoryModel model)
    {
        if (ModelState.IsValid)
            try
            {
                await _productViewModelService.InsertProductCategoryModel(model);
                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCategoryUpdate(ProductModel.ProductCategoryModel model)
    {
        if (ModelState.IsValid)
            try
            {
                await _productViewModelService.UpdateProductCategoryModel(model);
                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCategoryDelete(ProductModel.ProductCategoryModel model)
    {
        if (ModelState.IsValid)
        {
            await _productViewModelService.DeleteProductCategory(model.Id, model.ProductId);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Product collections

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductCollectionList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var productCollectionsModel = await _productViewModelService.PrepareProductCollectionModel(product);
        var gridModel = new DataSourceResult {
            Data = productCollectionsModel,
            Total = productCollectionsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCollectionInsert(ProductModel.ProductCollectionModel model)
    {
        if (ModelState.IsValid)
            try
            {
                await _productViewModelService.InsertProductCollection(model);
                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCollectionUpdate(ProductModel.ProductCollectionModel model)
    {
        if (ModelState.IsValid)
            try
            {
                await _productViewModelService.UpdateProductCollection(model);
                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCollectionDelete(ProductModel.ProductCollectionModel model)
    {
        if (ModelState.IsValid)
        {
            await _productViewModelService.DeleteProductCollection(model.Id, model.ProductId);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Related products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var relatedProducts = product.RelatedProducts.OrderBy(x => x.DisplayOrder);
        var relatedProductsModel = new List<ProductModel.RelatedProductModel>();
        foreach (var x in relatedProducts)
            relatedProductsModel.Add(new ProductModel.RelatedProductModel {
                Id = x.Id,
                ProductId1 = productId,
                ProductId2 = x.ProductId2,
                Product2Name = (await _productService.GetProductById(x.ProductId2))?.Name,
                DisplayOrder = x.DisplayOrder
            });

        var gridModel = new DataSourceResult {
            Data = relatedProductsModel,
            Total = relatedProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductUpdate(ProductModel.RelatedProductModel model)
    {
        if (ModelState.IsValid)
        {
            await _productViewModelService.UpdateRelatedProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductDelete(ProductModel.RelatedProductModel model)
    {
        if (ModelState.IsValid)
        {
            await _productViewModelService.DeleteRelatedProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> RelatedProductAddPopup(string productId)
    {
        var model = await _productViewModelService.PrepareRelatedProductModel();
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductAddPopupList(DataSourceRequest command,
        ProductModel.AddRelatedProductModel model)
    {
        var (products, totalCount) =
            await _productViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductAddPopup(ProductModel.AddRelatedProductModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await _productViewModelService.InsertRelatedProductModel(model);
            return Content("");
        }

        return Content(ModelState.GetErrors());
    }

    #endregion

    #region Similar products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var similarProducts = product.SimilarProducts.OrderBy(x => x.DisplayOrder);
        var similarProductsModel = new List<ProductModel.SimilarProductModel>();
        foreach (var x in similarProducts)
            similarProductsModel.Add(new ProductModel.SimilarProductModel {
                Id = x.Id,
                ProductId1 = productId,
                ProductId2 = x.ProductId2,
                Product2Name = (await _productService.GetProductById(x.ProductId2))?.Name,
                DisplayOrder = x.DisplayOrder
            });

        var gridModel = new DataSourceResult {
            Data = similarProductsModel,
            Total = similarProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductUpdate(ProductModel.SimilarProductModel model)
    {
        if (ModelState.IsValid)
        {
            await _productViewModelService.UpdateSimilarProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductDelete(ProductModel.SimilarProductModel model)
    {
        if (ModelState.IsValid)
        {
            await _productViewModelService.DeleteSimilarProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SimilarProductAddPopup(string productId)
    {
        var model = await _productViewModelService.PrepareSimilarProductModel();
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductAddPopupList(DataSourceRequest command,
        ProductModel.AddSimilarProductModel model)
    {
        var (products, totalCount) =
            await _productViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductAddPopup(ProductModel.AddSimilarProductModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await _productViewModelService.InsertSimilarProductModel(model);
            return Content("");
        }

        return Content(ModelState.GetErrors());
    }

    #endregion

    #region Bundle products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> BundleProductList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var bundleProducts = product.BundleProducts.OrderBy(x => x.DisplayOrder);
        var bundleProductsModel = new List<ProductModel.BundleProductModel>();
        foreach (var x in bundleProducts)
            bundleProductsModel.Add(new ProductModel.BundleProductModel {
                Id = x.Id,
                ProductBundleId = productId,
                ProductId = x.ProductId,
                ProductName = (await _productService.GetProductById(x.ProductId))?.Name,
                DisplayOrder = x.DisplayOrder,
                Quantity = x.Quantity
            });

        var gridModel = new DataSourceResult {
            Data = bundleProductsModel,
            Total = bundleProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BundleProductUpdate(ProductModel.BundleProductModel model)
    {
        if (ModelState.IsValid)
        {
            await _productViewModelService.UpdateBundleProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BundleProductDelete(ProductModel.BundleProductModel model)
    {
        if (ModelState.IsValid)
        {
            await _productViewModelService.DeleteBundleProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> BundleProductAddPopup(string productId)
    {
        var model = await _productViewModelService.PrepareBundleProductModel();
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BundleProductAddPopupList(DataSourceRequest command,
        ProductModel.AddBundleProductModel model)
    {
        var (products, totalCount) =
            await _productViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BundleProductAddPopup(ProductModel.AddBundleProductModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await _productViewModelService.InsertBundleProductModel(model);

            return Content("");
        }

        return Content(ModelState.GetErrors());
    }

    #endregion

    #region Cross-sell products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> CrossSellProductList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var crossSellProducts = product.CrossSellProduct;
        var crossSellProductsModel = new List<ProductModel.CrossSellProductModel>();
        foreach (var x in crossSellProducts)
            crossSellProductsModel.Add(new ProductModel.CrossSellProductModel {
                Id = x,
                ProductId = product.Id,
                Product2Name = (await _productService.GetProductById(x))?.Name
            });

        var gridModel = new DataSourceResult {
            Data = crossSellProductsModel,
            Total = crossSellProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CrossSellProductDelete(ProductModel.CrossSellProductModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await _productService.GetProductById(model.ProductId);

            var crossSellProduct = product.CrossSellProduct.FirstOrDefault(x => x == model.Id);
            if (string.IsNullOrEmpty(crossSellProduct))
                throw new ArgumentException("No cross-sell product found with the specified id");

            await _productViewModelService.DeleteCrossSellProduct(product.Id, crossSellProduct);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CrossSellProductAddPopup(string productId)
    {
        var model = await _productViewModelService.PrepareCrossSellProductModel();
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CrossSellProductAddPopupList(DataSourceRequest command,
        ProductModel.AddCrossSellProductModel model)
    {
        var (products, totalCount) =
            await _productViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CrossSellProductAddPopup(ProductModel.AddCrossSellProductModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await _productViewModelService.InsertCrossSellProductModel(model);
            return Content("");
        }

        return Content(ModelState.GetErrors());
    }

    #endregion

    #region Recommended products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> RecommendedProductList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var recommendedProductsModel = new List<ProductModel.RecommendedProductModel>();
        foreach (var x in product.RecommendedProduct)
            recommendedProductsModel.Add(new ProductModel.RecommendedProductModel {
                Id = x,
                ProductId = product.Id,
                Product2Name = (await _productService.GetProductById(x))?.Name
            });

        var gridModel = new DataSourceResult {
            Data = recommendedProductsModel,
            Total = recommendedProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RecommendedProductDelete(ProductModel.RecommendedProductModel model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null) throw new ArgumentException("Product not exists");

        var recommendedProduct = product.RecommendedProduct.FirstOrDefault(x => x == model.Id);
        if (string.IsNullOrEmpty(recommendedProduct))
            throw new ArgumentException("No recommended product found with the specified id");

        if (ModelState.IsValid)
        {
            await _productViewModelService.DeleteRecommendedProduct(product.Id, recommendedProduct);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> RecommendedProductAddPopup(string productId)
    {
        var model = await _productViewModelService.PrepareRecommendedProductModel();
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RecommendedProductAddPopupList(DataSourceRequest command,
        ProductModel.AddRecommendedProductModel model)
    {
        var (products, totalCount) =
            await _productViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RecommendedProductAddPopup(ProductModel.AddRecommendedProductModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await _productViewModelService.InsertRecommendedProductModel(model);
            return Content("");
        }

        return Content(ModelState.GetErrors());
    }

    #endregion

    #region Associated products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var vendorId = "";
        if (_workContext.CurrentVendor != null) vendorId = _workContext.CurrentVendor.Id;

        var associatedProducts = await _productService.GetAssociatedProducts(productId,
            vendorId: vendorId,
            showHidden: true);
        var associatedProductsModel = associatedProducts
            .Select(x => new ProductModel.AssociatedProductModel {
                Id = x.Id,
                ProductId = productId,
                ProductName = x.Name,
                DisplayOrder = x.DisplayOrder
            })
            .ToList();

        var gridModel = new DataSourceResult {
            Data = associatedProductsModel,
            Total = associatedProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductUpdate(ProductModel.AssociatedProductModel model)
    {
        if (ModelState.IsValid)
        {
            var associatedProduct = await _productService.GetProductById(model.Id);
            if (associatedProduct == null || associatedProduct.VendorId != _workContext.CurrentVendor.Id)
                throw new ArgumentException("No associated product found with the specified id");

            associatedProduct.DisplayOrder = model.DisplayOrder;
            await _productService.UpdateAssociatedProduct(associatedProduct);

            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductDelete(ProductModel.AssociatedProductModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await _productService.GetProductById(model.Id);
            if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
                throw new ArgumentException("No associated product found with the specified id");

            await _productViewModelService.DeleteAssociatedProduct(product);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AssociatedProductAddPopup(string productId)
    {
        var model = await _productViewModelService.PrepareAssociatedProductModel();
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductAddPopupList(DataSourceRequest command,
        ProductModel.AddAssociatedProductModel model)
    {
        var (products, totalCount) =
            await _productViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductAddPopup(ProductModel.AddAssociatedProductModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await _productViewModelService.InsertAssociatedProductModel(model);

            return Content("");
        }

        Error(ModelState);
        model = await _productViewModelService.PrepareAssociatedProductModel();
        model.ProductId = model.ProductId;
        return View(model);
    }

    #endregion

    #region Product pictures

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ProductPictureAdd(Reference reference, string objectId,
        [FromServices] IPictureService pictureService,
        [FromServices] MediaSettings mediaSettings)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Pictures))
            return Json(new {
                success = false,
                message = "Access denied - picture permissions"
            });

        if (reference != Reference.Product || string.IsNullOrEmpty(objectId))
            return Json(new {
                success = false,
                message = "Please save form before upload new pictures"
            });

        var form = await HttpContext.Request.ReadFormAsync();
        var httpPostedFiles = form.Files.ToList();
        if (!httpPostedFiles.Any())
            return Json(new {
                success = false,
                message = "No files uploaded"
            });

        var product = await _productService.GetProductById(objectId);

        //a vendor should have access only to his products
        if (product.VendorId != _workContext.CurrentVendor.Id)
            return Json(new {
                success = false,
                message = "Access denied - vendor permissions"
            });

        var values = new List<(string pictureUrl, string pictureId)>();
        foreach (var file in httpPostedFiles)
        {
            var qqFileNameParameter = "qqfilename";
            var fileName = file.FileName;
            if (string.IsNullOrEmpty(fileName) && form.ContainsKey(qqFileNameParameter))
                fileName = form[qqFileNameParameter].ToString();

            fileName = Path.GetFileName(fileName);

            var contentType = file.ContentType;
            var fileExtension = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();

            if (string.IsNullOrEmpty(contentType))
                _ = new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);

            if (FileExtensions.GetAllowedMediaFileTypes(mediaSettings.AllowedFileTypes).Contains(fileExtension))
            {
                var fileBinary = file.GetDownloadBits();
                //insert picture
                var picture = await pictureService.InsertPicture(fileBinary, contentType, null,
                    reference: reference, objectId: objectId);
                var pictureUrl = await pictureService.GetPictureUrl(picture);

                values.Add((pictureUrl, picture.Id));
                //assign picture to the product
                await _productViewModelService.InsertProductPicture(product, picture, 0);
            }
        }

        return Json(new { success = values.Any(), data = values });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductPictureList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var productPicturesModel = await _productViewModelService.PrepareProductPicturesModel(product);
        var gridModel = new DataSourceResult {
            Data = productPicturesModel,
            Total = productPicturesModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> ProductPicturePopup(string productId, string id)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            return Content("Product not exist");

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return Content(permission.message);

        var pp = product.ProductPictures.FirstOrDefault(x => x.Id == id);
        if (pp == null)
            return Content("Product picture not exist");

        var (model, picture) = await _productViewModelService.PrepareProductPictureModel(product, pp);
        //locales
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
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
            var product = await _productService.GetProductById(model.ProductId);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            if (product.ProductPictures.FirstOrDefault(x => x.Id == model.Id) == null)
                throw new ArgumentException("No product picture found with the specified id");

            await _productViewModelService.UpdateProductPicture(model);

            return Content("");
        }

        Error(ModelState);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPictureDelete(ProductModel.ProductPictureModel model)
    {
        if (ModelState.IsValid)
        {
            await _productViewModelService.DeleteProductPicture(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Product specification attributes

    //ajax
    [AcceptVerbs("GET")]
    public async Task<IActionResult> GetOptionsByAttributeId(string attributeId,
        [FromServices] ISpecificationAttributeService specificationAttributeService)
    {
        if (string.IsNullOrEmpty(attributeId))
            return Json("");

        var options =
            (await specificationAttributeService.GetSpecificationAttributeById(attributeId))
            .SpecificationAttributeOptions.OrderBy(x => x.DisplayOrder);
        var result = (from o in options
            select new { id = o.Id, name = o.Name }).ToList();
        return Json(result);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductSpecAttrList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var productSpecsModel = await _productViewModelService.PrepareProductSpecificationAttributeModel(product);
        var gridModel = new DataSourceResult {
            Data = productSpecsModel,
            Total = productSpecsModel.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductSpecAttrPopup(
        [FromServices] ISpecificationAttributeService specificationAttributeService,
        string productId, string id)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return Content(permission.message);

        var model = new ProductModel.AddProductSpecificationAttributeModel {
            //default specs values
            ShowOnProductPage = true
        };

        if (!string.IsNullOrEmpty(id))
        {
            var specification = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == id);
            if (specification != null) model = specification.ToModel();
        }

        model.AvailableAttributes = await PrepareAvailableAttributes(specificationAttributeService);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductSpecAttrPopup(
        [FromServices] ISpecificationAttributeService specificationAttributeService,
        ProductModel.AddProductSpecificationAttributeModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await _productService.GetProductById(model.ProductId);

            var psa = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == model.Id);
            if (psa == null)
                await _productViewModelService.InsertProductSpecificationAttributeModel(model, product);
            else
                await _productViewModelService.UpdateProductSpecificationAttributeModel(psa, model);

            return new JsonResult("");
        }

        Error(ModelState);
        model.AvailableAttributes = await PrepareAvailableAttributes(specificationAttributeService);

        return View(model);
    }

    private async Task<List<SelectListItem>> PrepareAvailableAttributes(
        ISpecificationAttributeService specificationAttributeService)
    {
        return (await specificationAttributeService.GetSpecificationAttributes())
            .Select(sa => new SelectListItem { Text = sa.Name, Value = sa.Id }).ToList();
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductSpecAttrDelete(ProductSpecificationAttributeModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await _productService.GetProductById(model.ProductId);

            var psa = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == model.Id);
            if (psa == null)
                throw new ArgumentException("No specification attribute found with the specified id");

            await _productViewModelService.DeleteProductSpecificationAttribute(product, psa);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Purchased with order

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> PurchasedWithOrders(DataSourceRequest command, string productId,
        [FromServices] IOrderViewModelService orderViewModelService)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Json(new DataSourceResult {
                Data = null,
                Total = 0
            });

        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var model = new OrderListModel {
            ProductId = productId
        };

        var (orderModels, totalCount) =
            await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = orderModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    #endregion

    #region Reviews

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> Reviews(DataSourceRequest command, string productId,
        [FromServices] IProductReviewService productReviewService)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var productReviews = await productReviewService.GetAllProductReviews("", null,
            null, null, "", "", productId);

        var items = new List<ProductReviewModel>();
        foreach (var item in productReviews.PagedForCommand(command))
        {
            var m = new ProductReviewModel();
            await _productViewModelService.PrepareProductReviewModel(m, item, false, true);
            items.Add(m);
        }

        var gridModel = new DataSourceResult {
            Data = items,
            Total = productReviews.Count
        };

        return Json(gridModel);
    }

    #endregion

    #region Export / Import

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelAll(ProductListModel model,
        [FromServices] IExportManager<Product> exportManager)
    {
        var products = await _productViewModelService.PrepareProducts(model);
        try
        {
            var bytes = await exportManager.Export(products);
            return File(bytes, "text/xls", "products.xlsx");
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelSelected(string selectedIds,
        [FromServices] IExportManager<Product> exportManager)
    {
        var products = new List<Product>();
        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x)
                .ToArray();
            products.AddRange(await _productService.GetProductsByIds(ids, true));
        }

        //a vendor should have access only to his products
        products = products.Where(p => p.VendorId == _workContext.CurrentVendor.Id).ToList();

        var bytes = await exportManager.Export(products);
        return File(bytes, "text/xls", "products.xlsx");
    }

    #endregion

    #region Bulk editing

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> BulkEdit()
    {
        var model = await _productViewModelService.PrepareBulkEditListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BulkEditSelect(DataSourceRequest command, BulkEditListModel model)
    {
        var (bulkEditProductModels, totalCount) =
            await _productViewModelService.PrepareBulkEditProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = bulkEditProductModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BulkEditUpdate(IEnumerable<BulkEditProductModel> products)
    {
        if (products != null) await _productViewModelService.UpdateBulkEdit(products.ToList());

        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> BulkEditDelete(IEnumerable<BulkEditProductModel> products)
    {
        if (products != null) await _productViewModelService.DeleteBulkEdit(products.ToList());

        return new JsonResult("");
    }

    #endregion

    #region Product currency price

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductPriceList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var items = new List<ProductModel.ProductPriceModel>();
        foreach (var item in product.ProductPrices)
            items.Add(new ProductModel.ProductPriceModel {
                Id = item.Id,
                CurrencyCode = item.CurrencyCode,
                Price = item.Price
            });

        var gridModel = new DataSourceResult {
            Data = items,
            Total = items.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPriceInsert(string productId, ProductModel.ProductPriceModel model)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        if (product.ProductPrices.Any(x => x.CurrencyCode == model.CurrencyCode))
            throw new ArgumentException("Currency code exists");

        if (ModelState.IsValid)
            try
            {
                await _productService.InsertProductPrice(new ProductPrice {
                    ProductId = product.Id,
                    CurrencyCode = model.CurrencyCode,
                    Price = model.Price
                });
                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPriceUpdate(string productId, ProductModel.ProductPriceModel model)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var productPrice = product.ProductPrices.FirstOrDefault(x => x.Id == model.Id);
        if (productPrice == null)
            throw new ArgumentException("Product price model not exists");

        if (product.ProductPrices.Any(x => x.Id != model.Id && x.CurrencyCode == model.CurrencyCode))
            throw new ArgumentException("You can't use this currency code");

        if (ModelState.IsValid)
            try
            {
                productPrice!.CurrencyCode = model.CurrencyCode;
                productPrice.Price = model.Price;
                productPrice.ProductId = productId;

                await _productService.UpdateProductPrice(productPrice);

                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPriceDelete(string productId, ProductModel.ProductPriceModel model)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var productPrice = product.ProductPrices.FirstOrDefault(x => x.Id == model.Id);
        if (productPrice == null)
            throw new ArgumentException("Product price model not exists");

        if (ModelState.IsValid)
        {
            productPrice!.ProductId = productId;
            await _productService.DeleteProductPrice(productPrice);

            return new JsonResult("");
        }

        return Content(ModelState.GetErrors());
    }

    #endregion

    #region Tier prices

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> TierPriceList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var tierPricesModel = await _productViewModelService.PrepareTierPriceModel(product);
        var gridModel = new DataSourceResult {
            Data = tierPricesModel,
            Total = tierPricesModel.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> TierPriceCreatePopup(string productId)
    {
        var model = new ProductModel.TierPriceModel {
            ProductId = productId
        };
        await _productViewModelService.PrepareTierPriceModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> TierPriceCreatePopup(ProductModel.TierPriceModel model)
    {
        if (ModelState.IsValid)
        {
            var tierPrice = model.ToEntity(_dateTimeService);
            await _productService.InsertTierPrice(tierPrice, model.ProductId);

            return Content("");
        }

        Error(ModelState);
        //If we got this far, something failed, redisplay form
        await _productViewModelService.PrepareTierPriceModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> TierPriceEditPopup(string id, string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var tierPrice = product.TierPrices.FirstOrDefault(x => x.Id == id);
        if (tierPrice == null)
            return Content("Empty tier price");

        //a vendor should have access only to his products
        if (product.VendorId != _workContext.CurrentVendor.Id)
            return Content("This is not your product");

        var model = tierPrice.ToModel(_dateTimeService);
        model.ProductId = productId;
        await _productViewModelService.PrepareTierPriceModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> TierPriceEditPopup(string productId, ProductModel.TierPriceModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await _productService.GetProductById(productId, true);

            var tierPrice = product.TierPrices.FirstOrDefault(x => x.Id == model.Id);
            if (tierPrice == null)
                return Content("Empty tier price");

            tierPrice = model.ToEntity(tierPrice, _dateTimeService);
            await _productService.UpdateTierPrice(tierPrice, product.Id);

            return Content("");
        }

        Error(ModelState);
        //stores
        await _productViewModelService.PrepareTierPriceModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> TierPriceDelete(ProductModel.TierPriceDeleteModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await _productService.GetProductById(model.ProductId, true);
            var tierPrice = product.TierPrices.FirstOrDefault(x => x.Id == model.Id);
            if (tierPrice == null)
                throw new ArgumentException("No tier price found with the specified id");

            await _productService.DeleteTierPrice(tierPrice, product.Id);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Product attributes

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeMappingList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var attributesModel = await _productViewModelService.PrepareProductAttributeMappingModels(product);
        var gridModel = new DataSourceResult {
            Data = attributesModel,
            Total = attributesModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeMappingPopup(string productId,
        string productAttributeMappingId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return Content(permission.message);

        if (string.IsNullOrEmpty(productAttributeMappingId))
        {
            var model = await _productViewModelService.PrepareProductAttributeMappingModel(product);
            return View(model);
        }
        else
        {
            var productAttributeMapping =
                product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
            var model = await _productViewModelService.PrepareProductAttributeMappingModel(productAttributeMapping);
            return View(model);
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeMappingPopup(ProductModel.ProductAttributeMappingModel model)
    {
        if (ModelState.IsValid)
        {
            if (string.IsNullOrEmpty(model.Id))
                await _productViewModelService.InsertProductAttributeMappingModel(model);
            else
                await _productViewModelService.UpdateProductAttributeMappingModel(model);

            return Content("");
        }

        Error(ModelState);
        model = await _productViewModelService.PrepareProductAttributeMappingModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeMappingDelete(string id, string productId,
        [FromServices] IProductAttributeService productAttributeService)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var productAttributeMapping = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == id);
        if (productAttributeMapping == null)
            throw new ArgumentException("No product attribute mapping found with the specified id");

        //a vendor should have access only to his products
        if (product.VendorId != _workContext.CurrentVendor.Id)
            return Content("This is not your product");

        await productAttributeService.DeleteProductAttributeMapping(productAttributeMapping, product.Id);
        return new JsonResult("");
    }

    //edit
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeValidationRulesPopup(string id, string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return Content(permission.message);

        var productAttributeMapping = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == id);
        if (productAttributeMapping == null)
            return Content("No attribute value found with the specified id");


        var model = await _productViewModelService.PrepareProductAttributeMappingModel(productAttributeMapping);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeValidationRulesPopup(
        ProductModel.ProductAttributeMappingModel model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var productAttributeMapping = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == model.Id);
        if (productAttributeMapping == null)
            throw new ArgumentException("No attribute value found with the specified id");

        if (ModelState.IsValid)
        {
            await _productViewModelService.UpdateProductAttributeValidationRulesModel(productAttributeMapping,
                model);
            return Content("");
        }

        Error(ModelState);
        model = await _productViewModelService.PrepareProductAttributeMappingModel(productAttributeMapping);
        return View(model);
    }

    #endregion

    #region Product attributes. Condition

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeConditionPopup(string productId,
        string productAttributeMappingId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return Content(permission.message);

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        if (productAttributeMapping == null)
            //No attribute value found with the specified id
            return Content("No attribute value found with the specified id");

        var model = await _productViewModelService.PrepareProductAttributeConditionModel(product,
            productAttributeMapping);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeConditionPopup(ProductAttributeConditionModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await _productService.GetProductById(model.ProductId);

            var productAttributeMapping =
                product.ProductAttributeMappings.FirstOrDefault(x => x.Id == model.ProductAttributeMappingId);
            if (productAttributeMapping == null)
                return Content("No attribute value found with the specified id");

            await _productViewModelService.UpdateProductAttributeConditionModel(product, productAttributeMapping,
                model);
        }

        return Content(ModelState.GetErrors());
    }

    #endregion

    #region Product attribute values

    //list
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> EditAttributeValues(string productAttributeMappingId, string productId,
        [FromServices] IProductAttributeService productAttributeService)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        if (productAttributeMapping == null)
            throw new ArgumentException("No product attribute mapping found with the specified id");

        var productAttribute =
            await productAttributeService.GetProductAttributeById(productAttributeMapping.ProductAttributeId);
        var model = new ProductModel.ProductAttributeValueListModel {
            ProductName = product.Name,
            ProductId = product.Id,
            ProductAttributeName = productAttribute.Name,
            ProductAttributeMappingId = productAttributeMappingId
        };

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeValueList(string productAttributeMappingId, string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        if (productAttributeMapping == null)
            throw new ArgumentException("No product attribute mapping found with the specified id");

        var values =
            await _productViewModelService.PrepareProductAttributeValueModels(product, productAttributeMapping);
        var gridModel = new DataSourceResult {
            Data = values,
            Total = values.Count
        };
        return Json(gridModel);
    }

    //create
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeValueCreatePopup(string productAttributeMappingId,
        string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return Content(permission.message);

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        if (productAttributeMapping == null)
            throw new ArgumentException("No product attribute mapping found with the specified id");

        var model = await _productViewModelService.PrepareProductAttributeValueModel(product,
            productAttributeMapping);
        //locales
        await AddLocales(_languageService, model.Locales);

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeValueCreatePopup(ProductModel.ProductAttributeValueModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await _productService.GetProductById(model.ProductId);

            var productAttributeMapping =
                product.ProductAttributeMappings.FirstOrDefault(x => x.Id == model.ProductAttributeMappingId);
            if (productAttributeMapping == null)
                //No product attribute found with the specified id
                return RedirectToAction("List", "Product");


            await _productViewModelService.InsertProductAttributeValueModel(model);
            return Content("");
        }

        return Content("");
    }

    //edit
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeValueEditPopup(string id, string productId,
        string productAttributeMappingId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var pa = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        if (pa == null)
            return RedirectToAction("List", "Product");

        var pav = pa.ProductAttributeValues.FirstOrDefault(x => x.Id == id);
        if (pav == null)
            //No attribute value found with the specified id
            return RedirectToAction("List", "Product");

        var model = await _productViewModelService.PrepareProductAttributeValueModel(pa, pav);
        //locales
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = pav.GetTranslation(x => x.Name, languageId, false);
        });
        //pictures
        await _productViewModelService.PrepareProductAttributeValueModel(product, model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeValueEditPopup(string productId,
        ProductModel.ProductAttributeValueModel model)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var pav = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == model.ProductAttributeMappingId)
            ?.ProductAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (pav == null)
            //No attribute value found with the specified id
            return RedirectToAction("List", "Product");

        if (ModelState.IsValid)
        {
            await _productViewModelService.UpdateProductAttributeValueModel(pav, model);
            return Content("");
        }

        //If we got this far, something failed, redisplay form
        await _productViewModelService.PrepareProductAttributeValueModel(product, model);
        return View(model);
    }

    //delete
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeValueDelete(string id, string pam, string productId,
        [FromServices] IProductAttributeService productAttributeService)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var pav = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == pam)?.ProductAttributeValues
            .FirstOrDefault(x => x.Id == id);
        if (pav == null)
            throw new ArgumentException("No product attribute value found with the specified id");

        if (ModelState.IsValid)
        {
            await productAttributeService.DeleteProductAttributeValue(pav, productId, pam);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    public async Task<IActionResult> AssociateProductToAttributeValuePopup()
    {
        var model = await _productViewModelService.PrepareAssociateProductToAttributeValueModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociateProductToAttributeValuePopupList(DataSourceRequest command,
        ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel model)
    {
        var (products, totalCount) =
            await _productViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociateProductToAttributeValuePopup(
        ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel model)
    {
        var associatedProduct = await _productService.GetProductById(model.AssociatedToProductId);
        if (associatedProduct == null || associatedProduct.VendorId != _workContext.CurrentVendor.Id)
            return Content("Cannot load a product");

        return Content("");
    }

    #endregion

    #region Product attribute combinations

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationList(string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var combinationsModel = await _productViewModelService.PrepareProductAttributeCombinationModel(product);
        var gridModel = new DataSourceResult {
            Data = combinationsModel,
            Total = combinationsModel.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationDelete(string id, string productId,
        [FromServices] IProductAttributeService productAttributeService)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var combination = product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == id);
        if (combination == null)
            throw new ArgumentException("No product attribute combination found with the specified id");

        await productAttributeService.DeleteProductAttributeCombination(combination, productId);

        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
        {
            var pr = await _productService.GetProductById(productId);
            pr.StockQuantity = pr.ProductAttributeCombinations.Sum(x => x.StockQuantity);
            pr.ReservedQuantity = pr.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);
            await _inventoryManageService.UpdateStockProduct(pr, false);
        }

        return new JsonResult("");
    }

    //edit
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AttributeCombinationPopup(string productId, string id)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return Content(permission.message);

        var model = await _productViewModelService.PrepareProductAttributeCombinationModel(product, id);
        await _productViewModelService.PrepareAddProductAttributeCombinationModel(model, product);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AttributeCombinationPopup(string productId,
        ProductAttributeCombinationModel model)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            //No product found with the specified id
            return RedirectToAction("List", "Product");

        var warnings =
            await _productViewModelService.InsertOrUpdateProductAttributeCombinationPopup(product, model);
        if (!warnings.Any()) return Content("");

        //If we got this far, something failed, redisplay form
        await _productViewModelService.PrepareAddProductAttributeCombinationModel(model, product);
        model.Warnings = warnings;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> GenerateAllAttributeCombinations(string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        await _productViewModelService.GenerateAllAttributeCombinations(product);

        return Json(new { Success = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ClearAllAttributeCombinations(string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        if (ModelState.IsValid)
        {
            await _productViewModelService.ClearAllAttributeCombinations(product);

            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            {
                product.StockQuantity = 0;
                product.ReservedQuantity = 0;
                await _inventoryManageService.UpdateStockProduct(product, false);
            }

            return Json(new { Success = true });
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #region Product Attribute combination - tier prices

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationTierPriceList(string productId,
        string productAttributeCombinationId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var tierPriceModel =
            await _productViewModelService.PrepareProductAttributeCombinationTierPricesModel(product,
                productAttributeCombinationId);
        var gridModel = new DataSourceResult {
            Data = tierPriceModel,
            Total = tierPriceModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationTierPriceInsert(
        ProductModel.ProductAttributeCombinationTierPricesModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await _productService.GetProductById(model.ProductId);
            var combination =
                product.ProductAttributeCombinations.FirstOrDefault(
                    x => x.Id == model.ProductAttributeCombinationId);
            if (combination != null)
                await _productViewModelService.InsertProductAttributeCombinationTierPricesModel(product,
                    combination, model);

            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationTierPriceUpdate(
        ProductModel.ProductAttributeCombinationTierPricesModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await _productService.GetProductById(model.ProductId);
            var combination =
                product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == model.ProductAttributeCombinationId);
            if (combination != null)
                await _productViewModelService.UpdateProductAttributeCombinationTierPricesModel(product, combination,
                    model);

            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationTierPriceDelete(string productId,
        string productAttributeCombinationId, string id)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var combination =
            product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == productAttributeCombinationId);
        if (combination != null)
        {
            var tierPrice = combination.TierPrices.FirstOrDefault(x => x.Id == id);
            if (tierPrice != null)
                await _productViewModelService.DeleteProductAttributeCombinationTierPrices(product, combination,
                    tierPrice);
        }

        return new JsonResult("");
    }

    #endregion

    #endregion

    #region Reservation

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ListReservations(DataSourceRequest command, string productId)
    {
        var product = await _productService.GetProductById(productId);

        var permission = await CheckAccessToProduct(product);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var reservations =
            await _productReservationService.GetProductReservationsByProductId(productId, null, null,
                command.Page - 1, command.PageSize);
        var reservationModel = reservations
            .Select(x => new ProductModel.ReservationModel {
                ReservationId = x.Id,
                Date = x.Date,
                OrderId = x.OrderId,
                ProductId = x.ProductId,
                Parameter = x.Parameter,
                Resource = x.Resource,
                Duration = x.Duration
            }).ToList();

        var gridModel = new DataSourceResult {
            Data = reservationModel,
            Total = reservations.TotalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> GenerateCalendar(ProductModel.GenerateCalendarModel model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var reservations =
            await _productReservationService.GetProductReservationsByProductId(model.ProductId, null, null);
        if (reservations.Any())
            if ((product.IntervalUnitId is IntervalUnit.Minute or IntervalUnit.Hour &&
                 (IntervalUnit)model.Interval == IntervalUnit.Day) ||
                (product.IntervalUnitId == IntervalUnit.Day &&
                 ((IntervalUnit)model.IntervalUnit == IntervalUnit.Minute ||
                  (IntervalUnit)model.IntervalUnit == IntervalUnit.Hour)))
                return Json(new {
                    errors = _translationService.GetResource(
                        "Vendor.Catalog.Products.Calendar.CannotChangeInterval")
                });

        if (!ModelState.IsValid)
        {
            var error =
                (Dictionary<string, Dictionary<string, object>>)ModelState.SerializeErrors();
            var s = "";
            foreach (var error1 in error)
            foreach (var error2 in error1.Value)
            {
                var v = (string[])error2.Value;
                s += v[0] + "\n";
            }

            return Json(new { errors = s });
        }

        //update fields on product
        await _productService.UpdateProductField(product, x => x.Interval, model.Interval);
        await _productService.UpdateProductField(product, x => x.IntervalUnitId, (IntervalUnit)model.IntervalUnit);
        await _productService.UpdateProductField(product, x => x.IncBothDate, model.IncBothDate);

        var minutesToAdd = (IntervalUnit)model.IntervalUnit switch {
            IntervalUnit.Minute => model.Interval,
            IntervalUnit.Hour => model.Interval * 60,
            IntervalUnit.Day => model.Interval * 60 * 24,
            _ => 0
        };

        var _hourFrom = model.StartTime.Hour;
        var _minutesFrom = model.StartTime.Minute;
        var _hourTo = model.EndTime.Hour;
        var _minutesTo = model.EndTime.Minute;
        var _dateFrom = new DateTime(model.StartDate.Value.Year, model.StartDate.Value.Month,
            model.StartDate.Value.Day, 0, 0, 0, 0);
        var _dateTo = new DateTime(model.EndDate.Value.Year, model.EndDate.Value.Month,
            model.EndDate.Value.Day, 23, 59, 59, 999);
        if ((IntervalUnit)model.IntervalUnit == IntervalUnit.Day)
        {
            model.Quantity = 1;
            model.Parameter = "";
        }
        else
        {
            model.Resource = "";
        }

        var dates = new List<DateTime>();
        var counter = 0;
        for (var iterator = _dateFrom; iterator <= _dateTo; iterator += new TimeSpan(0, minutesToAdd, 0))
        {
            if ((IntervalUnit)model.IntervalUnit != IntervalUnit.Day)
            {
                if (iterator.Hour >= _hourFrom && iterator.Hour <= _hourTo)
                {
                    if (iterator.Hour == _hourTo)
                        if (iterator.Minute > _minutesTo)
                            continue;

                    if (iterator.Hour == _hourFrom)
                        if (iterator.Minute < _minutesFrom)
                            continue;
                }
                else
                {
                    continue;
                }
            }

            if ((iterator.DayOfWeek == DayOfWeek.Monday && !model.Monday) ||
                (iterator.DayOfWeek == DayOfWeek.Tuesday && !model.Tuesday) ||
                (iterator.DayOfWeek == DayOfWeek.Wednesday && !model.Wednesday) ||
                (iterator.DayOfWeek == DayOfWeek.Thursday && !model.Thursday) ||
                (iterator.DayOfWeek == DayOfWeek.Friday && !model.Friday) ||
                (iterator.DayOfWeek == DayOfWeek.Saturday && !model.Saturday) ||
                (iterator.DayOfWeek == DayOfWeek.Sunday && !model.Sunday))
                continue;

            for (var i = 0; i < model.Quantity; i++)
            {
                dates.Add(iterator);
                try
                {
                    var insert = true;
                    if ((IntervalUnit)model.IntervalUnit == IntervalUnit.Day)
                        if (reservations.Any(x => x.Resource == model.Resource && x.Date == iterator))
                            insert = false;

                    if (insert)
                    {
                        if (counter++ > 1000)
                            break;

                        await _productReservationService.InsertProductReservation(new ProductReservation {
                            OrderId = "",
                            Date = iterator,
                            ProductId = model.ProductId,
                            Resource = model.Resource,
                            Parameter = model.Parameter,
                            Duration = model.Interval + " " +
                                       ((IntervalUnit)model.IntervalUnit).GetTranslationEnum(_translationService,
                                           _workContext)
                        });
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        return Json(new { success = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ClearCalendar(string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var toDelete = await _productReservationService.GetProductReservationsByProductId(productId, true, null);
        foreach (var record in toDelete) await _productReservationService.DeleteProductReservation(record);

        return Json("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ClearOld(string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var toDelete =
            (await _productReservationService.GetProductReservationsByProductId(productId, true, null)).Where(x =>
                x.Date < DateTime.UtcNow);
        foreach (var record in toDelete) await _productReservationService.DeleteProductReservation(record);

        return Json("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductReservationDelete(ProductModel.ReservationModel model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var toDelete = await _productReservationService.GetProductReservation(model.ReservationId);
        if (toDelete != null)
        {
            if (string.IsNullOrEmpty(toDelete.OrderId))
                await _productReservationService.DeleteProductReservation(toDelete);
            else
                return Json(new DataSourceResult {
                    Errors = _translationService.GetResource(
                        "Vendor.Catalog.ProductReservations.CantDeleteWithOrder")
                });
        }

        return Json("");
    }

    #endregion

    #region Bids

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ListBids(DataSourceRequest command, string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var (bidModels, totalCount) =
            await _productViewModelService.PrepareBidMode(productId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = bidModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BidDelete(ProductModel.BidModel model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null || product.VendorId != _workContext.CurrentVendor.Id)
            throw new ArgumentException("No product found with the specified id");

        var toDelete = await _auctionService.GetBid(model.BidId);
        if (toDelete != null)
        {
            if (string.IsNullOrEmpty(toDelete.OrderId))
            {
                //delete bid
                await _auctionService.DeleteBid(toDelete);
                return Json("");
            }

            return Json(new DataSourceResult
                { Errors = _translationService.GetResource("Vendor.Catalog.Products.Bids.CantDeleteWithOrder") });
        }

        return Json(new DataSourceResult { Errors = "Bid not exists" });
    }

    #endregion

    #endregion
}