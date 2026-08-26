using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.Admin.Controllers;

public class MerchandiseReturnController(
    IMerchandiseReturnViewModelService merchandiseReturnViewModelService,
    ITranslationService translationService,
    IMerchandiseReturnService merchandiseReturnService,
    IOrderService orderService,
    IAdminDataScope<MerchandiseReturn> scope)
    : BaseMerchandiseReturnController(merchandiseReturnViewModelService, translationService,
        merchandiseReturnService, orderService, scope);
