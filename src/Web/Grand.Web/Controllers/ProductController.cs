using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Products;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Events;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Grand.Web.Controllers
{
    public partial class ProductController : BasePublicController
    {
        #region Fields

        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;
        private readonly IRecentlyViewedProductsService _recentlyViewedProductsService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IAclService _aclService;
        private readonly IPermissionService _permissionService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerActionEventService _customerActionEventService;
        private readonly IMediator _mediator;
        private readonly CatalogSettings _catalogSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CaptchaSettings _captchaSettings;

        #endregion

        #region Constructors

        public ProductController(
            IProductService productService,
            IWorkContext workContext,
            ITranslationService translationService,
            IRecentlyViewedProductsService recentlyViewedProductsService,
            IShoppingCartService shoppingCartService,
            IAclService aclService,
            IPermissionService permissionService,
            ICustomerActivityService customerActivityService,
            ICustomerActionEventService customerActionEventService,
            IMediator mediator,
            CatalogSettings catalogSettings,
            ShoppingCartSettings shoppingCartSettings,
            CaptchaSettings captchaSettings
        )
        {
            _productService = productService;
            _workContext = workContext;
            _translationService = translationService;
            _recentlyViewedProductsService = recentlyViewedProductsService;
            _shoppingCartService = shoppingCartService;
            _aclService = aclService;
            _permissionService = permissionService;
            _customerActivityService = customerActivityService;
            _customerActionEventService = customerActionEventService;
            _mediator = mediator;
            _catalogSettings = catalogSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _captchaSettings = captchaSettings;
        }

        #endregion

        #region Product details page

        public virtual async Task<IActionResult> ProductDetails(string productId, string updatecartitemid = "")
        {
            var product = await _productService.GetProductById(productId);
            if (product == null)
                return InvokeHttp404();

            var customer = _workContext.CurrentCustomer;

            //published?
            if (!_catalogSettings.AllowViewUnpublishedProductPage)
            {
                //Check whether the current user has a "Manage catalog" permission
                //It allows him to preview a product before publishing
                if (!product.Published && !await _permissionService.Authorize(StandardPermission.ManageProducts, customer))
                    return InvokeHttp404();
            }

            //ACL (access control list)
            if (!_aclService.Authorize(product, customer))
                return InvokeHttp404();

            //Store access
            if (!_aclService.Authorize(product, _workContext.CurrentStore.Id))
                return InvokeHttp404();

            //availability dates
            if (!product.IsAvailable() && !(product.ProductTypeId == ProductType.Auction))
                return InvokeHttp404();

            //visible individually?
            if (!product.VisibleIndividually)
            {
                //is this one an associated products?
                var parentGroupedProduct = await _productService.GetProductById(product.ParentGroupedProductId);
                if (parentGroupedProduct == null)
                    return RedirectToRoute("HomePage");

                return RedirectToRoute("Product", new { SeName = parentGroupedProduct.GetSeName(_workContext.WorkingLanguage.Id) });
            }
            //update existing shopping cart item?
            ShoppingCartItem updatecartitem = null;
            if (_shoppingCartSettings.AllowCartItemEditing && !String.IsNullOrEmpty(updatecartitemid))
            {
                var cart = await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id);

                updatecartitem = cart.FirstOrDefault(x => x.Id == updatecartitemid);
                //not found?
                if (updatecartitem == null)
                {
                    return RedirectToRoute("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) });
                }
                //is it this product?
                if (product.Id != updatecartitem.ProductId)
                {
                    return RedirectToRoute("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) });
                }
            }

            //prepare the model
            var model = await _mediator.Send(new GetProductDetailsPage() {
                Store = _workContext.CurrentStore,
                Product = product,
                IsAssociatedProduct = false,
                UpdateCartItem = updatecartitem
            });

            //product layout
            var productLayoutViewPath = await _mediator.Send(new GetProductLayoutViewPath() { ProductLayoutId = product.ProductLayoutId });

            //save as recently viewed
            await _recentlyViewedProductsService.AddProductToRecentlyViewedList(customer.Id, product.Id);

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel, customer) &&
                await _permissionService.Authorize(StandardPermission.ManageProducts, customer))
            {
                //a vendor should have access only to his products
                if (_workContext.CurrentVendor == null || _workContext.CurrentVendor.Id == product.VendorId)
                {
                    DisplayEditLink(Url.Action("Edit", "Product", new { id = product.Id, area = "Admin" }));
                }
            }

            //activity log
            _ = _customerActivityService.InsertActivity("PublicStore.ViewProduct", product.Id, _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.PublicStore.ViewProduct"), product.Name);
            await _customerActionEventService.Viewed(customer, this.HttpContext.Request.Path.ToString(), Request.Headers[HeaderNames.Referer].ToString() != null ? Request.Headers[HeaderNames.Referer].ToString() : "");
            await _productService.UpdateMostView(product);

            return View(productLayoutViewPath, model);
        }

        //handle product attribute selection event. this way we return new price, overridden gtin/sku/mpn
        //currently we use this method on the product details pages
        [HttpPost]
        public virtual async Task<IActionResult> ProductDetails_AttributeChange(string productId, bool loadPicture, IFormCollection form)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null)
                return new JsonResult("");

            var model = await _mediator.Send(new GetProductDetailsAttributeChange() {
                Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore,
                Form = form,
                LoadPicture = loadPicture,
                Product = product,
            });

            return Json(new
            {
                gtin = model.Gtin,
                mpn = model.Mpn,
                sku = model.Sku,
                price = model.Price,
                stockAvailability = model.StockAvailability,
                outOfStockSubscription = model.DisplayOutOfStockSubscription,
                buttonTextOutOfStockSubscription = model.ButtonTextOutOfStockSubscription,
                enabledattributemappingids = model.EnabledAttributeMappingIds.ToArray(),
                disabledattributemappingids = model.DisabledAttributeMappingids.ToArray(),
                notAvailableAttributeMappingids = model.NotAvailableAttributeMappingids.ToArray(),
                pictureFullSizeUrl = model.PictureFullSizeUrl,
                pictureDefaultSizeUrl = model.PictureDefaultSizeUrl,
            });
        }

        //handle product warehouse selection event. this way we return stock
        //currently we use this method on the product details pages
        [HttpPost]
        public virtual async Task<IActionResult> ProductDetails_WarehouseChange(string productId, string warehouseId, [FromServices] IStockQuantityService stockQuantityService)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null)
                return new JsonResult("");

            var stock = stockQuantityService.FormatStockMessage(product, warehouseId, null);
            return Json(new
            {
                stockAvailability = stock
            });
        }

        [HttpPost]
        public virtual async Task<IActionResult> UploadFileProductAttribute(string attributeId, string productId,
            [FromServices] IDownloadService downloadService)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    downloadGuid = Guid.Empty,
                });
            }
            var attribute = product.ProductAttributeMappings.Where(x => x.Id == attributeId).FirstOrDefault();
            if (attribute == null || attribute.AttributeControlTypeId != AttributeControlType.FileUpload)
            {
                return Json(new
                {
                    success = false,
                    downloadGuid = Guid.Empty,
                });
            }
            var form = await HttpContext.Request.ReadFormAsync();
            var httpPostedFile = form.Files.FirstOrDefault();
            if (httpPostedFile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No file uploaded",
                    downloadGuid = Guid.Empty,
                });
            }
            var fileBinary = httpPostedFile.GetDownloadBits();

            var qqFileNameParameter = "qqfilename";
            var fileName = httpPostedFile.FileName;
            if (string.IsNullOrEmpty(fileName) && form.ContainsKey(qqFileNameParameter))
                fileName = form[qqFileNameParameter].ToString();
            //remove path (passed in IE)
            fileName = Path.GetFileName(fileName);

            var contentType = httpPostedFile.ContentType;

            var fileExtension = Path.GetExtension(fileName);
            if (!String.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();

            if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
            {
                var allowedFileExtensions = attribute.ValidationFileAllowedExtensions.ToLowerInvariant()
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
                if (!allowedFileExtensions.Contains(fileExtension.ToLowerInvariant()))
                {
                    return Json(new
                    {
                        success = false,
                        message = _translationService.GetResource("ShoppingCart.ValidationFileAllowed"),
                        downloadGuid = Guid.Empty,
                    });
                }
            }
            if (attribute.ValidationFileMaximumSize.HasValue)
            {
                //compare in bytes
                var maxFileSizeBytes = attribute.ValidationFileMaximumSize.Value * 1024;
                if (fileBinary.Length > maxFileSizeBytes)
                {
                    //when returning JSON the mime-type must be set to text/plain
                    //otherwise some browsers will pop-up a "Save As" dialog.
                    return Json(new
                    {
                        success = false,
                        message = string.Format(_translationService.GetResource("ShoppingCart.MaximumUploadedFileSize"), attribute.ValidationFileMaximumSize.Value),
                        downloadGuid = Guid.Empty,
                    });
                }
            }

            var download = new Download {
                DownloadGuid = Guid.NewGuid(),
                UseDownloadUrl = false,
                DownloadUrl = "",
                DownloadBinary = fileBinary,
                ContentType = contentType,
                //we store filename without extension for downloads
                Filename = Path.GetFileNameWithoutExtension(fileName),
                Extension = fileExtension,
                IsNew = true
            };
            await downloadService.InsertDownload(download);

            //when returning JSON the mime-type must be set to text/plain
            //otherwise some browsers will pop-up a "Save As" dialog.
            return Json(new
            {
                success = true,
                message = _translationService.GetResource("ShoppingCart.FileUploaded"),
                downloadUrl = Url.Action("GetFileUpload", "Download", new { downloadId = download.DownloadGuid }),
                downloadGuid = download.DownloadGuid,
            });
        }

        #region Quick view product

        public virtual async Task<IActionResult> QuickView(string productId)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null)
                return Json(new
                {
                    success = false,
                    message = "No product found with the specified ID"
                });

            var customer = _workContext.CurrentCustomer;

            //published?
            if (!_catalogSettings.AllowViewUnpublishedProductPage)
            {
                //Check whether the current user has a "Manage catalog" permission
                //It allows him to preview a product before publishing
                if (!product.Published && !await _permissionService.Authorize(StandardPermission.ManageProducts, customer))
                    return Json(new
                    {
                        success = false,
                        message = "No product found with the specified ID"
                    });
            }

            //ACL (access control list)
            if (!_aclService.Authorize(product, customer))
                return Json(new
                {
                    success = false,
                    message = "No product found with the specified ID"
                });

            //Store access
            if (!_aclService.Authorize(product, _workContext.CurrentStore.Id))
                return Json(new
                {
                    success = false,
                    message = "No product found with the specified ID"
                });

            //availability dates
            if (!product.IsAvailable() && !(product.ProductTypeId == ProductType.Auction))
                return Json(new
                {
                    success = false,
                    message = "No product found with the specified ID"
                });

            //visible individually?
            if (!product.VisibleIndividually)
            {
                //is this one an associated products?
                var parentGroupedProduct = await _productService.GetProductById(product.ParentGroupedProductId);
                if (parentGroupedProduct == null)
                {
                    return Json(new
                    {
                        redirect = Url.RouteUrl("HomePage"),
                    });
                }
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            //prepare the model
            var model = await _mediator.Send(new GetProductDetailsPage() {
                Store = _workContext.CurrentStore,
                Product = product,
                IsAssociatedProduct = false,
                UpdateCartItem = null
            });

            //product layout
            var productLayoutViewPath = await _mediator.Send(new GetProductLayoutViewPath() { ProductLayoutId = product.ProductLayoutId });

            //save as recently viewed
            await _recentlyViewedProductsService.AddProductToRecentlyViewedList(customer.Id, product.Id);

            //activity log
            _ = _customerActivityService.InsertActivity("PublicStore.ViewProduct", product.Id, _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.PublicStore.ViewProduct"), product.Name);
            await _customerActionEventService.Viewed(customer, HttpContext.Request.Path.ToString(), Request.Headers[HeaderNames.Referer].ToString() != null ? Request.Headers[HeaderNames.Referer].ToString() : "");
            await _productService.UpdateMostView(product);

            return Json(new
            {
                success = true,
                product = true,
                model = model,
                layoutPath = productLayoutViewPath
            });
        }
        #endregion

        #endregion

        #region Recently viewed products

        public virtual async Task<IActionResult> RecentlyViewedProducts()
        {
            if (!_catalogSettings.RecentlyViewedProductsEnabled)
                return Content("");

            var products = await _recentlyViewedProductsService.GetRecentlyViewedProducts(_workContext.CurrentCustomer.Id, _catalogSettings.RecentlyViewedProductsNumber);

            //prepare model
            var model = await _mediator.Send(new GetProductOverview() {
                Products = products,
            });

            return View(model);
        }

        #endregion

        #region Recently added products

        public virtual async Task<IActionResult> NewProducts()
        {
            if (!_catalogSettings.NewProductsEnabled)
                return Content("");

            var products = (await _productService.SearchProducts(
                storeId: _workContext.CurrentStore.Id,
                visibleIndividuallyOnly: true,
                markedAsNewOnly: true,
                orderBy: ProductSortingEnum.CreatedOn,
                pageSize: _catalogSettings.NewProductsNumber)).products;


            //prepare model
            var model = await _mediator.Send(new GetProductOverview() {
                PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
                Products = products,
            });

            return View(model);
        }
        #endregion

        #region Product reviews

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        [DenySystemAccount]
        public virtual async Task<IActionResult> ProductReviews(
            string productId,
            ProductReviewsModel model,
            bool captchaValid,
            [FromServices] IGroupService groupService,
            [FromServices] IOrderService orderService,
            [FromServices] IProductReviewService productReviewService)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null || !product.Published || !product.AllowCustomerReviews)
                return RedirectToRoute("HomePage");

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnProductReviewPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            if (await groupService.IsGuest(_workContext.CurrentCustomer) && !_catalogSettings.AllowAnonymousUsersToReviewProduct)
            {
                ModelState.AddModelError("", _translationService.GetResource("Reviews.OnlyRegisteredUsersCanWriteReviews"));
            }

            if (_catalogSettings.ProductReviewPossibleOnlyAfterPurchasing &&
                    !(await orderService.SearchOrders(customerId: _workContext.CurrentCustomer.Id, productId: productId, os: (int)OrderStatusSystem.Complete)).Any())
                ModelState.AddModelError(string.Empty, _translationService.GetResource("Reviews.ProductReviewPossibleOnlyAfterPurchasing"));

            if (_catalogSettings.ProductReviewPossibleOnlyOnce)
            {
                var reviews = await productReviewService.GetAllProductReviews(customerId: _workContext.CurrentCustomer.Id,
                                                                              productId: productId,
                                                                              pageSize: 1);
                if (reviews.Any())
                    ModelState.AddModelError(string.Empty, _translationService.GetResource("Reviews.ProductReviewPossibleOnlyOnce"));
            }

            if (ModelState.IsValid)
            {
                var productReview = await _mediator.Send(new InsertProductReviewCommand() {
                    Customer = _workContext.CurrentCustomer,
                    Store = _workContext.CurrentStore,
                    Model = model,
                    Product = product
                });

                //notification
                await _mediator.Publish(new ProductReviewEvent(product, model.AddProductReview));

                _ = _customerActivityService.InsertActivity("PublicStore.AddProductReview", product.Id,
                     _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    _translationService.GetResource("ActivityLog.PublicStore.AddProductReview"), product.Name);

                //raise event
                if (productReview.IsApproved)
                    await _mediator.Publish(new ProductReviewApprovedEvent(productReview));

                model = await _mediator.Send(new GetProductReviews() {
                    Customer = _workContext.CurrentCustomer,
                    Language = _workContext.WorkingLanguage,
                    Product = product,
                    Store = _workContext.CurrentStore,
                    Size = _catalogSettings.NumberOfReview
                });

                model.AddProductReview.Title = null;
                model.AddProductReview.ReviewText = null;

                model.AddProductReview.SuccessfullyAdded = true;
                if (!productReview.IsApproved)
                    model.AddProductReview.Result = _translationService.GetResource("Reviews.SeeAfterApproving");
                else
                {
                    model.AddProductReview.Result = _translationService.GetResource("Reviews.SuccessfullyAdded");
                    model.ProductReviewOverviewModel = await _mediator.Send(new GetProductReviewOverview() {
                        Product = product,
                        Language = _workContext.WorkingLanguage,
                        Store = _workContext.CurrentStore
                    });
                }
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            var newmodel = await _mediator.Send(new GetProductReviews() {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Product = product,
                Store = _workContext.CurrentStore,
                Size = _catalogSettings.NumberOfReview
            });

            newmodel.AddProductReview.Rating = model.AddProductReview.Rating;
            newmodel.AddProductReview.ReviewText = model.AddProductReview.ReviewText;
            newmodel.AddProductReview.Title = model.AddProductReview.Title;
            newmodel.AddProductReview.Result = string.Join(",", ModelState.Values.SelectMany(m => m.Errors).Select(e => e.ErrorMessage).ToList());

            return View(newmodel);
        }

        [HttpPost]
        [DenySystemAccount]
        public virtual async Task<IActionResult> SetProductReviewHelpfulness(string productReviewId, string productId, bool washelpful,
            [FromServices] ICustomerService customerService,
            [FromServices] IGroupService groupService,
            [FromServices] IProductReviewService productReviewService)
        {
            var product = await _productService.GetProductById(productId);
            var productReview = await productReviewService.GetProductReviewById(productReviewId);
            if (productReview == null)
                throw new ArgumentException("No product review found with the specified id");

            if (await groupService.IsGuest(_workContext.CurrentCustomer) && !_catalogSettings.AllowAnonymousUsersToReviewProduct)
            {
                return Json(new
                {
                    Result = _translationService.GetResource("Reviews.Helpfulness.OnlyRegistered"),
                    TotalYes = productReview.HelpfulYesTotal,
                    TotalNo = productReview.HelpfulNoTotal
                });
            }

            //customers aren't allowed to vote for their own reviews
            if (productReview.CustomerId == _workContext.CurrentCustomer.Id)
            {
                return Json(new
                {
                    Result = _translationService.GetResource("Reviews.Helpfulness.YourOwnReview"),
                    TotalYes = productReview.HelpfulYesTotal,
                    TotalNo = productReview.HelpfulNoTotal
                });
            }

            //delete previous helpfulness
            var prh = productReview.ProductReviewHelpfulnessEntries
                .FirstOrDefault(x => x.CustomerId == _workContext.CurrentCustomer.Id);
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
                    CustomerId = _workContext.CurrentCustomer.Id,
                    WasHelpful = washelpful,
                };
                productReview.ProductReviewHelpfulnessEntries.Add(prh);
                await productReviewService.UpdateProductReview(productReview);
                if (!_workContext.CurrentCustomer.HasContributions)
                {
                    await customerService.UpdateContributions(_workContext.CurrentCustomer);
                }

            }

            //new totals
            productReview.HelpfulYesTotal = productReview.ProductReviewHelpfulnessEntries.Count(x => x.WasHelpful);
            productReview.HelpfulNoTotal = productReview.ProductReviewHelpfulnessEntries.Count(x => !x.WasHelpful);
            await productReviewService.UpdateProductReview(productReview);

            return Json(new
            {
                Result = _translationService.GetResource("Reviews.Helpfulness.SuccessfullyVoted"),
                TotalYes = productReview.HelpfulYesTotal,
                TotalNo = productReview.HelpfulNoTotal
            });
        }

        #endregion

        #region Email a friend

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        [DenySystemAccount]
        public virtual async Task<IActionResult> ProductEmailAFriend(ProductEmailAFriendModel model, bool captchaValid,
            [FromServices] IGroupService groupService)
        {
            var product = await _productService.GetProductById(model.ProductId);
            if (product == null || !product.Published || !_catalogSettings.EmailAFriendEnabled)
                return Content("");

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnEmailProductToFriendPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            //check whether the current customer is guest and ia allowed to email a friend
            if (await groupService.IsGuest(_workContext.CurrentCustomer) && !_catalogSettings.AllowAnonymousUsersToEmailAFriend)
            {
                ModelState.AddModelError("", _translationService.GetResource("Products.EmailAFriend.OnlyRegisteredUsers"));
            }

            if (ModelState.IsValid)
            {
                //email
                await _mediator.Send(new SendProductEmailAFriendMessageCommand() {
                    Customer = _workContext.CurrentCustomer,
                    Product = product,
                    Language = _workContext.WorkingLanguage,
                    Store = _workContext.CurrentStore,
                    Model = model,
                });

                model.ProductId = product.Id;
                model.ProductName = product.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id);
                model.ProductSeName = product.GetSeName(_workContext.WorkingLanguage.Id);

                model.SuccessfullySent = true;
                model.Result = _translationService.GetResource("Products.EmailAFriend.SuccessfullySent");

                return Json(model);
            }

            //If we got this far, something failed, redisplay form
            model.ProductId = product.Id;
            model.ProductName = product.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id);
            model.ProductSeName = product.GetSeName(_workContext.WorkingLanguage.Id);
            model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnEmailProductToFriendPage;
            model.SuccessfullySent = false;
            model.Result = string.Join(",", ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage));

            return Json(model);
        }

        #endregion

        #region Ask question

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        [DenySystemAccount]
        public virtual async Task<IActionResult> AskQuestionOnProduct(ProductAskQuestionSimpleModel model, bool captchaValid)
        {
            var product = await _productService.GetProductById(model.Id);
            if (product == null || !product.Published || !_catalogSettings.AskQuestionOnProduct)
                return Json(new
                {
                    success = false,
                    message = "Product not found"
                });

            // validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnAskQuestionPage && !captchaValid)
            {
                return Json(new
                {
                    success = false,
                    message = _captchaSettings.GetWrongCaptchaMessage(_translationService)
                });
            }

            if (ModelState.IsValid)
            {
                var productaskqestionmodel = new ProductAskQuestionModel() {
                    Email = model.AskQuestionEmail,
                    FullName = model.AskQuestionFullName,
                    Phone = model.AskQuestionPhone,
                    Message = model.AskQuestionMessage
                };

                // email
                await _mediator.Send(new SendProductAskQuestionMessageCommand() {
                    Customer = _workContext.CurrentCustomer,
                    Language = _workContext.WorkingLanguage,
                    Store = _workContext.CurrentStore,
                    Model = productaskqestionmodel,
                    Product = product,
                    RemoteIpAddress = HttpContext.Connection?.RemoteIpAddress?.ToString()
                });

                //activity log
                _ = _customerActivityService.InsertActivity("PublicStore.AskQuestion", _workContext.CurrentCustomer.Id,
                     _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    _translationService.GetResource("ActivityLog.PublicStore.AskQuestion"));
                //return Json
                return Json(new
                {
                    success = true,
                    message = _translationService.GetResource("Products.AskQuestion.SuccessfullySent")
                });

            }

            // If we got this far, something failed, redisplay form
            return Json(new
            {
                success = false,
                message = string.Join(",", ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage))
            });

        }

        #endregion

        #region Comparing products

        public virtual async Task<IActionResult> SidebarCompareProducts([FromServices] MediaSettings mediaSettings)
        {
            if (!_catalogSettings.CompareProductsEnabled)
                return Content("");

            var model = await _mediator.Send(new GetCompareProducts() { PictureProductThumbSize = mediaSettings.MiniCartThumbPictureSize });

            return View(model);

        }


        public virtual async Task<IActionResult> CompareProducts([FromServices] MediaSettings mediaSettings)
        {
            if (!_catalogSettings.CompareProductsEnabled)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetCompareProducts() { PictureProductThumbSize = mediaSettings.CartThumbPictureSize });

            return View(model);
        }
        #endregion

        #region Calendar

        public virtual async Task<IActionResult> GetDatesForMonth(string productId, int month, string parameter, int year, [FromServices] IProductReservationService productReservationService)
        {
            var allReservations = await productReservationService.GetProductReservationsByProductId(productId, true, null);
            var query = allReservations.Where(x => x.Date.Month == month && x.Date.Year == year && x.Date >= DateTime.UtcNow);
            if (!string.IsNullOrEmpty(parameter))
            {
                query = query.Where(x => x.Parameter == parameter);
            }

            var reservations = query.ToList();
            var inCart = (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id))
                .Where(x => !string.IsNullOrEmpty(x.ReservationId)).ToList();
            foreach (var cartItem in inCart)
            {
                var match = reservations.FirstOrDefault(x => x.Id == cartItem.ReservationId);
                if (match != null)
                {
                    reservations.Remove(match);
                }
            }

            var toReturn = reservations.GroupBy(x => x.Date).Select(x => x.First()).ToList();

            return Json(toReturn);
        }

        #endregion

    }
}
