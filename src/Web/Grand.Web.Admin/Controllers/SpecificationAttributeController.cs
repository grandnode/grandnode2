using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.SpecificationAttributes)]
public class SpecificationAttributeController(
    ISpecificationAttributeService specificationAttributeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IProductService productService,
    SeoSettings seoSettings,
    IAdminDataScope<SpecificationAttribute> scope)
    : BaseSpecificationAttributeController(specificationAttributeService, languageService,
        translationService, productService, seoSettings, scope);