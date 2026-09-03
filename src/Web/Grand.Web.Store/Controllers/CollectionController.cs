using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
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

// Reduced to a thin subclass of BaseCollectionController (ARCH-001 Collection consolidation). All
// regions of behavior live in the shared base; this class only supplies Store's DI wiring, the
// EditWarningCheck hook, and the attributes that used to arrive transitively via
// BaseStoreController. Same pattern as CategoryController (see that file).
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class CollectionController(
    ICollectionViewModelService collectionViewModelService,
    ICollectionService collectionService,
    IStoreService storeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IPictureViewModelService pictureViewModelService,
    IProductService productService,
    IAdminDataScope<Collection> scope)
    : BaseCollectionController(collectionViewModelService, collectionService, storeService,
        languageService, translationService, pictureViewModelService, productService, scope)
{
    // Re-derived from the original Store CollectionController.Edit(GET) (pre-cutover) - the
    // condition is unusual (warns when NOT limited to stores at all, or when limited AND the
    // staff member's store is one of several) and easy to get backwards. Scope.DefaultStoreId is
    // exactly StaffStoreId for Store (StoreAdminDataScope.DefaultStoreId =>
    // CurrentCustomer.StaffStoreId).
    protected override void EditWarningCheck(Collection collection)
    {
        if (!collection.LimitedToStores ||
            (collection.Stores.Contains(Scope.DefaultStoreId) &&
             collection.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Catalog.Collections.Permissions"));
    }
}
