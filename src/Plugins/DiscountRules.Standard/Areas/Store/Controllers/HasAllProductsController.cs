using DiscountRules.Standard.Models;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiscountRules.Standard.Areas.Store.Controllers;

public class HasAllProductsController : BaseStorePluginController
{
    private readonly IDiscountService _discountService;
    private readonly IPermissionService _permissionService;
    private readonly IProductService _productService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IVendorService _vendorService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IEnumTranslationService _enumTranslationService;

    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public HasAllProductsController(IDiscountService discountService,
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

    public async Task<IActionResult> Configure(string discountId, string discountRequirementId)
    {
        if (!await AuthorizeManageDiscounts())
            return Content("Access denied");

        var discount = await GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");

        var restrictedProductIds = string.Empty;

        if (!string.IsNullOrEmpty(discountRequirementId))
        {
            var discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
            if (discountRequirement == null)
                return Content("Failed to load requirement.");

            restrictedProductIds = discountRequirement.Metadata;
        }

        var model = new RequirementAllProductsModel {
            RequirementId = !string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "",
            DiscountId = discountId,
            Products = restrictedProductIds
        };

        //add a prefix
        ViewData.TemplateInfo.HtmlFieldPrefix =
            $"DiscountRulesHasAllProducts{discount.Id}-{(!string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "")}";

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> Configure(string discountId, string discountRequirementId, string productIds)
    {
        if (!await AuthorizeManageDiscounts())
            return Content("Access denied");

        var discount = await GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");

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
                DiscountRequirementRuleSystemName = "DiscountRules.HasAllProducts",
                Metadata = productIds
            };
            discount.DiscountRules.Add(discountRequirement);
            await _discountService.UpdateDiscount(discount);
        }

        return new JsonResult(new { Result = true, NewRequirementId = discountRequirement.Id });
    }

    public async Task<IActionResult> ProductAddPopup(string btnId, string productIdsInput)
    {
        if (!await AuthorizeManageProducts())
            return Content("Access denied");

        var model = new RequirementAllProductsModel.AddProductModel();

        //stores - scoped to current store manager's store
        model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var s in await _storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

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
        RequirementAllProductsModel.AddProductModel model)
    {
        if (!await AuthorizeManageProducts())
            return Content("Access denied");

        var searchCategoryIds = new List<string>();
        if (!string.IsNullOrEmpty(model.SearchCategoryId))
            searchCategoryIds.Add(model.SearchCategoryId);

        var products = (await _productService.SearchProducts(
            categoryIds: searchCategoryIds,
            collectionId: model.SearchCollectionId,
            storeId: !string.IsNullOrEmpty(model.SearchStoreId) ? model.SearchStoreId : CurrentStoreId,
            productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true
        )).products;
        var gridModel = new DataSourceResult {
            Data = products.Select(x => new RequirementAllProductsModel.ProductModel {
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
        var result = "";

        if (!await AuthorizeManageProducts())
            return new JsonResult(new { Text = result });

        if (string.IsNullOrWhiteSpace(productIds)) return new JsonResult(new { Text = result });
        var ids = new List<string>();
        var rangeArray = productIds
            .Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        foreach (var str1 in rangeArray)
        {
            var str2 = str1;
            if (str2.Contains(':'))
                str2 = str2[..str2.IndexOf(":", StringComparison.Ordinal)];
            ids.Add(str2);
        }

        var products = await _productService.GetProductsByIds(ids.ToArray(), true);
        for (var i = 0; i <= products.Count - 1; i++)
        {
            result += products[i].Name;
            if (i != products.Count - 1)
                result += ", ";
        }

        return new JsonResult(new { Text = result });
    }

    private async Task<bool> AuthorizeManageDiscounts()
    {
        return await _permissionService.Authorize(StandardPermission.ManageDiscounts);
    }

    private async Task<bool> AuthorizeManageProducts()
    {
        return await _permissionService.Authorize(StandardPermission.ManageProducts);
    }

    private async Task<Discount> GetDiscountById(string discountId)
    {
        return await _discountService.GetDiscountById(discountId);
    }
}
