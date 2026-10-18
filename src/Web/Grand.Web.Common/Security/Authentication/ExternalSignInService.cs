using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Common.Security.Authentication;

public class ExternalSignInService(
    IExternalAuthenticationService externalAuthenticationService,
    IGrandAuthenticationService authenticationService,
    ITranslationService translationService) : IExternalSignInService
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
            case ExternalAuthStatus.AlreadyAssigned:
                return Error([
                    translationService.GetResource("Account.AssociatedExternalAuth.AlreadyAssigned", language.Id)
                ]);
            case ExternalAuthStatus.RegistrationDisabled:
                return Error([
                    translationService.GetResource("Account.AssociatedExternalAuth.RegistrationDisabled", language.Id)
                ]);
            case ExternalAuthStatus.Error:
                return Error(result.Errors);
            default:
                throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status,
                    "Unhandled external authentication status");
        }
    }

    protected virtual IActionResult ReturnUrl(string returnUrl)
    {
        //the return URL comes from the query string of the login request; never leave the store with it
        if (string.IsNullOrEmpty(returnUrl) || !RedirectHttpResult.IsLocalUrl(returnUrl))
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
