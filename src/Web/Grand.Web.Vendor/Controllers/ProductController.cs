using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
public class ProductController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IInventoryManageService inventoryManageService,
    ILanguageService languageService,
    IRequestTranslationService translationService,
    IProductReservationService productReservationService,
    IAuctionService auctionService,
    IDateTimeService dateTimeService,
    IRequestPermissionService permissionService,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<Product> scope)
    : BaseProductController(productViewModelService, productService, inventoryManageService, languageService,
        translationService, productReservationService, auctionService, dateTimeService, permissionService,
        enumTranslationService, scope);
