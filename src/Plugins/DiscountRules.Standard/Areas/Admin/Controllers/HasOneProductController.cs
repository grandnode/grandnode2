using DiscountRules.Standard.Models;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiscountRules.Standard.Areas.Admin.Controllers;

public class HasOneProductController : BaseAdminPluginController
{
    private readonly IDiscountService _discountService;
    private readonly IPermissionService _permissionService;
    private readonly IProductService _productService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IVendorService _vendorService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IEnumTranslationService _enumTranslationService;

    private Vendor CurrentVendor => _contextAccessor.WorkContext.CurrentVendor;

    public HasOneProductController(IDiscountService discountService,
        IPermissionService permissionService,
        IContextAccessor contextAccessor,
        ITranslationService translationService,
        IStoreService storeService,
        IVendorService vendorService,
        IProductService productService,
        IEnumTranslationService enumTranslationService)
    {
        _discountService = discountService;
        _permissionService = permissionService;
        _contextAccessor = contextAccessor;
        _translationService = translationService;
        _storeService = storeService;
        _vendorService = vendorService;
        _productService = productService;
        _enumTranslationService = enumTranslationService;
    }

    private async Task<IActionResult> AuthorizeAsync(Permission permission)
    {
        if (!await _permissionService.Authorize(permission))
            return Content("Access denied");
        return null;
    }

    private async Task<Discount> GetDiscountAsync(string discountId)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");
        return discount;
    }

    public async Task<IActionResult> Configure(string discountId, string discountRequirementId)
    {
        var authResult = await AuthorizeAsync(StandardPermission.ManageDiscounts);
        if (authResult != null) return authResult;

        var discount = await GetDiscountAsync(discountId);

        var restrictedProductIds = string.Empty;
        if (!string.IsNullOrEmpty(discountRequirementId))
        {
            var discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
            if (discountRequirement == null)
                return Content("Failed to load requirement.");

            restrictedProductIds = discountRequirement.Metadata;
        }

        var model = new RequirementOneProductModel {
            RequirementId = !string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "",
            DiscountId = discountId,
            Products = restrictedProductIds
        };

        //add a prefix
        ViewData.TemplateInfo.HtmlFieldPrefix =
            $"DiscountRulesHasOneProduct{discount.Id}-{(!string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "")}";

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> Configure(string discountId, string discountRequirementId, string productIds)
    {
        var authResult = await AuthorizeAsync(StandardPermission.ManageDiscounts);
        if (authResult != null) return authResult;

        var discount = await GetDiscountAsync(discountId);

        DiscountRule discountRequirement = null;
        if (!string.IsNullOrEmpty(discountRequirementId))
            discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);

        if (discountRequirement != null)
        {
            //update existing rule
            discountRequirement.Metadata = productIds;
            await _discountService.UpdateDiscount(discount);
        }
        else
        {
            //save new rule
            discountRequirement = new DiscountRule {
                DiscountRequirementRuleSystemName = "DiscountRules.HasOneProduct",
                Metadata = productIds
            };
            discount.DiscountRules.Add(discountRequirement);
            await _discountService.UpdateDiscount(discount);
        }

        return new JsonResult(new { Result = true, NewRequirementId = discountRequirement.Id });
    }

    public async Task<IActionResult> ProductAddPopup(string btnId, string productIdsInput)
    {
        var authResult = await AuthorizeAsync(StandardPermission.ManageProducts);
        if (authResult != null) return authResult;

        var model = new RequirementOneProductModel.AddProductModel {
            //a vendor should have access only to his products
            IsLoggedInAsVendor = CurrentVendor != null
        };

        //stores
        model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var s in await _storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        //vendors
        model.AvailableVendors.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
            model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id });

        //product types
        model.AvailableProductTypes = _enumTranslationService.ToSelectList(ProductType.SimpleProduct, false).ToList();
        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

        ViewBag.productIdsInput = productIdsInput;
        ViewBag.btnId = btnId;

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command,
        RequirementOneProductModel.AddProductModel model)
    {
        var authResult = await AuthorizeAsync(StandardPermission.ManageProducts);
        if (authResult != null) return authResult;

        //a vendor should have access only to his products
        if (CurrentVendor != null) model.SearchVendorId = CurrentVendor.Id;

        var searchCategoryIds = new List<string>();
        if (!string.IsNullOrEmpty(model.SearchCategoryId))
            searchCategoryIds.Add(model.SearchCategoryId);

        var products = (await _productService.SearchProducts(
            categoryIds: searchCategoryIds,
            collectionId: model.SearchCollectionId,
            storeId: model.SearchStoreId,
            vendorId: model.SearchVendorId,
            productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true
        )).products;
        var gridModel = new DataSourceResult {
            Data = products.Select(x => new RequirementOneProductModel.ProductModel {
                Id = x.Id,
                Name = x.Name,
                Published = x.Published
            }),
            Total = products.TotalCount
        };

        return new JsonResult(gridModel);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> LoadProductFriendlyNames(string productIds)
    {
        var authResult = await AuthorizeAsync(StandardPermission.ManageProducts);
        if (authResult != null) return authResult;

        var result = "";

        if (string.IsNullOrWhiteSpace(productIds)) return new JsonResult(new { Text = result });
        var ids = productIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split(':')[0].Trim())
            .ToList();

        var products = await _productService.GetProductsByIds(ids.ToArray(), true);
        result = string.Join(", ", products.Select(p => p.Name));

        return new JsonResult(new { Text = result });
    }
}
