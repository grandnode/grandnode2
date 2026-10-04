using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AuthorizeStore]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
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