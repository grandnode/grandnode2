using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Common.Security.Authentication;

public class ExternalSignInService(
    IExternalAuthenticationService externalAuthenticationService,
    IGrandAuthenticationService authenticationService) : IExternalSignInService
{
    public virtual async Task<IActionResult> Authenticate(ExternalAuthParam parameters, Customer customer,
        Store store, Language language, string returnUrl = null)
    {
        //the logged-in or registered event is already published; sign in after it, as before
        var result = await externalAuthenticationService.Authenticate(parameters, customer, store, language);
        switch (result.Status)
        {
            case ExternalAuthStatus.LoggedIn:
                await authenticationService.SignIn(result.Customer, false);
                return ReturnUrl(returnUrl);
            case ExternalAuthStatus.Registered:
                await authenticationService.SignIn(result.Customer, false);
                return RegisterResult(UserRegistrationType.Standard);
            case ExternalAuthStatus.Associated:
                return ReturnUrl(returnUrl);
            case ExternalAuthStatus.AwaitingApproval:
                return RegisterResult(UserRegistrationType.AdminApproval);
            default:
                return Error(result.Errors);
        }
    }

    protected virtual IActionResult ReturnUrl(string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
            return new RedirectToRouteResult("HomePage", new { area = "" });

        return new RedirectResult(returnUrl);
    }

    protected virtual IActionResult RegisterResult(UserRegistrationType registrationType)
    {
        return new RedirectToRouteResult("RegisterResult", new { resultId = (int)registrationType });
    }

    protected virtual IActionResult Error(IEnumerable<string> errors)
    {
        return new RedirectToActionResult("ExternalAuthenticationError", "Common", new { Errors = errors });
    }
}
