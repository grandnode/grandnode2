using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AuthorizeStore]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeMenu]
public class CheckoutAttributeController(
    ICheckoutAttributeService checkoutAttributeService,
    ILanguageService languageService,
    ITranslationService translationService,
    ICurrencyService currencyService,
    CurrencySettings currencySettings,
    IMeasureService measureService,
    MeasureSettings measureSettings,
    ICheckoutAttributeViewModelService checkoutAttributeViewModelService,
    IAdminDataScope<CheckoutAttribute> scope)
    : BaseCheckoutAttributeController(checkoutAttributeService, languageService, translationService,
        currencyService, currencySettings, measureService, measureSettings,
        checkoutAttributeViewModelService, scope);
