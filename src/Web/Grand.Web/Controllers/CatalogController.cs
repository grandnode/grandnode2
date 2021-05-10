using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Events;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Vendors;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Vendors;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Vendors;
using MediatR;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public partial class CatalogController : BasePublicController
    {
        #region Fields

        private readonly IVendorService _vendorService;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;
        private readonly ICollectionService _collectionService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly ITranslationService _translationService;
        private readonly IUserFieldService _userFieldService;
        private readonly IAclService _aclService;
        private readonly IPermissionService _permissionService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerActionEventService _customerActionEventService;
        private readonly IMediator _mediator;

        private readonly VendorSettings _vendorSettings;

        #endregion

        #region Constructors
        public CatalogController(
            IVendorService vendorService,
            ICategoryService categoryService,
            IBrandService brandService,
            ICollectionService collectionService,
            IWorkContext workContext,
            IGroupService groupService,
            ITranslationService translationService,
            IUserFieldService userFieldService,
            IAclService aclService,
            IPermissionService permissionService,
            ICustomerActivityService customerActivityService,
            ICustomerActionEventService customerActionEventService,
            IMediator mediator,
            VendorSettings vendorSettings)
        {
            _vendorService = vendorService;
            _categoryService = categoryService;
            _brandService = brandService;
            _collectionService = collectionService;
            _workContext = workContext;
            _groupService = groupService;
            _translationService = translationService;
            _userFieldService = userFieldService;
            _aclService = aclService;
            _permissionService = permissionService;
            _customerActivityService = customerActivityService;
            _customerActionEventService = customerActionEventService;
            _mediator = mediator;
            _vendorSettings = vendorSettings;
        }

        #endregion

        #region Utilities

        protected async Task SaveLastContinueShoppingPage(Customer customer)
        {
            await _userFieldService.SaveField(customer,
                SystemCustomerFieldNames.LastContinueShoppingPage,
                HttpContext?.Request?.GetDisplayUrl(),
                _workContext.CurrentStore.Id);
        }

        private VendorReviewOverviewModel PrepareVendorReviewOverviewModel(Vendor vendor)
        {
            var model = new VendorReviewOverviewModel()
            {
                RatingSum = vendor.ApprovedRatingSum,
                TotalReviews = vendor.ApprovedTotalReviews,
                VendorId = vendor.Id,
                AllowCustomerReviews = vendor.AllowCustomerReviews
            };
            return model;
        }

        #endregion

        #region Categories

        public virtual async Task<IActionResult> Category(string categoryId, CatalogPagingFilteringModel command)
        {
            var category = await _categoryService.GetCategoryById(categoryId);
            if (category == null)
                return InvokeHttp404();

            var customer = _workContext.CurrentCustomer;

            //Check whether the current user has a "Manage catalog" permission
            //It allows him to preview a category before publishing
            if (!category.Published && !await _permissionService.Authorize(StandardPermission.ManageCategories, customer))
                return InvokeHttp404();

            //ACL (access control list)
            if (!_aclService.Authorize(category, customer))
                return InvokeHttp404();

            //Store access
            if (!_aclService.Authorize(category, _workContext.CurrentStore.Id))
                return InvokeHttp404();

            //'Continue shopping' URL
            await SaveLastContinueShoppingPage(customer);

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel, customer) && await _permissionService.Authorize(StandardPermission.ManageCategories, customer))
                DisplayEditLink(Url.Action("Edit", "Category", new { id = category.Id, area = "Admin" }));

            //activity log
            await _customerActivityService.InsertActivity("PublicStore.ViewCategory", category.Id, _translationService.GetResource("ActivityLog.PublicStore.ViewCategory"), category.Name);
            await _customerActionEventService.Viewed(customer, HttpContext.Request.Path.ToString(), Request.Headers[HeaderNames.Referer].ToString() != null ? Request.Headers["Referer"].ToString() : "");

            //model
            var model = await _mediator.Send(new GetCategory()
            {
                Category = category,
                Command = command,
                Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });

            //layout
            var layoutViewPath = await _mediator.Send(new GetCategoryLayoutViewPath() { LayoutId = category.CategoryLayoutId });

            return View(layoutViewPath, model);
        }

        #endregion

        #region Brands

        public virtual async Task<IActionResult> Brand(string brandId, CatalogPagingFilteringModel command)
        {
            var brand = await _brandService.GetBrandById(brandId);
            if (brand == null)
                return InvokeHttp404();

            var customer = _workContext.CurrentCustomer;

            //Check whether the current user has a "Manage catalog" permission
            //It allows him to preview a collection before publishing
            if (!brand.Published && !await _permissionService.Authorize(StandardPermission.ManageBrands, customer))
                return InvokeHttp404();

            //ACL (access control list)
            if (!_aclService.Authorize(brand, customer))
                return InvokeHttp404();

            //Store access
            if (!_aclService.Authorize(brand, _workContext.CurrentStore.Id))
                return InvokeHttp404();

            //'Continue shopping' URL
            await SaveLastContinueShoppingPage(customer);

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel, customer) && await _permissionService.Authorize(StandardPermission.ManageBrands, customer))
                DisplayEditLink(Url.Action("Edit", "Brand", new { id = brand.Id, area = "Admin" }));

            //activity log
            await _customerActivityService.InsertActivity("PublicStore.ViewBrand", brand.Id, _translationService.GetResource("ActivityLog.PublicStore.ViewBrand"), brand.Name);
            await _customerActionEventService.Viewed(customer, HttpContext.Request.Path.ToString(), Request.Headers[HeaderNames.Referer].ToString() != null ? Request.Headers[HeaderNames.Referer].ToString() : "");

            //model
            var model = await _mediator.Send(new GetBrand()
            {
                Command = command,
                Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Brand = brand,
                Store = _workContext.CurrentStore
            });

            //template
            var layoutViewPath = await _mediator.Send(new GetBrandLayoutViewPath() { LayoutId = brand.BrandLayoutId });

            return View(layoutViewPath, model);
        }

        public virtual async Task<IActionResult> BrandAll()
        {
            var model = await _mediator.Send(new GetBrandAll()
            {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });
            return View(model);
        }

        #endregion

        #region Collections

        public virtual async Task<IActionResult> Collection(string collectionId, CatalogPagingFilteringModel command)
        {
            var collection = await _collectionService.GetCollectionById(collectionId);
            if (collection == null)
                return InvokeHttp404();

            var customer = _workContext.CurrentCustomer;

            //Check whether the current user has a "Manage catalog" permission
            //It allows him to preview a collection before publishing
            if (!collection.Published && !await _permissionService.Authorize(StandardPermission.ManageCollections, customer))
                return InvokeHttp404();

            //ACL (access control list)
            if (!_aclService.Authorize(collection, customer))
                return InvokeHttp404();

            //Store access
            if (!_aclService.Authorize(collection, _workContext.CurrentStore.Id))
                return InvokeHttp404();

            //'Continue shopping' URL
            await SaveLastContinueShoppingPage(customer);

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel, customer) && await _permissionService.Authorize(StandardPermission.ManageCollections, customer))
                DisplayEditLink(Url.Action("Edit", "Collection", new { id = collection.Id, area = "Admin" }));

            //activity log
            await _customerActivityService.InsertActivity("PublicStore.ViewCollection", collection.Id, _translationService.GetResource("ActivityLog.PublicStore.ViewCollection"), collection.Name);
            await _customerActionEventService.Viewed(customer, HttpContext.Request.Path.ToString(), Request.Headers[HeaderNames.Referer].ToString() != null ? Request.Headers[HeaderNames.Referer].ToString() : "");

            //model
            var model = await _mediator.Send(new GetCollection()
            {
                Command = command,
                Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Collection = collection,
                Store = _workContext.CurrentStore
            });

            //template
            var layoutViewPath = await _mediator.Send(new GetCollectionLayoutViewPath() { LayoutId = collection.CollectionLayoutId });

            return View(layoutViewPath, model);
        }

        public virtual async Task<IActionResult> CollectionAll()
        {
            var model = await _mediator.Send(new GetCollectionAll()
            {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });
            return View(model);
        }

        #endregion

        #region Vendors

        public virtual async Task<IActionResult> Vendor(string vendorId, CatalogPagingFilteringModel command)
        {
            var vendor = await _vendorService.GetVendorById(vendorId);
            if (vendor == null || vendor.Deleted || !vendor.Active)
                return InvokeHttp404();

            //Vendor is active?
            if (!vendor.Active)
                return InvokeHttp404();

            var customer = _workContext.CurrentCustomer;

            //'Continue shopping' URL
            await SaveLastContinueShoppingPage(customer);

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel, customer) && await _permissionService.Authorize(StandardPermission.ManageCollections, customer))
                DisplayEditLink(Url.Action("Edit", "Vendor", new { id = vendor.Id, area = "Admin" }));

            var model = await _mediator.Send(new GetVendor()
            {
                Command = command,
                Vendor = vendor,
                Language = _workContext.WorkingLanguage,
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore,
            });
            //review
            model.VendorReviewOverview = PrepareVendorReviewOverviewModel(vendor);

            return View(model);
        }

        public virtual async Task<IActionResult> VendorAll()
        {
            //we don't allow viewing of vendors if "vendors" block is hidden
            if (_vendorSettings.VendorsBlockItemsToDisplay == 0)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetVendorAll() { Language = _workContext.WorkingLanguage });
            return View(model);
        }

        #endregion

        #region Vendor reviews

        public virtual async Task<IActionResult> VendorReviews(string vendorId)
        {
            var vendor = await _vendorService.GetVendorById(vendorId);
            if (vendor == null || !vendor.Active || !vendor.AllowCustomerReviews)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetVendorReviews() { Vendor = vendor });

            //only registered users can leave reviews
            if (await _groupService.IsGuest(_workContext.CurrentCustomer) && !_vendorSettings.AllowAnonymousUsersToReviewVendor)
                ModelState.AddModelError("", _translationService.GetResource("VendorReviews.OnlyRegisteredUsersCanWriteReviews"));
            //default value
            model.AddVendorReview.Rating = _vendorSettings.DefaultVendorRatingValue;
            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        public virtual async Task<IActionResult> VendorReviews(string vendorId, VendorReviewsModel model, bool captchaValid,
            [FromServices] CaptchaSettings captchaSettings)
        {
            var vendor = await _vendorService.GetVendorById(vendorId);
            if (vendor == null || !vendor.Active || !vendor.AllowCustomerReviews)
                return RedirectToRoute("HomePage");

            //validate CAPTCHA
            if (captchaSettings.Enabled && captchaSettings.ShowOnVendorReviewPage && !captchaValid)
            {
                ModelState.AddModelError("", captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            if (await _groupService.IsGuest(_workContext.CurrentCustomer) && !_vendorSettings.AllowAnonymousUsersToReviewVendor)
            {
                ModelState.AddModelError("", _translationService.GetResource("VendorReviews.OnlyRegisteredUsersCanWriteReviews"));
            }

            //allow reviews only by customer that bought something from this vendor
            if (_vendorSettings.VendorReviewPossibleOnlyAfterPurchasing &&
                    !(await _mediator.Send(new GetOrderQuery()
                    {
                        CustomerId = _workContext.CurrentCustomer.Id,
                        VendorId = vendorId,
                        Os = (int)OrderStatusSystem.Complete,
                        PageSize = 1
                    })).Any())
                ModelState.AddModelError(string.Empty, _translationService.GetResource("VendorReviews.VendorReviewPossibleOnlyAfterPurchasing"));

            if (ModelState.IsValid)
            {
                var vendorReview = await _mediator.Send(new InsertVendorReviewCommand() { Vendor = vendor, Store = _workContext.CurrentStore, Model = model });
                //activity log
                await _customerActivityService.InsertActivity("PublicStore.AddVendorReview", vendor.Id, _translationService.GetResource("ActivityLog.PublicStore.AddVendorReview"), vendor.Name);

                //raise event
                if (vendorReview.IsApproved)
                    await _mediator.Publish(new VendorReviewApprovedEvent(vendorReview));

                model = await _mediator.Send(new GetVendorReviews() { Vendor = vendor });
                model.AddVendorReview.Title = null;
                model.AddVendorReview.ReviewText = null;

                model.AddVendorReview.SuccessfullyAdded = true;
                if (!vendorReview.IsApproved)
                    model.AddVendorReview.Result = _translationService.GetResource("VendorReviews.SeeAfterApproving");
                else
                    model.AddVendorReview.Result = _translationService.GetResource("VendorReviews.SuccessfullyAdded");

                return View(model);
            }
            model = await _mediator.Send(new GetVendorReviews() { Vendor = vendor });

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> SetVendorReviewHelpfulness(string VendorReviewId, string vendorId, bool washelpful, [FromServices] ICustomerService customerService)
        {
            var vendor = await _vendorService.GetVendorById(vendorId);
            var vendorReview = await _vendorService.GetVendorReviewById(VendorReviewId);
            if (vendorReview == null)
                throw new ArgumentException("No vendor review found with the specified id");

            var customer = _workContext.CurrentCustomer;

            if (await _groupService.IsGuest(customer) && !_vendorSettings.AllowAnonymousUsersToReviewVendor)
            {
                return Json(new
                {
                    Result = _translationService.GetResource("VendorReviews.Helpfulness.OnlyRegistered"),
                    TotalYes = vendorReview.HelpfulYesTotal,
                    TotalNo = vendorReview.HelpfulNoTotal
                });
            }

            //customers aren't allowed to vote for their own reviews
            if (vendorReview.CustomerId == customer.Id)
            {
                return Json(new
                {
                    Result = _translationService.GetResource("VendorReviews.Helpfulness.YourOwnReview"),
                    TotalYes = vendorReview.HelpfulYesTotal,
                    TotalNo = vendorReview.HelpfulNoTotal
                });
            }

            vendorReview = await _mediator.Send(new SetVendorReviewHelpfulnessCommand()
            {
                Customer = _workContext.CurrentCustomer,
                Vendor = vendor,
                Review = vendorReview,
                Washelpful = washelpful
            });

            return Json(new
            {
                Result = _translationService.GetResource("VendorReviews.Helpfulness.SuccessfullyVoted"),
                TotalYes = vendorReview.HelpfulYesTotal,
                TotalNo = vendorReview.HelpfulNoTotal
            });
        }

        #endregion

        #region Product tags

        public virtual async Task<IActionResult> ProductsByTag(string productTagId, CatalogPagingFilteringModel command, [FromServices] IProductTagService productTagService)
        {
            var productTag = await productTagService.GetProductTagById(productTagId);
            if (productTag == null)
                return InvokeHttp404();

            var model = await _mediator.Send(new GetProductsByTag()
            {
                Command = command,
                Language = _workContext.WorkingLanguage,
                ProductTag = productTag,
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore
            });
            return View(model);
        }
        public virtual async Task<IActionResult> ProductsByTagName(string seName, CatalogPagingFilteringModel command, [FromServices] IProductTagService productTagService)
        {
            var productTag = await productTagService.GetProductTagBySeName(seName);
            if (productTag == null)
                return InvokeHttp404();

            var model = await _mediator.Send(new GetProductsByTag()
            {
                Command = command,
                Language = _workContext.WorkingLanguage,
                ProductTag = productTag,
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore
            });
            return View("ProductsByTag", model);
        }

        public virtual async Task<IActionResult> ProductTagsAll()
        {
            var model = await _mediator.Send(new GetProductTagsAll()
            {
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });
            return View(model);
        }

        #endregion

        #region Searching

        public virtual async Task<IActionResult> Search(SearchModel model, CatalogPagingFilteringModel command, string searchCategoryId)
        {
            //'Continue shopping' URL
            await SaveLastContinueShoppingPage(_workContext.CurrentCustomer);
            if (model != null && !string.IsNullOrEmpty(searchCategoryId))
            {
                model.cid = searchCategoryId;
                model.adv = true;
            }
            //Prepare model
            var isSearchTermSpecified = HttpContext?.Request?.Query.ContainsKey("q");
            var searchmodel = await _mediator.Send(new GetSearch()
            {
                Command = command,
                Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                IsSearchTermSpecified = isSearchTermSpecified.HasValue ? isSearchTermSpecified.Value : false,
                Language = _workContext.WorkingLanguage,
                Model = model,
                Store = _workContext.CurrentStore
            });
            return View(searchmodel);
        }

        public virtual async Task<IActionResult> SearchTermAutoComplete(string term, string categoryId, [FromServices] CatalogSettings catalogSettings)
        {
            if (String.IsNullOrWhiteSpace(term) || term.Length < catalogSettings.ProductSearchTermMinimumLength)
                return Content("");

            var result = await _mediator.Send(new GetSearchAutoComplete()
            {
                CategoryId = categoryId,
                Term = term.Trim(),
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore,
                Language = _workContext.WorkingLanguage,
                Currency = _workContext.WorkingCurrency
            });
            return Json(result);
        }

        #endregion
    }
}
