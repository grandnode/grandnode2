using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

// ARCH-001: adds SetLanguage on top of BaseHomeController - Admin and Store only (see
// BaseHomeController's own remarks for why Vendor stays on the plain base instead of this one).
public abstract class BaseHomeControllerWithSetLanguage(
    ICountryService countryService,
    ITranslationService translationService,
    IGrandAuthenticationService authenticationService,
    IContextAccessor contextAccessor)
    : BaseHomeController(countryService, translationService, authenticationService)
{
    /// <summary>The area name this host's own Home/Index route resolves under.</summary>
    protected abstract string AreaName { get; }

    public async Task<IActionResult> SetLanguage(string langid,
        [FromServices] ILanguageService languageService,
        [FromServices] ICustomerService customerService,
        string returnUrl = "")
    {
        var language = await languageService.GetLanguageById(langid);
        if (language != null)
            await customerService.UpdateUserField(contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.LanguageId,
                language.Id, contextAccessor.StoreContext.CurrentStore.Id);

        //home page
        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = Url.Action("Index", "Home", new { area = AreaName });
        //prevent open redirection attack
        if (!Url.IsLocalUrl(returnUrl))
            return RedirectToAction("Index", "Home", new { area = AreaName });
        return Redirect(returnUrl);
    }
}
