using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Permissions;
using Grand.Mediator;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Discounts;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Discounts)]
[AutoValidateAntiforgeryToken]
public abstract class BaseDiscountController(
    IDiscountViewModelService discountViewModelService,
    IDiscountService discountService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IDiscountProviderLoader discountProviderLoader,
    IAdminDataScope<Discount> scope)
    : BaseController
{
    #region Discounts

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        var model = discountViewModelService.PrepareDiscountListModel();
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DiscountListModel model, DataSourceRequest command)
    {
        var (discountModel, totalCount) =
            await discountViewModelService.PrepareDiscountModel(model, command.Page, command.PageSize);
        return Json(new DataSourceResult { Data = discountModel.ToList(), Total = totalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new DiscountModel();
        await discountViewModelService.PrepareDiscountModel(model, null);
        model.LimitationTimes = 1;
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(DiscountModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var discount = await discountViewModelService.InsertDiscountModel(model);
            Success(translationService.GetResource("admin.marketing.discounts.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = discount.Id }) : RedirectToAction("List");
        }

        await discountViewModelService.PrepareDiscountModel(model, null);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var discount = await discountService.GetDiscountById(id);
        if (discount == null || !await scope.CanView(discount))
            return RedirectToAction("List");

        var model = discount.ToModel(dateTimeService);
        await discountViewModelService.PrepareDiscountModel(model, discount);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(DiscountModel model, bool continueEditing)
    {
        var discount = await discountService.GetDiscountById(model.Id);
        if (discount == null)
            return RedirectToAction("List");

        if (!await scope.HasAccess(discount))
            return RedirectToAction("Edit", new { id = discount.Id });

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            discount = await discountViewModelService.UpdateDiscountModel(discount, model);
            Success(translationService.GetResource("admin.marketing.discounts.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = discount.Id });
            }

            return RedirectToAction("List");
        }

        await discountViewModelService.PrepareDiscountModel(model, discount);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var discount = await discountService.GetDiscountById(id);
        if (discount == null)
            return RedirectToAction("List");

        if (!await scope.HasAccess(discount))
            return RedirectToAction("Edit", new { id = discount.Id });

        var usageHistory = await mediator.Send(new GetDiscountUsageHistoryQuery { DiscountId = discount.Id });
        if (usageHistory.Count > 0)
        {
            Error(translationService.GetResource("admin.marketing.discounts.Deleted.UsageHistory"));
            return RedirectToAction("Edit", new { id = discount.Id });
        }

        if (ModelState.IsValid)
        {
            await discountViewModelService.DeleteDiscount(discount);
            Success(translationService.GetResource("admin.marketing.discounts.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id = discount.Id });
    }

    #endregion

    #region Discount requirements

    [AcceptVerbs("GET")]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> GetDiscountRequirementConfigurationUrl(string rulesystemName,
        string discountId, string discountRequirementId)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(rulesystemName);

        var discountPlugin = discountProviderLoader.LoadDiscountProviderByRuleSystemName(rulesystemName);
        if (discountPlugin == null)
            throw new ArgumentException("Discount requirement rule could not be loaded");

        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");
        if (!await scope.HasAccess(discount))
            return Json(new { Result = false, Error = "Access denied" });

        var singleRequirement = discountPlugin.GetRequirementRules().FirstOrDefault(x =>
            x.SystemName.Equals(rulesystemName, StringComparison.OrdinalIgnoreCase));
        var url = discountViewModelService.GetRequirementUrlInternal(singleRequirement, discount,
            discountRequirementId);
        return Json(new { url });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> GetDiscountRequirementMetaInfo(string discountRequirementId, string discountId)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");
        if (!await scope.HasAccess(discount))
            return Json(new { Result = false, Error = "Access denied" });

        var discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
        if (discountRequirement == null)
            throw new ArgumentException("Discount requirement could not be loaded");

        var discountPlugin = discountProviderLoader.LoadDiscountProviderByRuleSystemName(
            discountRequirement.DiscountRequirementRuleSystemName);
        if (discountPlugin == null)
            throw new ArgumentException("Discount requirement rule could not be loaded");

        var discountRequirementRule = discountPlugin.GetRequirementRules()
            .First(x => x.SystemName == discountRequirement.DiscountRequirementRuleSystemName);
        var url = discountViewModelService.GetRequirementUrlInternal(discountRequirementRule, discount,
            discountRequirementId);
        return Json(new { url, ruleName = discountRequirementRule.FriendlyName });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> DeleteDiscountRequirement(string discountRequirementId, string discountId)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");
        if (!await scope.HasAccess(discount))
            return Json(new { Result = false, Error = "Access denied" });

        var discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
        if (discountRequirement == null)
            throw new ArgumentException("Discount requirement could not be loaded");

        if (ModelState.IsValid)
        {
            await discountViewModelService.DeleteDiscountRequirement(discountRequirement, discount);
            return Json(new { Result = true });
        }
        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Discount coupon codes

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> CouponCodeList(DataSourceRequest command, string discountId)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var couponcodes = await discountService.GetAllCouponCodesByDiscountId(discount.Id,
            command.Page - 1, command.PageSize);
        return Json(new DataSourceResult {
            Data = couponcodes.Select(x => new { x.Id, x.CouponCode, x.Used }),
            Total = couponcodes.TotalCount
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CouponCodeDelete(string discountId, string id)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var coupon = await discountService.GetDiscountCodeById(id);
        if (coupon == null)
            throw new Exception("No coupon code found with the specified id");
        if (ModelState.IsValid)
        {
            if (!coupon.Used)
                await discountService.DeleteDiscountCoupon(coupon);
            else
                return new JsonResult(new DataSourceResult { Errors = "You can't delete coupon code, it was used" });
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CouponCodeInsert(string discountId, string couponCode)
    {
        if (string.IsNullOrEmpty(couponCode))
            throw new Exception("Coupon code can't be empty");

        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        couponCode = couponCode.ToUpperInvariant();

        if (await discountService.GetDiscountByCouponCode(couponCode) != null)
            return new JsonResult(new DataSourceResult { Errors = "Coupon code exists" });
        if (ModelState.IsValid)
        {
            await discountViewModelService.InsertCouponCode(discountId, couponCode);
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Applied to products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductList(DataSourceRequest command, string discountId,
        [FromServices] IProductService productService)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.CanView(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var products = await productService.GetProductsByDiscount(discount.Id, command.Page - 1, command.PageSize);
        return Json(new DataSourceResult {
            Data = products.Select(x => new DiscountModel.AppliedToProductModel { ProductId = x.Id, ProductName = x.Name }),
            Total = products.TotalCount
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductDelete(string discountId, string productId,
        [FromServices] IProductService productService)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new Exception("No product found with the specified id");

        if (ModelState.IsValid)
        {
            await discountViewModelService.DeleteProduct(discount, product);
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAddPopup(string discountId)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var model = await discountViewModelService.PrepareProductToDiscountModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command,
        DiscountModel.AddProductToDiscountModel model)
    {
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;
        var products = await discountViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        return Json(new DataSourceResult { Data = products.products.ToList(), Total = products.totalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopup(DiscountModel.AddProductToDiscountModel model)
    {
        var discount = await discountService.GetDiscountById(model.DiscountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return Content("Access denied");

        if (model.SelectedProductIds != null) await discountViewModelService.InsertProductToDiscountModel(model);
        return Content("");
    }

    #endregion

    #region Applied to categories

    // CategoryList/CategoryDelete/CategoryAddPopup(POST) below preserve Store's original strict
    // AccessToEntityByStore check (widened here to scope.CanView/HasAccess) and widen Admin's
    // original, which had no check at all, to match Store. CategoryAddPopup(GET)/CategoryAddPopupList
    // are the genuine both-hosts gap: neither original guarded them, so applying scope.HasAccess
    // here is real disclosed security hardening, not behavior preservation.

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> CategoryList(DataSourceRequest command, string discountId,
        [FromServices] ICategoryService categoryService)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.CanView(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var categories = await categoryService.GetAllCategoriesByDiscount(discount.Id);
        var items = new List<DiscountModel.AppliedToCategoryModel>();
        foreach (var item in categories)
            items.Add(new DiscountModel.AppliedToCategoryModel {
                CategoryId = item.Id, CategoryName = await categoryService.GetFormattedBreadCrumb(item)
            });
        return Json(new DataSourceResult { Data = items, Total = categories.Count });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CategoryDelete(string discountId, string categoryId,
        [FromServices] ICategoryService categoryService)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var category = await categoryService.GetCategoryById(categoryId);
        if (category == null)
            throw new Exception("No category found with the specified id");

        if (ModelState.IsValid)
        {
            await discountViewModelService.DeleteCategory(discount, category);
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CategoryAddPopup(string discountId)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        return View(new DiscountModel.AddCategoryToDiscountModel());
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CategoryAddPopupList(DataSourceRequest command,
        DiscountModel.AddCategoryToDiscountModel model, [FromServices] ICategoryService categoryService)
    {
        var categories = await categoryService.GetAllCategories(parentId: null, categoryName: model.SearchCategoryName,
            storeId: scope.DefaultStoreId ?? "", pageIndex: command.Page - 1, pageSize: command.PageSize, showHidden: true);
        var items = new List<CategoryModel>();
        foreach (var item in categories)
        {
            var categoryModel = item.ToModel();
            categoryModel.Breadcrumb = await categoryService.GetFormattedBreadCrumb(item);
            items.Add(categoryModel);
        }
        return Json(new DataSourceResult { Data = items, Total = categories.TotalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CategoryAddPopup(DiscountModel.AddCategoryToDiscountModel model)
    {
        var discount = await discountService.GetDiscountById(model.DiscountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return Content("Access denied");

        if (model.SelectedCategoryIds != null) await discountViewModelService.InsertCategoryToDiscountModel(model);
        return Content("");
    }

    #endregion

    #region Applied to brands

    // BrandList/BrandDelete/BrandAddPopup(POST) below preserve Store's original strict
    // AccessToEntityByStore check (widened here to scope.CanView/HasAccess) and widen Admin's
    // original, which had no check at all, to match Store. BrandAddPopup(GET)/BrandAddPopupList
    // are the genuine both-hosts gap: neither original guarded them, so applying scope.HasAccess
    // here is real disclosed security hardening, not behavior preservation.

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> BrandList(DataSourceRequest command, string discountId,
        [FromServices] IBrandService brandService)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.CanView(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var brands = await brandService.GetAllBrandsByDiscount(discount.Id);
        return Json(new DataSourceResult {
            Data = brands.Select(x => new DiscountModel.AppliedToBrandModel { BrandId = x.Id, BrandName = x.Name }),
            Total = brands.Count
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> BrandDelete(string discountId, string brandId,
        [FromServices] IBrandService brandService)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var brand = await brandService.GetBrandById(brandId);
        if (brand == null)
            throw new Exception("No brand found with the specified id");
        if (ModelState.IsValid)
        {
            await discountViewModelService.DeleteBrand(discount, brand);
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> BrandAddPopup(string discountId)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        return View(new DiscountModel.AddBrandToDiscountModel());
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BrandAddPopupList(DataSourceRequest command,
        DiscountModel.AddBrandToDiscountModel model, [FromServices] IBrandService brandService)
    {
        var brands = await brandService.GetAllBrands(model.SearchBrandName,
            scope.DefaultStoreId ?? "", command.Page - 1, command.PageSize, true);
        return Json(new DataSourceResult { Data = brands.Select(x => x.ToModel()), Total = brands.TotalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BrandAddPopup(DiscountModel.AddBrandToDiscountModel model)
    {
        var discount = await discountService.GetDiscountById(model.DiscountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return Content("Access denied");

        if (model.SelectedBrandIds != null) await discountViewModelService.InsertBrandToDiscountModel(model);
        return Content("");
    }

    #endregion

    #region Applied to collections

    // CollectionList/CollectionDelete/CollectionAddPopup(POST) below preserve Store's original strict
    // AccessToEntityByStore check (widened here to scope.CanView/HasAccess) and widen Admin's
    // original, which had no check at all, to match Store. CollectionAddPopup(GET)/CollectionAddPopupList
    // are the genuine both-hosts gap: neither original guarded them, so applying scope.HasAccess
    // here is real disclosed security hardening, not behavior preservation.

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> CollectionList(DataSourceRequest command, string discountId,
        [FromServices] ICollectionService collectionService)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.CanView(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var collections = await collectionService.GetAllCollectionsByDiscount(discount.Id);
        return Json(new DataSourceResult {
            Data = collections.Select(x => new DiscountModel.AppliedToCollectionModel { CollectionId = x.Id, CollectionName = x.Name }),
            Total = collections.Count
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CollectionDelete(string discountId, string collectionId,
        [FromServices] ICollectionService collectionService)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var collection = await collectionService.GetCollectionById(collectionId);
        if (collection == null)
            throw new Exception("No collection found with the specified id");
        if (ModelState.IsValid)
        {
            await discountViewModelService.DeleteCollection(discount, collection);
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CollectionAddPopup(string discountId)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        return View(new DiscountModel.AddCollectionToDiscountModel());
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CollectionAddPopupList(DataSourceRequest command,
        DiscountModel.AddCollectionToDiscountModel model, [FromServices] ICollectionService collectionService)
    {
        var collections = await collectionService.GetAllCollections(model.SearchCollectionName,
            scope.DefaultStoreId ?? "", command.Page - 1, command.PageSize, true);
        return Json(new DataSourceResult { Data = collections.Select(x => x.ToModel()), Total = collections.TotalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CollectionAddPopup(DiscountModel.AddCollectionToDiscountModel model)
    {
        var discount = await discountService.GetDiscountById(model.DiscountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return Content("Access denied");

        if (model.SelectedCollectionIds != null) await discountViewModelService.InsertCollectionToDiscountModel(model);
        return Content("");
    }

    #endregion

    #region Discount usage history

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> UsageHistoryList(string discountId, DataSourceRequest command)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var (usageHistoryModels, totalCount) =
            await discountViewModelService.PrepareDiscountUsageHistoryModel(discount, command.Page,
                command.PageSize);
        var gridModel = new DataSourceResult {
            Data = usageHistoryModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UsageHistoryDelete(string discountId, string id)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var duh = await discountService.GetDiscountUsageHistoryById(id);
        if (duh != null)
        {
            if (ModelState.IsValid)
                await discountService.DeleteDiscountUsageHistory(duh);
            else
                return ErrorForKendoGridJson(ModelState);
        }

        return new JsonResult("");
    }

    #endregion
}
