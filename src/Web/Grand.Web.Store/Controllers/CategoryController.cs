using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseCategoryController (ARCH-001 Category consolidation). All
// regions of behavior live in the shared base; this class only supplies Store's DI wiring, the
// EditWarningCheck hook, and the attributes that used to arrive transitively via
// BaseStoreController. Same pattern as ProductController (see that file).
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class CategoryController(
    ICategoryService categoryService,
    ICategoryViewModelService categoryViewModelService,
    ILanguageService languageService,
    ITranslationService translationService,
    IPictureViewModelService pictureViewModelService,
    IProductService productService,
    IAdminDataScope<Category> scope)
    : BaseCategoryController(categoryService, categoryViewModelService, languageService,
        translationService, pictureViewModelService, productService, scope)
{
    // Re-derived from the original Store CategoryController.Edit(GET) (pre-cutover:
    // src/Web/Grand.Web.Store/Controllers/CategoryController.cs, lines ~122-132) - the condition is
    // unusual (warns when NOT limited to stores at all, or when limited AND the staff member's
    // store is one of several) and easy to get backwards. Scope.DefaultStoreId is exactly
    // StaffStoreId for Store (StoreAdminDataScope.DefaultStoreId => CurrentCustomer.StaffStoreId).
    protected override void EditWarningCheck(Category category)
    {
        if (!category.LimitedToStores ||
            (category.Stores.Contains(Scope.DefaultStoreId) &&
             category.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Catalog.Categories.Permissions"));
    }
}
