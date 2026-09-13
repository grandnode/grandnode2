using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseProductAttributeController (ARCH-001 ProductAttribute consolidation). All
// regions of behavior live in the shared base; this class only supplies Admin's DI wiring plus the
// attributes that used to arrive transitively via BaseAdminController - BaseProductAttributeController
// can't inherit any single host's base controller (it's shared across Admin/Store, each with a
// different [Area]/[Authorize*] pair), so each subclass restates its own host's attribute set
// explicitly.
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class ProductAttributeController(
    IProductService productService,
    IProductAttributeService productAttributeService,
    ILanguageService languageService,
    ITranslationService translationService,
    SeoSettings seoSettings,
    IAdminDataScope<ProductAttribute> scope)
    : BaseProductAttributeController(productService, productAttributeService, languageService,
        translationService, seoSettings, scope);