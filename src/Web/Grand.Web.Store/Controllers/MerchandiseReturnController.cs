using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

public class MerchandiseReturnController(
    IMerchandiseReturnViewModelService merchandiseReturnViewModelService,
    ITranslationService translationService,
    IMerchandiseReturnService merchandiseReturnService,
    IOrderService orderService,
    IAdminDataScope<MerchandiseReturn> scope)
    : BaseMerchandiseReturnController(merchandiseReturnViewModelService, translationService,
        merchandiseReturnService, orderService, scope)
{
    // Preserved host divergence (spec §5/§11, DECIDED): Store's original soft-denies with an empty
    // body instead of throwing - see BaseMerchandiseReturnController.NotFoundOrDeniedForNotesSelect's
    // doc comment.
    protected override IActionResult NotFoundOrDeniedForNotesSelect() => Content("");
}
