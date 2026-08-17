using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

// Reduced to a thin subclass of BaseProductController (ARCH-001 Phase 1 Task 11). All 24 regions of
// behavior live in the shared base; this class only supplies Vendor's DI wiring, the attributes that
// used to arrive transitively via BaseVendorController, and the 6 vendor-specific hooks
// BaseProductController's own doc comments explicitly assign to "a future Vendor subclass" once hosts
// are subclassed onto it (this task). BaseProductController can't inherit any single host's base
// controller (it's shared across Admin/Store/Vendor, each with a different [Area]/[Authorize*] pair),
// so each subclass restates its own host's attribute set explicitly. No EditWarningCheck override
// needed - Vendor's original had no equivalent branch.
//
// NOT wired into DI yet: this file compiles fine (it references AdminShared's IProductViewModelService
// directly), but Vendor's DI container still only registers its own old, duplicate
// Grand.Web.Vendor.Interfaces.IProductViewModelService/ProductViewModelService - nothing registers
// AdminShared's IProductViewModelService for Vendor yet, so this constructor cannot be resolved at
// runtime until Task 12 deletes Vendor's duplicate and rewires DI to AdminShared's implementation (see
// Task 11's plan Step 4). Left as an uncommitted working-tree change per plan Step 5 until Task 12.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
public class ProductController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IInventoryManageService inventoryManageService,
    ILanguageService languageService,
    ITranslationService translationService,
    IProductReservationService productReservationService,
    IAuctionService auctionService,
    IDateTimeService dateTimeService,
    IPermissionService permissionService,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<Product> scope,
    IContextAccessor contextAccessor)
    : BaseProductController(productViewModelService, productService, inventoryManageService, languageService,
        translationService, productReservationService, auctionService, dateTimeService, permissionService,
        enumTranslationService, scope)
{
    // Vendor's original passed CurrentVendor.Id into GetAssociatedProducts(vendorId:) so a vendor only
    // sees the subset of a grouped product's associated products that they themselves own. Overriding
    // the base's empty default, per BaseProductController.AssociatedProductVendorId's own doc comment.
    protected override string AssociatedProductVendorId => contextAccessor.WorkContext.CurrentVendor.Id;

    // Vendor's original AddPopup(POST) actions returned Content(ModelState.GetErrors()) on an invalid
    // model, instead of Admin/Store's re-prepare-and-View. Overriding the base's Admin/Store default,
    // per each hook's own doc comment.
    protected override Task<IActionResult> InvalidRelatedProductAddPopupResult(ProductModel.AddRelatedProductModel model)
        => Task.FromResult<IActionResult>(Content(ModelState.GetErrors()));

    protected override Task<IActionResult> InvalidSimilarProductAddPopupResult(ProductModel.AddSimilarProductModel model)
        => Task.FromResult<IActionResult>(Content(ModelState.GetErrors()));

    protected override Task<IActionResult> InvalidBundleProductAddPopupResult(ProductModel.AddBundleProductModel model)
        => Task.FromResult<IActionResult>(Content(ModelState.GetErrors()));

    protected override Task<IActionResult> InvalidCrossSellProductAddPopupResult(ProductModel.AddCrossSellProductModel model)
        => Task.FromResult<IActionResult>(Content(ModelState.GetErrors()));

    protected override Task<IActionResult> InvalidRecommendedProductAddPopupResult(ProductModel.AddRecommendedProductModel model)
        => Task.FromResult<IActionResult>(Content(ModelState.GetErrors()));
}
