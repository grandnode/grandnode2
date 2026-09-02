using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseBrandController (ARCH-001 Brand consolidation). All regions of
// behavior live in the shared base; this class only supplies Store's DI wiring, the
// EditWarningCheck hook, and the attributes that used to arrive transitively via
// BaseStoreController. Same pattern as CategoryController/CollectionController (see those files).
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class BrandController(
    IBrandViewModelService brandViewModelService,
    IBrandService brandService,
    IStoreService storeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IPictureViewModelService pictureViewModelService,
    IAdminDataScope<Brand> scope)
    : BaseBrandController(brandViewModelService, brandService, storeService, languageService,
        translationService, pictureViewModelService, scope)
{
    // Re-derived from the original Store BrandController.Edit(GET) (pre-cutover:
    // src/Web/Grand.Web.Store/Controllers/BrandController.cs, lines ~138-148) - the condition is
    // unusual (warns when NOT limited to stores at all, or when limited AND the staff member's
    // store is one of several) and easy to get backwards. Scope.DefaultStoreId is exactly
    // StaffStoreId for Store (StoreAdminDataScope.DefaultStoreId => CurrentCustomer.StaffStoreId).
    protected override void EditWarningCheck(Brand brand)
    {
        if (!brand.LimitedToStores ||
            (brand.LimitedToStores &&
             brand.Stores.Contains(Scope.DefaultStoreId) &&
             brand.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Catalog.Brands.Permissions"));
    }
}
