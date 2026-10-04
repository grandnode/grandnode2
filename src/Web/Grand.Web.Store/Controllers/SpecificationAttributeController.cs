using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[Area(Constants.AreaStore)]
[AuthorizeStore]
[AutoValidateAntiforgeryToken]
[AuthorizeMenu]
public class SpecificationAttributeController(
    ISpecificationAttributeService specificationAttributeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IProductService productService,
    SeoSettings seoSettings,
    IAdminDataScope<SpecificationAttribute> scope)
    : BaseSpecificationAttributeController(specificationAttributeService, languageService,
        translationService, productService, seoSettings, scope);