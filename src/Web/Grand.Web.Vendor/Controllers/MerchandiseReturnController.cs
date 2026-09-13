using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

// Concrete host subclass of BaseMerchandiseReturnController (ARCH-001 MerchandiseReturn
// consolidation). This class supplies Vendor's DI wiring plus the attributes that used to arrive
// transitively via BaseVendorController - BaseMerchandiseReturnController can't inherit any single
// host's base controller (it's shared across Admin/Store/Vendor, each with a different
// [Area]/[Authorize*] pair), so each subclass restates its own host's attribute set explicitly, same
// pattern as OrderController.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
public class MerchandiseReturnController(
    IMerchandiseReturnViewModelService merchandiseReturnViewModelService,
    ITranslationService translationService,
    IMerchandiseReturnService merchandiseReturnService,
    IOrderService orderService,
    IAdminDataScope<MerchandiseReturn> scope)
    : BaseMerchandiseReturnController(merchandiseReturnViewModelService, translationService,
        merchandiseReturnService, orderService, scope);
