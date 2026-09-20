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