using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseCheckoutAttributeController (ARCH-001 CheckoutAttribute consolidation). All
// regions of behavior live in the shared base; this class only supplies Admin's DI wiring plus the
// attributes that used to arrive transitively via BaseAdminController - BaseCheckoutAttributeController
// can't inherit any single host's base controller (it's shared across Admin/Store, each with a
// different [Area]/[Authorize*] pair), so each subclass restates its own host's attribute set
// explicitly.
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
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