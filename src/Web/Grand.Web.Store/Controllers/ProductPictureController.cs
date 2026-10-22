using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class ProductPictureController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    ILanguageService languageService,
    IRequestTranslationService translationService,
    IRequestPermissionService permissionService,
    IAdminDataScope<Product> scope)
    : BaseProductPictureController(productViewModelService, productService, languageService, translationService, permissionService, scope);
