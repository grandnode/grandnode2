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
