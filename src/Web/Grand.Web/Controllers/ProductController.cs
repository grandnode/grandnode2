using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Products;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Events;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Grand.SharedKernel.Attributes;
using Grand.Web.Models.Orders;
using Microsoft.AspNetCore.Http;
using Grand.SharedKernel.Extensions;

namespace Grand.Web.Controllers;

[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class ProductController : BasePublicController
{
    #region Constructors

    public ProductController(
        IProductService productService,
        IContextAccessor contextAccessor,
        ITranslationService translationService,
        IRecentlyViewedProductsService recentlyViewedProductsService,
        IShoppingCartService shoppingCartService,
        IAclService aclService,
        IPermissionService permissionService,
        IMediator mediator,
        CatalogSettings catalogSettings,
        CaptchaSettings captchaSettings
    )
    {
        _productService = productService;
        _contextAccessor = contextAccessor;
        _translationService = translationService;
        _recentlyViewedProductsService = recentlyViewedProductsService;
        _shoppingCartService = shoppingCartService;
        _aclService = aclService;
        _permissionService = permissionService;
        _mediator = mediator;
        _catalogSettings = catalogSettings;
        _captchaSettings = captchaSettings;
    }

    #endregion

    #region Recently viewed products

    [HttpGet]
    public virtual async Task<IActionResult> RecentlyViewedProducts()
    {
        if (!_catalogSettings.RecentlyViewedProductsEnabled)
            return Content("");

        var products = await _recentlyViewedProductsService.GetRecentlyViewedProducts(_contextAccessor.WorkContext.CurrentCustomer.Id,
            _catalogSettings.RecentlyViewedProductsNumber);

        //prepare model
        var model = await _mediator.Send(new GetProductOverview {
            Products = products
        });

        return View(model);
    }

    #endregion

    #region Related products

    [HttpGet]
    public virtual async Task<IActionResult> RelatedProducts(string productId, int? productThumbPictureSize)
    {
        var productIds = (await _productService.GetProductById(productId)).RelatedProducts.OrderBy(x => x.DisplayOrder)
            .Select(x => x.ProductId2).ToArray();

        //load products
        var products = await _productService.GetProductsByIds(productIds);

        var model = await _mediator.Send(new GetProductOverview {
            PreparePictureModel = true,
            PreparePriceModel = true,
            PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
            ProductThumbPictureSize = productThumbPictureSize,
            Products = products
        });

        return View(model);
    }

    #endregion


    #region Recently added products

    [HttpGet]
    public virtual async Task<IActionResult> NewProducts()
    {
        if (!_catalogSettings.NewProductsEnabled)
            return Content("");

        var products = (await _productService.SearchProducts(
            storeId: _contextAccessor.StoreContext.CurrentStore.Id,
            visibleIndividuallyOnly: true,
            markedAsNewOnly: true,
            orderBy: ProductSortingEnum.CreatedOn,
            pageSize: _catalogSettings.NewProductsNumber)).products;


        //prepare model
        var model = await _mediator.Send(new GetProductOverview {
            PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
            Products = products
        });

        return View(model);
    }

    #endregion

    #region Email a friend

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    public virtual async Task<IActionResult> ProductEmailAFriend(ProductEmailAFriendModel model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product is not { Published: true } || !_catalogSettings.EmailAFriendEnabled)
            return Content("");

        if (ModelState.IsValid)
        {
            await _mediator.Send(new SendProductEmailAFriendMessageCommand {
                Customer = _contextAccessor.WorkContext.CurrentCustomer,
                Product = product,
                Language = _contextAccessor.WorkContext.WorkingLanguage,
                Store = _contextAccessor.StoreContext.CurrentStore,
                Model = model
            });

            model.ProductId = product.Id;
            model.ProductName = product.GetTranslation(x => x.Name, _contextAccessor.WorkContext.WorkingLanguage.Id);
            model.ProductSeName = product.GetSeName(_contextAccessor.WorkContext.WorkingLanguage.Id);

            model.SuccessfullySent = true;
            model.Result = _translationService.GetResource("Products.EmailAFriend.SuccessfullySent");

            return Json(model);
        }

        //If we got this far, something failed, redisplay form
        model.ProductId = product.Id;
        model.ProductName = product.GetTranslation(x => x.Name, _contextAccessor.WorkContext.WorkingLanguage.Id);
        model.ProductSeName = product.GetSeName(_contextAccessor.WorkContext.WorkingLanguage.Id);
        model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnEmailProductToFriendPage;
        model.SuccessfullySent = false;
        model.Result = string.Join(",", ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage));

        return Json(model);
    }

    #endregion

    #region Ask question

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    public virtual async Task<IActionResult> AskQuestionOnProduct(ProductAskQuestionSimpleModel model)
    {
        var product = await _productService.GetProductById(model.Id);
        if (product is not { Published: true } || !_catalogSettings.AskQuestionOnProduct)
            return Json(new {
                success = false,
                message = "Product not found"
            });

        if (!ModelState.IsValid)

            return Json(new {
                success = false,
                message = string.Join(",", ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage))
            });

        var productaskqestionmodel = new ProductAskQuestionModel {
            Email = model.AskQuestionEmail,
            FullName = model.AskQuestionFullName,
            Phone = model.AskQuestionPhone,
            Message = model.AskQuestionMessage
        };

        // email
        await _mediator.Send(new SendProductAskQuestionMessageCommand {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Model = productaskqestionmodel,
            Product = product,
            RemoteIpAddress = HttpContext.Connection?.RemoteIpAddress?.ToString()
        });

        //return Json
        return Json(new {
            success = true,
            message = _translationService.GetResource("Products.AskQuestion.SuccessfullySent")
        });
    }

    #endregion

    #region Calendar

    [HttpPost]
    public virtual async Task<IActionResult> GetDatesForMonth(string productId, int month, string parameter, int year,
        [FromServices] IProductReservationService productReservationService)
    {
        var allReservations = await productReservationService.GetProductReservationsByProductId(productId, true, null);
        var query = allReservations.Where(
            x => x.Date.Month == month && x.Date.Year == year && x.Date >= DateTime.UtcNow);
        if (!string.IsNullOrEmpty(parameter)) query = query.Where(x => x.Parameter == parameter);

        var reservations = query.ToList();
        var inCart = (await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id))
            .Where(x => !string.IsNullOrEmpty(x.ReservationId)).ToList();
        foreach (var cartItem in inCart)
        {
            var match = reservations.FirstOrDefault(x => x.Id == cartItem.ReservationId);
            if (match != null) reservations.Remove(match);
        }

        var toReturn = reservations.GroupBy(x => x.Date).Select(x => x.First()).ToList();

        return Json(toReturn);
    }

    #endregion

    #region Fields

    private readonly IProductService _productService;
    private readonly IContextAccessor _contextAccessor;
    private readonly ITranslationService _translationService;
    private readonly IRecentlyViewedProductsService _recentlyViewedProductsService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IAclService _aclService;
    private readonly IPermissionService _permissionService;
    private readonly IMediator _mediator;
    private readonly CatalogSettings _catalogSettings;
    private readonly CaptchaSettings _captchaSettings;

    #endregion

    #region Product details page

    [HttpGet]
    public virtual async Task<ActionResult<ProductDetailsModel>> ProductDetails(string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            return NotFound();

        var customer = _contextAccessor.WorkContext.CurrentCustomer;

        //published?
        if (!_catalogSettings.AllowViewUnpublishedProductPage)
            //Check whether the current user has a "Manage catalog" permission
            //It allows him to preview a product before publishing
            if (!product.Published && !await _permissionService.Authorize(StandardPermission.ManageProducts, customer))
                return NotFound();

        //ACL (access control list)
        if (!_aclService.Authorize(product, customer))
            return NotFound();

        //Store access
        if (!_aclService.Authorize(product, _contextAccessor.StoreContext.CurrentStore.Id))
            return NotFound();

        //availability dates
        if (!product.IsAvailable() && product.ProductTypeId != ProductType.Auction)
            return NotFound();

        //visible individually?
        if (!product.VisibleIndividually)
        {
            //is this one an associated products?
            var parentGroupedProduct = await _productService.GetProductById(product.ParentGroupedProductId);
            return parentGroupedProduct == null
                ? RedirectToRoute("HomePage")
                : RedirectToRoute("Product",
                    new { SeName = parentGroupedProduct.GetSeName(_contextAccessor.WorkContext.WorkingLanguage.Id) });
        }

        //prepare the model
        var model = await _mediator.Send(new GetProductDetailsPage {
            Store = _contextAccessor.StoreContext.CurrentStore,
            Product = product,
            IsAssociatedProduct = false
        });

        //product layout
        var productLayoutViewPath = await _mediator.Send(new GetProductLayoutViewPath
            { ProductLayoutId = product.ProductLayoutId });

        //save as recently viewed
        await _recentlyViewedProductsService.AddProductToRecentlyViewedList(customer.Id, product.Id);

        //display "edit" (manage) link
        if (await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer) &&
            await _permissionService.Authorize(StandardPermission.ManageProducts, customer))
            //a vendor should have access only to his products
            if (_contextAccessor.WorkContext.CurrentVendor == null || _contextAccessor.WorkContext.CurrentVendor.Id == product.VendorId)
                DisplayEditLink(Url.Action("Edit", "Product", new { id = product.Id, area = "Admin" }));
        _ = _productService.IncrementProductField(product, x => x.Viewed, 1);

        return View(productLayoutViewPath, model);
    }

    //handle product attribute selection event. this way we return new price, overridden gtin/sku/mpn
    //currently we use this method on the product details pages
    [HttpPost]
    public virtual async Task<ActionResult<ProductDetailsModel>> ProductDetails_AttributeChange(ProductModel model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null)
            return new JsonResult("");

        var modelProduct = await _mediator.Send(new GetProductDetailsAttributeChange {
            Currency = _contextAccessor.WorkContext.WorkingCurrency,
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Model = model,
            Product = product
        });

        return Json(new {
            gtin = modelProduct.Gtin,
            mpn = modelProduct.Mpn,
            sku = modelProduct.Sku,
            price = modelProduct.Price,
            stockAvailability = modelProduct.StockAvailability,
            outOfStockSubscription = modelProduct.DisplayOutOfStockSubscription,
            buttonTextOutOfStockSubscription = modelProduct.ButtonTextOutOfStockSubscription,
            enabledattributemappingids = modelProduct.EnabledAttributeMappingIds.ToArray(),
            disabledattributemappingids = modelProduct.DisabledAttributeMappingids.ToArray(),
            notAvailableAttributeMappingids = modelProduct.NotAvailableAttributeMappingids.ToArray(),
            pictureFullSizeUrl = modelProduct.PictureFullSizeUrl,
            pictureDefaultSizeUrl = modelProduct.PictureDefaultSizeUrl
        });
    }

    //handle product warehouse selection event. this way we return stock
    [HttpPost]
    public virtual async Task<IActionResult> ProductDetails_WarehouseChange(ProductModel model,
        [FromServices] IStockQuantityService stockQuantityService)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null)
            return new JsonResult("");

        var stock = stockQuantityService.FormatStockMessage(product, model.WarehouseId, new List<CustomAttribute>());
        return Json(new {
            stockAvailability = string.Format(_translationService.GetResource(stock.resource), stock.arg0)
        });
    }

    [HttpPost]
    public virtual async Task<IActionResult> UploadFileProductAttribute(string attributeId, string productId, IFormFile file,
        [FromServices] IDownloadService downloadService)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            return Json(new {
                success = false,
                downloadGuid = Guid.Empty
            });
        var attribute = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == attributeId);
        if (attribute is not { AttributeControlTypeId: AttributeControlType.FileUpload })
            return Json(new {
                success = false,
                downloadGuid = Guid.Empty
            });

        if (file == null)
            return Json(new {
                success = false,
                message = "No file uploaded",
                downloadGuid = Guid.Empty
            });
        var fileName = file.FileName;
        var contentType = file.ContentType;
        var fileExtension = Path.GetExtension(fileName);

        if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
        {
            var allowedFileExtensions = attribute.ValidationFileAllowedExtensions.Split([','], StringSplitOptions.RemoveEmptyEntries);
            if (!allowedFileExtensions.IsAllowedMediaFileType(fileExtension))
                return Json(new {
                    success = false,
                    message = _translationService.GetResource("ShoppingCart.ValidationFileAllowed"),
                    downloadGuid = Guid.Empty
                });
        }
        var fileBinary = file.GetDownloadBits();

        if (attribute.ValidationFileMaximumSize.HasValue)
        {
            //compare in bytes
            var maxFileSizeBytes = attribute.ValidationFileMaximumSize.Value * 1024;
            if (fileBinary.Length > maxFileSizeBytes)
                //when returning JSON the mime-type must be set to text/plain
                //otherwise some browsers will pop-up a "Save As" dialog.
                return Json(new {
                    success = false,
                    message = string.Format(_translationService.GetResource("ShoppingCart.MaximumUploadedFileSize"),
                        attribute.ValidationFileMaximumSize.Value),
                    downloadGuid = Guid.Empty
                });
        }

        var download = new Download {
            DownloadGuid = Guid.NewGuid(),
            CustomerId = _contextAccessor.WorkContext.CurrentCustomer.Id,
            UseDownloadUrl = false,
            DownloadUrl = "",
            DownloadBinary = fileBinary,
            ContentType = contentType,
            Filename = Path.GetFileNameWithoutExtension(fileName),
            Extension = fileExtension,
            DownloadType = DownloadType.ProductAttribute,
            ReferenceId = attributeId
        };
        await downloadService.InsertDownload(download);

        //when returning JSON the mime-type must be set to text/plain
        //otherwise some browsers will pop-up a "Save As" dialog.
        return Json(new {
            success = true,
            message = _translationService.GetResource("ShoppingCart.FileUploaded"),
            downloadUrl = Url.Action("GetFileUpload", "Download", new { downloadId = download.DownloadGuid }),
            downloadGuid = download.DownloadGuid
        });
    }

    #region Quick view product

    [HttpGet]
    public virtual async Task<IActionResult> QuickView(string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            return Json(new {
                success = false,
                message = "No product found with the specified ID"
            });

        var customer = _contextAccessor.WorkContext.CurrentCustomer;

        //published?
        if (!_catalogSettings.AllowViewUnpublishedProductPage)
            //Check whether the current user has a "Manage catalog" permission
            //It allows him to preview a product before publishing
            if (!product.Published && !await _permissionService.Authorize(StandardPermission.ManageProducts, customer))
                return Json(new {
                    success = false,
                    message = "No product found with the specified ID"
                });

        //ACL (access control list)
        if (!_aclService.Authorize(product, customer))
            return Json(new {
                success = false,
                message = "No product found with the specified ID"
            });

        //Store access
        if (!_aclService.Authorize(product, _contextAccessor.StoreContext.CurrentStore.Id))
            return Json(new {
                success = false,
                message = "No product found with the specified ID"
            });

        //availability dates
        if (!product.IsAvailable() && product.ProductTypeId != ProductType.Auction)
            return Json(new {
                success = false,
                message = "No product found with the specified ID"
            });

        //visible individually?
        if (!product.VisibleIndividually)
        {
            //is this one an associated products?
            var parentGroupedProduct = await _productService.GetProductById(product.ParentGroupedProductId);
            if (parentGroupedProduct == null)
                return Json(new {
                    redirect = Url.RouteUrl("HomePage")
                });
            return Json(new {
                redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_contextAccessor.WorkContext.WorkingLanguage.Id) })
            });
        }

        //prepare the model
        var model = await _mediator.Send(new GetProductDetailsPage {
            Store = _contextAccessor.StoreContext.CurrentStore,
            Product = product,
            IsAssociatedProduct = false,
            UpdateCartItem = null
        });

        //product layout
        var productLayoutViewPath = await _mediator.Send(new GetProductLayoutViewPath
            { ProductLayoutId = product.ProductLayoutId });

        //save as recently viewed
        await _recentlyViewedProductsService.AddProductToRecentlyViewedList(customer.Id, product.Id);

        _ = _productService.IncrementProductField(product, x => x.Viewed, 1);

        return Json(new {
            success = true,
            product = true,
            model,
            layoutPath = productLayoutViewPath
        });
    }

    #endregion

    #endregion

    #region Product reviews

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    public virtual async Task<IActionResult> ProductReviews(
        ProductReviewsModel model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product is not { Published: true } || !product.AllowCustomerReviews)
            return Content("");

        if (ModelState.IsValid)
        {
            var productReview = await _mediator.Send(new InsertProductReviewCommand {
                Customer = _contextAccessor.WorkContext.CurrentCustomer,
                Store = _contextAccessor.StoreContext.CurrentStore,
                Model = model,
                Product = product
            });

            //notification
            await _mediator.Publish(new ProductReviewEvent(product, model.AddProductReview));

            //raise event
            if (productReview.IsApproved)
                await _mediator.Publish(new ProductReviewApprovedEvent(productReview));

            model = await _mediator.Send(new GetProductReviews {
                Customer = _contextAccessor.WorkContext.CurrentCustomer,
                Language = _contextAccessor.WorkContext.WorkingLanguage,
                Product = product,
                Store = _contextAccessor.StoreContext.CurrentStore,
                Size = _catalogSettings.NumberOfReview
            });

            model.AddProductReview.Title = null;
            model.AddProductReview.ReviewText = null;

            model.AddProductReview.SuccessfullyAdded = true;
            if (!productReview.IsApproved)
            {
                model.AddProductReview.Result = _translationService.GetResource("Reviews.SeeAfterApproving");
            }
            else
            {
                model.AddProductReview.Result = _translationService.GetResource("Reviews.SuccessfullyAdded");
                model.ProductReviewOverviewModel = await _mediator.Send(new GetProductReviewOverview {
                    Product = product,
                    Language = _contextAccessor.WorkContext.WorkingLanguage,
                    Store = _contextAccessor.StoreContext.CurrentStore
                });
            }

            return Json(model);
        }

        //If we got this far, something failed, redisplay form
        var newmodel = await _mediator.Send(new GetProductReviews {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Product = product,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Size = _catalogSettings.NumberOfReview
        });

        newmodel.AddProductReview.Rating = model.AddProductReview.Rating;
        newmodel.AddProductReview.ReviewText = model.AddProductReview.ReviewText;
        newmodel.AddProductReview.Title = model.AddProductReview.Title;
        newmodel.AddProductReview.Result = string.Join(",",
            ModelState.Values.SelectMany(m => m.Errors).Select(e => e.ErrorMessage).ToList());

        return Json(newmodel);
    }

    [HttpPost]
    [DenySystemAccount]
    public virtual async Task<IActionResult> SetProductReviewHelpfulness(string productReviewId, string productId,
        bool washelpful,
        [FromServices] ICustomerService customerService,
        [FromServices] IGroupService groupService,
        [FromServices] IProductReviewService productReviewService)
    {
        var productReview = await productReviewService.GetProductReviewById(productReviewId);
        if (productReview == null)
            throw new ArgumentException("No product review found with the specified id");

        if (await groupService.IsGuest(_contextAccessor.WorkContext.CurrentCustomer) &&
            !_catalogSettings.AllowAnonymousUsersToReviewProduct)
            return Json(new {
                Result = _translationService.GetResource("Reviews.Helpfulness.OnlyRegistered"),
                TotalYes = productReview.HelpfulYesTotal,
                TotalNo = productReview.HelpfulNoTotal
            });

        //customers aren't allowed to vote for their own reviews
        if (productReview.CustomerId == _contextAccessor.WorkContext.CurrentCustomer.Id)
            return Json(new {
                Result = _translationService.GetResource("Reviews.Helpfulness.YourOwnReview"),
                TotalYes = productReview.HelpfulYesTotal,
                TotalNo = productReview.HelpfulNoTotal
            });

        //delete previous helpfulness
        var prh = productReview.ProductReviewHelpfulnessEntries
            .FirstOrDefault(x => x.CustomerId == _contextAccessor.WorkContext.CurrentCustomer.Id);
        if (prh != null)
        {
            //existing one
            prh.WasHelpful = washelpful;
        }
        else
        {
            //insert new helpfulness
            prh = new ProductReviewHelpfulness {
                ProductReviewId = productReview.Id,
                CustomerId = _contextAccessor.WorkContext.CurrentCustomer.Id,
                WasHelpful = washelpful
            };
            productReview.ProductReviewHelpfulnessEntries.Add(prh);
            await productReviewService.UpdateProductReview(productReview);
            if (!_contextAccessor.WorkContext.CurrentCustomer.HasContributions)
                await customerService.UpdateContributions(_contextAccessor.WorkContext.CurrentCustomer);
        }

        //new totals
        productReview.HelpfulYesTotal = productReview.ProductReviewHelpfulnessEntries.Count(x => x.WasHelpful);
        productReview.HelpfulNoTotal = productReview.ProductReviewHelpfulnessEntries.Count(x => !x.WasHelpful);
        await productReviewService.UpdateProductReview(productReview);

        return Json(new {
            Result = _translationService.GetResource("Reviews.Helpfulness.SuccessfullyVoted"),
            TotalYes = productReview.HelpfulYesTotal,
            TotalNo = productReview.HelpfulNoTotal
        });
    }

    #endregion

    #region Comparing products

    [HttpGet]
    public virtual async Task<IActionResult> SidebarCompareProducts([FromServices] MediaSettings mediaSettings)
    {
        if (!_catalogSettings.CompareProductsEnabled)
            return Content("");

        var model = await _mediator.Send(new GetCompareProducts
            { PictureProductThumbSize = mediaSettings.MiniCartThumbPictureSize });
        return Json(model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> CompareProducts([FromServices] MediaSettings mediaSettings)
    {
        if (!_catalogSettings.CompareProductsEnabled)
            return RedirectToRoute("HomePage");

        var model = await _mediator.Send(new GetCompareProducts
            { PictureProductThumbSize = mediaSettings.CartThumbPictureSize });

        return View(model);
    }

    #endregion
}