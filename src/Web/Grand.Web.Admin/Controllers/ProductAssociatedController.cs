using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class ProductAssociatedController(
    IProductAddPopupViewModelService productAddPopupViewModelService,
    IProductAssociatedViewModelService productAssociatedViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseProductAssociatedController(productAddPopupViewModelService, productAssociatedViewModelService, productService, translationService, scope);
