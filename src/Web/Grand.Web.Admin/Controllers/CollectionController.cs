using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseCollectionController (ARCH-001 Collection consolidation). All
// regions of behavior live in the shared base; this class only supplies Admin's DI wiring plus the
// attributes that used to arrive transitively via BaseAdminController - BaseCollectionController
// can't inherit any single host's base controller (it's shared across Admin/Store, each with a
// different [Area]/[Authorize*] pair), so each subclass restates its own host's attribute set
// explicitly. Same pattern as CategoryController (see that file).
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
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
        languageService, translationService, pictureViewModelService, productService, scope);
