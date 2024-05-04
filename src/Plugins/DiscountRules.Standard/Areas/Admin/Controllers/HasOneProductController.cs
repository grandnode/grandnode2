using DiscountRules.Standard.Models;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
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
    private readonly IWorkContext _workContext;

    public HasOneProductController(IDiscountService discountService,
        IPermissionService permissionService,
        IWorkContext workContext,
        ITranslationService translationService,
        IStoreService storeService,
        IVendorService vendorService,
        IProductService productService)
    {
        _discountService = discountService;
        _permissionService = permissionService;
        _workContext = workContext;
        _translationService = translationService;
        _storeService = storeService;
        _vendorService = vendorService;
        _productService = productService;
    }

    public async Task<IActionResult> Configure(string discountId, string discountRequirementId)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageDiscounts))
            return Content("Access denied");

        var discount = await _discountService.GetDiscountById(discountId);
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
        if (!await _permissionService.Authorize(StandardPermission.ManageDiscounts))
            return Content("Access denied");

        var discount = await _discountService.GetDiscountById(discountId);
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
        if (!await _permissionService.Authorize(StandardPermission.ManageProducts))
            return Content("Access denied");

        var model = new RequirementOneProductModel.AddProductModel {
            //a vendor should have access only to his products
            IsLoggedInAsVendor = _workContext.CurrentVendor != null
        };

        //stores
        model.AvailableStores.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var s in await _storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        //vendors
        model.AvailableVendors.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
            model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id });

        //product types
        model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(HttpContext, false).ToList();
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
        if (!await _permissionService.Authorize(StandardPermission.ManageProducts))
            return Content("Access denied");

        //a vendor should have access only to his products
        if (_workContext.CurrentVendor != null) model.SearchVendorId = _workContext.CurrentVendor.Id;

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
        var result = "";

        if (!await _permissionService.Authorize(StandardPermission.ManageProducts))
            return new JsonResult(new { Text = result });

        if (string.IsNullOrWhiteSpace(productIds)) return new JsonResult(new { Text = result });
        var ids = new List<string>();
        var rangeArray = productIds
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        //we support three ways of specifying products:
        //1. The comma-separated list of product identifiers (e.g. 77, 123, 156).
        //2. The comma-separated list of product identifiers with quantities.
        //      {Product ID}:{Quantity}. For example, 77:1, 123:2, 156:3
        //3. The comma-separated list of product identifiers with quantity range.
        //      {Product ID}:{Min quantity}-{Max quantity}. For example, 77:1-3, 123:2-5, 156:3-8
        foreach (var str1 in rangeArray)
        {
            var str2 = str1;
            //we do not display specified quantities and ranges
            //parse only product names (before : sign)
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
}