using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AuthorizeStore]
[Area(Constants.AreaStore)]
[AuthorizeMenu]
public class WarehouseLocationController(
    IWarehouseLocationService locationService,
    IWarehouseService warehouseService,
    IRequestTranslationService translationService,
    IAdminDataScope<Warehouse> warehouseScope)
    : BaseWarehouseLocationController(locationService, warehouseService, translationService, warehouseScope);
