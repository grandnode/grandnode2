using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class ProductController(
    IProductViewModelService productViewModelService,
    IProductAddPopupViewModelService productAddPopupViewModelService,
    IProductService productService,
    ILanguageService languageService,
    IRequestTranslationService translationService,
    IDateTimeService dateTimeService,
    IRequestPermissionService permissionService,
    IAdminDataScope<Product> scope)
    : BaseProductController(productViewModelService, productAddPopupViewModelService, productService, languageService, translationService,
        dateTimeService, permissionService, scope)
{
    // Re-derived from the original Store ProductController.Edit(GET) (pre-Task-11:
    // src/Web/Grand.Web.Store/Controllers/ProductController.cs:184-189), not retyped from memory - the
    // condition is unusual (warns when NOT limited to stores at all, or when limited AND the staff
    // member's store is one of several) and easy to get backwards. Scope.DefaultStoreId is exactly
    // StaffStoreId for Store (StoreAdminDataScope.DefaultStoreId => CurrentCustomer.StaffStoreId), so it
    // stands in for the original's direct IContextAccessor access without reintroducing that dependency,
    // which Tasks 7/8 deliberately eliminated from this call path.
    protected override void EditWarningCheck(Product product)
    {
        if (!product.LimitedToStores ||
            (product.Stores.Contains(Scope.DefaultStoreId) &&
             product.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Catalog.Products.Permissions"));
    }
}
