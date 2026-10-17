using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Discounts;
using Grand.Mediator;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.Store.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class DiscountController(
    IDiscountViewModelService discountViewModelService,
    IDiscountService discountService,
    IRequestTranslationService translationService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IDiscountProviderLoader discountProviderLoader,
    IAdminDataScope<Discount> scope)
    : BaseDiscountController(discountViewModelService, discountService, translationService, dateTimeService,
        mediator, discountProviderLoader, scope);
