using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
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
public class ProductBundleController(
    IProductAddPopupViewModelService productAddPopupViewModelService,
    IProductBundleViewModelService productBundleViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseProductBundleController(productAddPopupViewModelService, productBundleViewModelService, productService, translationService, scope)
{
    // Vendor's original AddPopup(POST) actions returned Content(ModelState.GetErrors()) on an invalid
    // model, instead of Admin/Store's re-prepare-and-View. Overriding the base's Admin/Store default,
    // per each hook's own doc comment.
    protected override Task<IActionResult> InvalidBundleProductAddPopupResult(ProductModel.AddBundleProductModel model)
        => Task.FromResult<IActionResult>(Content(ModelState.GetErrors()));
}
