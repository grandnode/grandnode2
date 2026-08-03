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
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Discounts;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.Discounts)]
public class DiscountController : BaseStoreController
{
    #region Constructors

    public DiscountController(
        IDiscountViewModelService discountViewModelService,
        IDiscountService discountService,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        IDateTimeService dateTimeService,
        IMediator mediator)
    {
        _discountViewModelService = discountViewModelService;
        _discountService = discountService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
        _dateTimeService = dateTimeService;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IDiscountViewModelService _discountViewModelService;
    private readonly IDiscountService _discountService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IDateTimeService _dateTimeService;
    private readonly IMediator _mediator;

    #endregion

    #region Utilities

    private string CurrentStoreId =>
        _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    #endregion

    #region Discounts

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        var model = _discountViewModelService.PrepareDiscountListModel();
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DiscountListModel model, DataSourceRequest command)
    {
        var (discountModel, totalCount) =
            await _discountViewModelService.PrepareDiscountModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = discountModel.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new DiscountModel();
        await _discountViewModelService.PrepareDiscountModel(model, null);
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
            model.Stores = [CurrentStoreId];
            var discount = await _discountViewModelService.InsertDiscountModel(model);
            Success(_translationService.GetResource("admin.marketing.discounts.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = discount.Id })
                : RedirectToAction("List");
        }

        await _discountViewModelService.PrepareDiscountModel(model, null);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var discount = await _discountService.GetDiscountById(id);
        if (discount == null)
            return RedirectToAction("List");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        var model = discount.ToModel(_dateTimeService);
        await _discountViewModelService.PrepareDiscountModel(model, discount);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(DiscountModel model, bool continueEditing)
    {
        var discount = await _discountService.GetDiscountById(model.Id);
        if (discount == null)
            return RedirectToAction("List");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("Edit", new { id = discount.Id });

        if (ModelState.IsValid)
        {
            model.Stores = [CurrentStoreId];
            discount = await _discountViewModelService.UpdateDiscountModel(discount, model);
            Success(_translationService.GetResource("admin.marketing.discounts.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = discount.Id });
            }

            return RedirectToAction("List");
        }

        await _discountViewModelService.PrepareDiscountModel(model, discount);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var discount = await _discountService.GetDiscountById(id);
        if (discount == null)
            return RedirectToAction("List");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("Edit", new { id });

        var usageHistory = await _mediator.Send(new GetDiscountUsageHistoryQuery { DiscountId = discount.Id });
        if (usageHistory.Count > 0)
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

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var couponcodes = await _discountService.GetAllCouponCodesByDiscountId(discount.Id,
            command.Page - 1, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = couponcodes.Select(x => new {
                x.Id,
                x.CouponCode,
                x.Used
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

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var coupon = await _discountService.GetDiscountCodeById(Id);
        if (coupon == null)
            throw new Exception("No coupon code found with the specified id");
        if (ModelState.IsValid)
        {
            if (!coupon.Used)
                await _discountService.DeleteDiscountCoupon(coupon);
            else
                return new JsonResult(new DataSourceResult
                    { Errors = "You can't delete coupon code, it was used" });

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

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        couponCode = couponCode.ToUpperInvariant();

        if (await _discountService.GetDiscountByCouponCode(couponCode) != null)
            return new JsonResult(new DataSourceResult { Errors = "Coupon code exists" });
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
    public async Task<IActionResult> GetDiscountRequirementConfigurationUrl(string rulesystemName,
        string discountId, string discountRequirementId,
        [FromServices] IDiscountProviderLoader discountProviderLoader)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(rulesystemName);

        var discountPlugin = discountProviderLoader.LoadDiscountProviderByRuleSystemName(rulesystemName);

        if (discountPlugin == null)
            throw new ArgumentException("Discount requirement rule could not be loaded");

        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            throw new ArgumentException("Access denied");

        var singleRequirement = discountPlugin.GetRequirementRules().FirstOrDefault(x =>
            x.SystemName.Equals(rulesystemName, StringComparison.OrdinalIgnoreCase));
        var url = _discountViewModelService.GetRequirementUrlInternal(singleRequirement, discount, discountRequirementId);
        return Json(new { url });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> GetDiscountRequirementMetaInfo(string discountRequirementId, string discountId,
        [FromServices] IDiscountProviderLoader discountProviderLoader)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            throw new ArgumentException("Access denied");

        var discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
        if (discountRequirement == null)
            throw new ArgumentException("Discount requirement could not be loaded");

        var discountPlugin =
            discountProviderLoader.LoadDiscountProviderByRuleSystemName(discountRequirement
                .DiscountRequirementRuleSystemName);
        if (discountPlugin == null)
            throw new ArgumentException("Discount requirement rule could not be loaded");

        var discountRequirementRule = discountPlugin.GetRequirementRules()
            .First(x => x.SystemName == discountRequirement.DiscountRequirementRuleSystemName);
        var url = _discountViewModelService.GetRequirementUrlInternal(discountRequirementRule, discount, discountRequirementId);
        var ruleName = discountRequirementRule.FriendlyName;

        return Json(new { url, ruleName });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> DeleteDiscountRequirement(string discountRequirementId, string discountId)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return Json(new { Result = false, Error = "Access denied" });

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
    public async Task<IActionResult> ProductList(DataSourceRequest command, string discountId,
        [FromServices] IProductService productService)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var products = await productService.GetProductsByDiscount(discount.Id, command.Page - 1,
            command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.Select(x => new DiscountModel.AppliedToProductModel {
                ProductId = x.Id,
                ProductName = x.Name
            }),
            Total = products.TotalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductDelete(string discountId, string productId,
        [FromServices] IProductService productService)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

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
    public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command,
        DiscountModel.AddProductToDiscountModel model)
    {
        model.SearchStoreId = CurrentStoreId;
        var products = await _discountViewModelService.PrepareProductModel(model, command.Page, command.PageSize);

        var gridModel = new DataSourceResult {
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

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return Content("Access denied");

        if (model.SelectedProductIds != null) await _discountViewModelService.InsertProductToDiscountModel(model);

        return Content("");
    }

    #endregion

    #region Applied to categories

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> CategoryList(DataSourceRequest command, string discountId,
        [FromServices] ICategoryService categoryService)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var categories = await categoryService.GetAllCategoriesByDiscount(discount.Id);
        var items = new List<DiscountModel.AppliedToCategoryModel>();
        foreach (var item in categories)
            items.Add(new DiscountModel.AppliedToCategoryModel {
                CategoryId = item.Id,
                CategoryName = await categoryService.GetFormattedBreadCrumb(item)
            });

        var gridModel = new DataSourceResult {
            Data = items,
            Total = categories.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CategoryDelete(string discountId, string categoryId,
        [FromServices] ICategoryService categoryService)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

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
    public async Task<IActionResult> CategoryAddPopupList(DataSourceRequest command,
        DiscountModel.AddCategoryToDiscountModel model, [FromServices] ICategoryService categoryService)
    {
        var categories = await categoryService.GetAllCategories(categoryName: model.SearchCategoryName,
            storeId: CurrentStoreId, pageIndex: command.Page - 1, pageSize: command.PageSize, showHidden: true);
        var items = new List<CategoryModel>();
        foreach (var item in categories)
        {
            var categoryModel = item.ToModel();
            categoryModel.Breadcrumb = await categoryService.GetFormattedBreadCrumb(item);
            items.Add(categoryModel);
        }

        var gridModel = new DataSourceResult {
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

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return Content("Access denied");

        if (model.SelectedCategoryIds != null) await _discountViewModelService.InsertCategoryToDiscountModel(model);

        return Content("");
    }

    #endregion

    #region Applied to brands

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> BrandList(DataSourceRequest command, string discountId,
        [FromServices] IBrandService brandService)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var brands = await brandService.GetAllBrandsByDiscount(discount.Id);
        var gridModel = new DataSourceResult {
            Data = brands.Select(x => new DiscountModel.AppliedToBrandModel {
                BrandId = x.Id,
                BrandName = x.Name
            }),
            Total = brands.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> BrandDelete(string discountId, string brandId,
        [FromServices] IBrandService brandService)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

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
    public async Task<IActionResult> BrandAddPopupList(DataSourceRequest command,
        DiscountModel.AddBrandToDiscountModel model, [FromServices] IBrandService brandService)
    {
        var brands = await brandService.GetAllBrands(model.SearchBrandName,
            CurrentStoreId, command.Page - 1, command.PageSize, true);

        var gridModel = new DataSourceResult {
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

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return Content("Access denied");

        if (model.SelectedBrandIds != null) await _discountViewModelService.InsertBrandToDiscountModel(model);

        return Content("");
    }

    #endregion

    #region Applied to collections

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> CollectionList(DataSourceRequest command, string discountId,
        [FromServices] ICollectionService collectionService)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var collections = await collectionService.GetAllCollectionsByDiscount(discount.Id);
        var gridModel = new DataSourceResult {
            Data = collections.Select(x => new DiscountModel.AppliedToCollectionModel {
                CollectionId = x.Id,
                CollectionName = x.Name
            }),
            Total = collections.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CollectionDelete(string discountId, string collectionId,
        [FromServices] ICollectionService collectionService)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

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
    public async Task<IActionResult> CollectionAddPopupList(DataSourceRequest command,
        DiscountModel.AddCollectionToDiscountModel model, [FromServices] ICollectionService collectionService)
    {
        var collections = await collectionService.GetAllCollections(model.SearchCollectionName, CurrentStoreId,
            command.Page - 1, command.PageSize, true);
        var gridModel = new DataSourceResult {
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

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return Content("Access denied");

        if (model.SelectedCollectionIds != null) await _discountViewModelService.InsertCollectionToDiscountModel(model);

        return Content("");
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

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var (usageHistoryModels, totalCount) =
            await _discountViewModelService.PrepareDiscountUsageHistoryModel(discount, command.Page,
                command.PageSize);
        var gridModel = new DataSourceResult {
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

        if (!discount.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var duh = await _discountService.GetDiscountUsageHistoryById(id);
        if (duh != null)
        {
            if (ModelState.IsValid)
                await _discountService.DeleteDiscountUsageHistory(duh);
            else
                return ErrorForKendoGridJson(ModelState);
        }

        return new JsonResult("");
    }

    #endregion
}
