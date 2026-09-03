using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseGiftVoucherController (ARCH-001 GiftVoucher
// consolidation). All regions of behavior live in the shared base; this class only supplies
// Store's DI wiring, the EditWarningCheck hook, and the attributes that used to arrive
// transitively via BaseStoreController. Same pattern as CategoryController's EditWarningCheck
// override (see that file).
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class GiftVoucherController(
    IGiftVoucherViewModelService giftVoucherViewModelService,
    IGiftVoucherService giftVoucherService,
    ITranslationService translationService,
    IAdminDataScope<GiftVoucher> scope)
    : BaseGiftVoucherController(giftVoucherViewModelService, giftVoucherService, translationService, scope)
{
    // Re-derived from the current design spec's finding: GetGiftVoucherQueryHandler treats an
    // empty/null StoreId as visible from every store, so a global voucher must warn, not block,
    // on Edit - matches Category/Collection/Page/News's proven EditWarningCheck idiom, adapted
    // for GiftVoucher's flat StoreId (no LimitedToStores/Stores list to inspect).
    protected override void EditWarningCheck(GiftVoucher giftVoucher)
    {
        if (giftVoucher.StoreId != Scope.DefaultStoreId)
            Warning(TranslationService.GetResource("Admin.GiftVouchers.Permissions"));
    }
}
