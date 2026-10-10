using Authentication.OpenIdConnect.Models;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Security.ExternalRegistration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.OpenIdConnect.Controllers;

public class OidcLoginController : BasePluginController
{
    #region Ctor

    public OidcLoginController(
        IExternalRegistrationContext externalRegistrationContext,
        IOidcCustomerResolver oidcCustomerResolver,
        IGrandAuthenticationService authenticationService,
        IMediator mediator,
        IExternalAuthenticationService externalAuthenticationService,
        IAuthenticationSchemeProvider authenticationSchemeProvider,
        IGroupService groupService,
        IContextAccessor contextAccessor)
    {
        _externalRegistrationContext = externalRegistrationContext;
        _oidcCustomerResolver = oidcCustomerResolver;
        _authenticationService = authenticationService;
        _mediator = mediator;
        _externalAuthenticationService = externalAuthenticationService;
        _authenticationSchemeProvider = authenticationSchemeProvider;
        _groupService = groupService;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Fields

    private readonly IExternalRegistrationContext _externalRegistrationContext;
    private readonly IOidcCustomerResolver _oidcCustomerResolver;
    private readonly IGrandAuthenticationService _authenticationService;
    private readonly IMediator _mediator;
    private readonly IExternalAuthenticationService _externalAuthenticationService;
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;
    private readonly IGroupService _groupService;
    private readonly IContextAccessor _contextAccessor;

    #endregion

    #region Methods

    public async Task<IActionResult> Login(string returnUrl)
    {
        if (!_externalAuthenticationService.AuthenticationProviderIsAvailable(OidcLoginDefaults.ProviderSystemName) ||
            await _authenticationSchemeProvider.GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme) == null)
            return RedirectToRoute("OidcLoginFailed");

        var properties = new AuthenticationProperties {
            RedirectUri = Url.RouteUrl("OidcLoginCallback", new { returnUrl = LocalOrNull(returnUrl) })
        };
        return Challenge(properties, OidcLoginDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> Callback(string returnUrl)
    {
        var pending = await _externalRegistrationContext.GetPending();
        if (pending == null)
            return RedirectToRoute("OidcLoginFailed");

        returnUrl = LocalOrNull(returnUrl);
        var result = await _oidcCustomerResolver.ResolveForLogin(pending.Subject, pending.Email, pending.EmailVerified,
            _contextAccessor.StoreContext.CurrentStore.Id);
        switch (result.Outcome)
        {
            case OidcLoginOutcome.SignIn:
                await _mediator.Publish(new CustomerLoggedInEvent(result.Customer));
                await _authenticationService.SignIn(result.Customer, false);
                await _externalRegistrationContext.Clear();
                return RedirectLocal(returnUrl);
            case OidcLoginOutcome.RegistrationRequired:
                return RedirectToRoute("Register", new { returnUrl });
            case OidcLoginOutcome.LinkRequired:
                return RedirectToRoute("OidcLoginLink", new { returnUrl });
            default:
                await _externalRegistrationContext.Clear();
                return RedirectToRoute("OidcLoginFailed", new { reason = (int)result.Refusal });
        }
    }

    public async Task<IActionResult> Link(string returnUrl)
    {
        var pending = await _externalRegistrationContext.GetPending();
        if (pending == null)
            return RedirectToRoute("OidcLoginFailed");

        returnUrl = LocalOrNull(returnUrl);
        var completeUrl = Url.RouteUrl("OidcLoginLinkComplete", new { returnUrl });
        return View(new LinkModel {
            Email = pending.Email,
            CompleteUrl = completeUrl,
            LoginUrl = Url.RouteUrl("Login", new { returnUrl = completeUrl }),
            SignedIn = await _groupService.IsRegistered(_contextAccessor.WorkContext.CurrentCustomer)
        });
    }

    public async Task<IActionResult> LinkComplete(string returnUrl)
    {
        returnUrl = LocalOrNull(returnUrl);
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (!await _groupService.IsRegistered(customer))
            return RedirectToRoute("OidcLoginLink", new { returnUrl });

        var pending = await _externalRegistrationContext.GetPending();
        if (pending == null)
            return RedirectToRoute("OidcLoginFailed");

        //only the account the provider identity names may be connected to it
        if (!string.Equals(customer.Email?.Trim(), pending.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            await _externalRegistrationContext.Clear();
            return RedirectToRoute("OidcLoginFailed", new { mismatch = true });
        }

        return await _externalRegistrationContext.Complete(customer) == OidcLinkResult.Linked
            ? RedirectLocal(returnUrl)
            : RedirectToRoute("OidcLoginFailed", new { reason = (int)OidcLoginRefusal.LinkedToOtherIdentity });
    }

    public IActionResult Failed(int? reason, bool mismatch = false) =>
        View(new FailedModel { Reason = (OidcLoginRefusal)(reason ?? 0), Mismatch = mismatch });

    #endregion

    #region Utilities

    private string LocalOrNull(string returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    private IActionResult RedirectLocal(string returnUrl) =>
        returnUrl != null ? Redirect(returnUrl) : RedirectToRoute("HomePage");

    #endregion
}
