using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseGiftVoucherController (ARCH-001 GiftVoucher
// consolidation). All regions of behavior live in the shared base; this class only supplies
// Admin's DI wiring plus the attributes that used to arrive transitively via
// BaseAdminController - BaseGiftVoucherController can't inherit any single host's base
// controller (it's shared across Admin/Store, each with a different [Area]/[Authorize*] pair),
// so each subclass restates its own host's attribute set explicitly. Same pattern as
// CategoryController (see that file).
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class GiftVoucherController(
    IGiftVoucherViewModelService giftVoucherViewModelService,
    IGiftVoucherService giftVoucherService,
    ITranslationService translationService,
    IAdminDataScope<GiftVoucher> scope)
    : BaseGiftVoucherController(giftVoucherViewModelService, giftVoucherService, translationService, scope);
