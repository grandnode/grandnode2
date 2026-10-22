using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class ProductController(
    IProductViewModelService productViewModelService,
    IProductAddPopupViewModelService productAddPopupViewModelService,
    IProductService productService,
    ILanguageService languageService,
    IRequestTranslationService translationService,
    IDateTimeService dateTimeService,
    IRequestPermissionService permissionService,
    IAdminDataScope<Product> scope)
    : BaseProductController(productViewModelService, productAddPopupViewModelService, productService, languageService, translationService,
        dateTimeService, permissionService, scope);
