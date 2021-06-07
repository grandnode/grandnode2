using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Domain;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Discounts;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Discounts)]
    public partial class DiscountController : BaseAdminController
    {
        #region Fields

        private readonly IDiscountViewModelService _discountViewModelService;
        private readonly IDiscountService _discountService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IGroupService _groupService;

        #endregion

        #region Constructors

        public DiscountController(
            IDiscountViewModelService discountViewModelService,
            IDiscountService discountService,
            ITranslationService translationService,
            IWorkContext workContext,
            IStoreService storeService,
            IDateTimeService dateTimeService,
            IGroupService groupService)
        {
            _discountViewModelService = discountViewModelService;
            _discountService = discountService;
            _translationService = translationService;
            _workContext = workContext;
            _storeService = storeService;
            _dateTimeService = dateTimeService;
            _groupService = groupService;
        }

        #endregion

        #region Discounts

        //list
        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List()
        {
            var model = _discountViewModelService.PrepareDiscountListModel();
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DiscountListModel model, DataSourceRequest command)
        {
            var (discountModel, totalCount) = await _discountViewModelService.PrepareDiscountModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = discountModel.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new DiscountModel();
            await _discountViewModelService.PrepareDiscountModel(model, null);

            //default values
            model.LimitationTimes = 1;
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create(DiscountModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }

                var discount = await _discountViewModelService.InsertDiscountModel(model);
                Success(_translationService.GetResource("admin.marketing.discounts.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = discount.Id }) : RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            await _discountViewModelService.PrepareDiscountModel(model, null);

            return View(model);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var discount = await _discountService.GetDiscountById(id);
            if (discount == null)
                //No discount found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!discount.LimitedToStores || (discount.LimitedToStores && discount.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && discount.Stores.Count > 1))
                    Warning(_translationService.GetResource("admin.marketing.discounts.Permisions"));
                else
                {
                    if (!discount.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                        return RedirectToAction("List");
                }
            }

            var model = discount.ToModel(_dateTimeService);
            await _discountViewModelService.PrepareDiscountModel(model, discount);

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(DiscountModel model, bool continueEditing)
        {
            var discount = await _discountService.GetDiscountById(model.Id);
            if (discount == null)
                //No discount found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!discount.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return RedirectToAction("Edit", new { id = discount.Id });
            }

            if (ModelState.IsValid)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }
                discount = await _discountViewModelService.UpdateDiscountModel(discount, model);
                Success(_translationService.GetResource("admin.marketing.discounts.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();
                    return RedirectToAction("Edit", new { id = discount.Id });
                }
                return RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            await _discountViewModelService.PrepareDiscountModel(model, discount);

            return View(model);
        }

        //delete
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            var discount = await _discountService.GetDiscountById(id);
            if (discount == null)
                //No discount found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!discount.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return RedirectToAction("Edit", new { id = discount.Id });
            }

            var usagehistory = await _discountService.GetAllDiscountUsageHistory(discount.Id);
            if (usagehistory.Count > 0)
            {
                Error(_translationService.GetResource("admin.marketing.discounts.Deleted.UsageHistory"));
                return RedirectToAction("Edit", new { id = discount.Id });
            }
            if (ModelState.IsValid)
            {
                await _discountViewModelService.DeleteDiscount(discount);
                Success(_translationService.GetResource("admin.marketing.discounts.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = discount.Id });
        }

        #endregion

        #region Discount coupon codes
        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> CouponCodeList(DataSourceRequest command, string discountId)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var couponcodes = await _discountService.GetAllCouponCodesByDiscountId(discount.Id, pageIndex: command.Page - 1, pageSize: command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = couponcodes.Select(x => new
                {
                    Id = x.Id,
                    CouponCode = x.CouponCode,
                    Used = x.Used
                }),
                Total = couponcodes.TotalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> CouponCodeDelete(string discountId, string Id)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var coupon = await _discountService.GetDiscountCodeById(Id);
            if (coupon == null)
                throw new Exception("No coupon code found with the specified id");
            if (ModelState.IsValid)
            {
                if (!coupon.Used)
                    await _discountService.DeleteDiscountCoupon(coupon);
                else
                    return new JsonResult(new DataSourceResult() { Errors = "You can't delete coupon code, it was used" });

                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);

        }
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> CouponCodeInsert(string discountId, string couponCode)
        {
            if (string.IsNullOrEmpty(couponCode))
                throw new Exception("Coupon code can't be empty");

            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            couponCode = couponCode.ToUpper();

            if ((await _discountService.GetDiscountByCouponCode(couponCode)) != null)
                return new JsonResult(new DataSourceResult() { Errors = "Coupon code exists" });
            if (ModelState.IsValid)
            {
                await _discountViewModelService.InsertCouponCode(discountId, couponCode);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }
        #endregion

        #region Discount requirements

        [AcceptVerbs("GET")]
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> GetDiscountRequirementConfigurationUrl(string systemName, string discountId, string discountRequirementId)
        {
            if (String.IsNullOrEmpty(systemName))
                throw new ArgumentNullException("systemName");

            var discountPlugin = _discountService.LoadDiscountProviderBySystemName(systemName);

            if (discountPlugin == null)
                throw new ArgumentException("Discount requirement rule could not be loaded");

            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            var singleRequirement = discountPlugin.GetRequirementRules().Single(x => x.SystemName == systemName);
            string url = _discountViewModelService.GetRequirementUrlInternal(singleRequirement, discount, discountRequirementId);
            return Json(new { url = url });
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> GetDiscountRequirementMetaInfo(string discountRequirementId, string discountId)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            var discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
            if (discountRequirement == null)
                throw new ArgumentException("Discount requirement could not be loaded");

            var discountPlugin = _discountService.LoadDiscountProviderBySystemName(discountRequirement.DiscountRequirementRuleSystemName);
            if (discountPlugin == null)
                throw new ArgumentException("Discount requirement rule could not be loaded");

            var discountRequirementRule = discountPlugin.GetRequirementRules().First(x => x.SystemName == discountRequirement.DiscountRequirementRuleSystemName);
            string url = _discountViewModelService.GetRequirementUrlInternal(discountRequirementRule, discount, discountRequirementId);
            string ruleName = discountRequirementRule.FriendlyName;

            return Json(new { url = url, ruleName = ruleName });
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> DeleteDiscountRequirement(string discountRequirementId, string discountId)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            var discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
            if (discountRequirement == null)
                throw new ArgumentException("Discount requirement could not be loaded");

            if (ModelState.IsValid)
            {
                await _discountViewModelService.DeleteDiscountRequirement(discountRequirement, discount);
                return Json(new { Result = true });
            }
            return ErrorForKendoGridJson(ModelState);
        }

        #endregion

        #region Applied to products

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> ProductList(DataSourceRequest command, string discountId, [FromServices] IProductService productService)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var products = await productService.GetProductsByDiscount(discount.Id, pageIndex: command.Page - 1, pageSize: command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = products.Select(x => new DiscountModel.AppliedToProductModel
                {
                    ProductId = x.Id,
                    ProductName = x.Name
                }),
                Total = products.TotalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ProductDelete(string discountId, string productId, [FromServices] IProductService productService)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var product = await productService.GetProductById(productId);
            if (product == null)
                throw new Exception("No product found with the specified id");

            if (ModelState.IsValid)
            {
                await _discountViewModelService.DeleteProduct(discount, product);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ProductAddPopup(string discountId)
        {
            var model = await _discountViewModelService.PrepareProductToDiscountModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command, DiscountModel.AddProductToDiscountModel model)
        {
            var products = await _discountViewModelService.PrepareProductModel(model, command.Page, command.PageSize);

            var gridModel = new DataSourceResult
            {
                Data = products.products.ToList(),
                Total = products.totalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductAddPopup(DiscountModel.AddProductToDiscountModel model)
        {
            var discount = await _discountService.GetDiscountById(model.DiscountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            if (model.SelectedProductIds != null)
            {
                await _discountViewModelService.InsertProductToDiscountModel(model);
            }

            ViewBag.RefreshPage = true;
            return View(model);
        }

        #endregion

        #region Applied to categories

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> CategoryList(DataSourceRequest command, string discountId, [FromServices] ICategoryService categoryService)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var categories = await categoryService.GetAllCategoriesByDiscount(discount.Id);
            var items = new List<DiscountModel.AppliedToCategoryModel>();
            foreach (var item in categories)
            {
                items.Add(new DiscountModel.AppliedToCategoryModel
                {
                    CategoryId = item.Id,
                    CategoryName = await categoryService.GetFormattedBreadCrumb(item)
                });
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = categories.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> CategoryDelete(string discountId, string categoryId, [FromServices] ICategoryService categoryService)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var category = await categoryService.GetCategoryById(categoryId);
            if (category == null)
                throw new Exception("No category found with the specified id");

            if (ModelState.IsValid)
            {
                await _discountViewModelService.DeleteCategory(discount, category);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public IActionResult CategoryAddPopup(string discountId)
        {
            var model = new DiscountModel.AddCategoryToDiscountModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> CategoryAddPopupList(DataSourceRequest command, DiscountModel.AddCategoryToDiscountModel model, [FromServices] ICategoryService categoryService)
        {
            var categories = await categoryService.GetAllCategories(categoryName: model.SearchCategoryName,
                pageIndex: command.Page - 1, pageSize: command.PageSize, showHidden: true);
            var items = new List<CategoryModel>();
            foreach (var item in categories)
            {
                var categoryModel = item.ToModel();
                categoryModel.Breadcrumb = await categoryService.GetFormattedBreadCrumb(item);
                items.Add(categoryModel);
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = categories.TotalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> CategoryAddPopup(DiscountModel.AddCategoryToDiscountModel model)
        {
            var discount = await _discountService.GetDiscountById(model.DiscountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            if (model.SelectedCategoryIds != null)
            {
                await _discountViewModelService.InsertCategoryToDiscountModel(model);
            }
            ViewBag.RefreshPage = true;
            return View(model);
        }

        #endregion

        #region Applied to brands

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> BrandList(DataSourceRequest command, string discountId, [FromServices] IBrandService brandService)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var brands = await brandService.GetAllBrandsByDiscount(discount.Id);
            var gridModel = new DataSourceResult
            {
                Data = brands.Select(x => new DiscountModel.AppliedToBrandModel
                {
                    BrandId = x.Id,
                    BrandName = x.Name
                }),
                Total = brands.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> BrandDelete(string discountId, string brandId, [FromServices] IBrandService brandService)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var brand = await brandService.GetBrandById(brandId);
            if (brand == null)
                throw new Exception("No brand found with the specified id");
            if (ModelState.IsValid)
            {
                await _discountViewModelService.DeleteBrand(discount, brand);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public IActionResult BrandAddPopup(string discountId)
        {
            var model = new DiscountModel.AddBrandToDiscountModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> BrandAddPopupList(DataSourceRequest command, DiscountModel.AddBrandToDiscountModel model, [FromServices] IBrandService brandService)
        {
            var brands = await brandService.GetAllBrands(model.SearchBrandName, _workContext.CurrentCustomer.StaffStoreId, command.Page - 1, command.PageSize, true);

            var gridModel = new DataSourceResult
            {
                Data = brands.Select(x => x.ToModel()),
                Total = brands.TotalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> BrandAddPopup(DiscountModel.AddBrandToDiscountModel model)
        {
            var discount = await _discountService.GetDiscountById(model.DiscountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            if (model.SelectedBrandIds != null)
            {
                await _discountViewModelService.InsertBrandToDiscountModel(model);
            }
            ViewBag.RefreshPage = true;
            return View(model);
        }

        #endregion

        #region Applied to collections

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> CollectionList(DataSourceRequest command, string discountId, [FromServices] ICollectionService collectionService)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var collections = await collectionService.GetAllCollectionsByDiscount(discount.Id);
            var gridModel = new DataSourceResult
            {
                Data = collections.Select(x => new DiscountModel.AppliedToCollectionModel
                {
                    CollectionId = x.Id,
                    CollectionName = x.Name
                }),
                Total = collections.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> CollectionDelete(string discountId, string collectionId, [FromServices] ICollectionService collectionService)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var collection = await collectionService.GetCollectionById(collectionId);
            if (collection == null)
                throw new Exception("No collection found with the specified id");

            if (ModelState.IsValid)
            {
                await _discountViewModelService.DeleteCollection(discount, collection);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public IActionResult CollectionAddPopup(string discountId)
        {
            var model = new DiscountModel.AddCollectionToDiscountModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> CollectionAddPopupList(DataSourceRequest command, DiscountModel.AddCollectionToDiscountModel model, [FromServices] ICollectionService collectionService)
        {
            var collections = await collectionService.GetAllCollections(model.SearchCollectionName, "",
                command.Page - 1, command.PageSize, true);
            var gridModel = new DataSourceResult
            {
                Data = collections.Select(x => x.ToModel()),
                Total = collections.TotalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> CollectionAddPopup(DiscountModel.AddCollectionToDiscountModel model)
        {
            var discount = await _discountService.GetDiscountById(model.DiscountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            if (model.SelectedCollectionIds != null)
            {
                await _discountViewModelService.InsertCollectionToDiscountModel(model);
            }
            ViewBag.RefreshPage = true;
            return View(model);
        }

        #endregion

        #region Applied to vendors

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> VendorList(DataSourceRequest command, string discountId, [FromServices] IVendorService vendorService)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var vendors = await vendorService.GetAllVendorsByDiscount(discount.Id);
            var gridModel = new DataSourceResult
            {
                Data = vendors.Select(x => new DiscountModel.AppliedToVendorModel
                {
                    VendorId = x.Id,
                    VendorName = x.Name
                }),
                Total = vendors.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> VendorDelete(string discountId, string vendorId, [FromServices] IVendorService vendorService)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            var vendor = await vendorService.GetVendorById(vendorId);
            if (vendor == null)
                throw new Exception("No vendor found with the specified id");
            if (ModelState.IsValid)
            {
                await _discountViewModelService.DeleteVendor(discount, vendor);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public IActionResult VendorAddPopup(string discountId)
        {
            var model = new DiscountModel.AddVendorToDiscountModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> VendorAddPopupList(DataSourceRequest command, DiscountModel.AddVendorToDiscountModel model, [FromServices] IVendorService vendorService)
        {
            var vendors = await vendorService.GetAllVendors(model.SearchVendorName, command.Page - 1, command.PageSize, true);

            //search for emails
            if (!(string.IsNullOrEmpty(model.SearchVendorEmail)))
            {
                var tempVendors = vendors.Where(x => x.Email.ToLowerInvariant().Contains(model.SearchVendorEmail.Trim()));
                vendors = new PagedList<Domain.Vendors.Vendor>(tempVendors, command.Page - 1, command.PageSize);
            }

            var gridModel = new DataSourceResult
            {
                Data = vendors.Select(x => x.ToModel()),
                Total = vendors.TotalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> VendorAddPopup(DiscountModel.AddVendorToDiscountModel model)
        {
            var discount = await _discountService.GetDiscountById(model.DiscountId);
            if (discount == null)
                throw new Exception("No discount found with the specified id");

            if (model.SelectedVendorIds != null)
            {
                await _discountViewModelService.InsertVendorToDiscountModel(model);
            }
            ViewBag.RefreshPage = true;
            return View(model);
        }

        #endregion

        #region Discount usage history

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> UsageHistoryList(string discountId, DataSourceRequest command)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("No discount found with the specified id");

            var (usageHistoryModels, totalCount) = await _discountViewModelService.PrepareDiscountUsageHistoryModel(discount, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = usageHistoryModels.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> UsageHistoryDelete(string discountId, string id)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("No discount found with the specified id");

            var duh = await _discountService.GetDiscountUsageHistoryById(id);
            if (duh != null)
            {
                if (ModelState.IsValid)
                {
                    await _discountService.DeleteDiscountUsageHistory(duh);
                }
                else
                    return ErrorForKendoGridJson(ModelState);
            }
            return new JsonResult("");
        }

        #endregion
    }
}
