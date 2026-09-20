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
