using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Discounts;
using Grand.Mediator;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Concrete host subclass of BaseDiscountController (ARCH-001 Discount consolidation). This class
// supplies Store's DI wiring plus the attributes that used to arrive transitively via
// BaseStoreController - BaseDiscountController can't inherit any single host's base controller
// (it's shared across Admin/Store, each with a different [Area]/[Authorize*] pair), so each
// subclass restates its own host's attribute set explicitly, same pattern as OrderController and
// MerchandiseReturnController. Store never had an "Applied to vendors" region (see Admin's
// DiscountController), so this subclass is a true thin subclass with no extra actions.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class DiscountController(
    IDiscountViewModelService discountViewModelService,
    IDiscountService discountService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IDiscountProviderLoader discountProviderLoader,
    IAdminDataScope<Discount> scope)
    : BaseDiscountController(discountViewModelService, discountService, translationService, dateTimeService,
        mediator, discountProviderLoader, scope);
