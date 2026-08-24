using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseProductController (ARCH-001 Phase 1 Task 11). All 24 regions of
// behavior live in the shared base; this class only supplies Store's DI wiring, the EditWarningCheck
// hook, and the attributes that used to arrive transitively via BaseStoreController -
// BaseProductController can't inherit any single host's base controller (it's shared across
// Admin/Store/Vendor, each with a different [Area]/[Authorize*] pair), so each subclass restates its
// own host's attribute set explicitly.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
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
    IAdminDataScope<Product> scope)
    : BaseProductController(productViewModelService, productService, inventoryManageService, languageService,
        translationService, productReservationService, auctionService, dateTimeService, permissionService,
        enumTranslationService, scope)
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
            (product.LimitedToStores &&
             product.Stores.Contains(Scope.DefaultStoreId) &&
             product.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Catalog.Products.Permissions"));
    }
}
