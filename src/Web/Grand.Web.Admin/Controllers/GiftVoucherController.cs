using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Domain.Orders;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class GiftVoucherController(
    IGiftVoucherViewModelService giftVoucherViewModelService,
    IGiftVoucherService giftVoucherService,
    IRequestTranslationService translationService,
    IAdminDataScope<GiftVoucher> scope)
    : BaseGiftVoucherController(giftVoucherViewModelService, giftVoucherService, translationService, scope);
