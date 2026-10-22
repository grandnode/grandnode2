using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
public class ProductAssociatedController(
    IProductViewModelService productViewModelService,
    IProductAssociatedViewModelService productAssociatedViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope,
    IContextAccessor contextAccessor)
    : BaseProductAssociatedController(productViewModelService, productAssociatedViewModelService, productService, translationService, scope)
{
    // Vendor's original passed CurrentVendor.Id into GetAssociatedProducts(vendorId:) so a vendor only
    // sees the subset of a grouped product's associated products that they themselves own. Overriding
    // the base's empty default, per BaseProductAssociatedController.AssociatedProductVendorId's own doc comment.
    protected override string AssociatedProductVendorId => contextAccessor.WorkContext.CurrentVendor.Id;
}
