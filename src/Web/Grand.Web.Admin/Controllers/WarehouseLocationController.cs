using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Shipping;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class WarehouseLocationController(
    IWarehouseLocationService locationService,
    IWarehouseService warehouseService,
    IRequestTranslationService translationService,
    IAdminDataScope<Warehouse> warehouseScope)
    : BaseWarehouseLocationController(locationService, warehouseService, translationService, warehouseScope);
