using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

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
    protected override void EditWarningCheck(Collection collection)
    {
        if (!collection.LimitedToStores ||
            (collection.Stores.Contains(Scope.DefaultStoreId) &&
             collection.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Catalog.Collections.Permissions"));
    }
}
