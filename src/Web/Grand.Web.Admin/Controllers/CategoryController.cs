using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseCategoryController (ARCH-001 Category consolidation). All
// regions of behavior live in the shared base; this class only supplies Admin's DI wiring plus the
// attributes that used to arrive transitively via BaseAdminController - BaseCategoryController
// can't inherit any single host's base controller (it's shared across Admin/Store, each with a
// different [Area]/[Authorize*] pair), so each subclass restates its own host's attribute set
// explicitly. Same pattern as ProductController (see that file).
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
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
        translationService, pictureViewModelService, productService, scope);
