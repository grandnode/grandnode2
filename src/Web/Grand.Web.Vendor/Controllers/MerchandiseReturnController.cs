using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

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
