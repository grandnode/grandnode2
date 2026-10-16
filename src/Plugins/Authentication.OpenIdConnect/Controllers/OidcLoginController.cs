using Authentication.OpenIdConnect.Infrastructure;
using Authentication.OpenIdConnect.Models;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Security.ExternalRegistration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Authentication.OpenIdConnect.Controllers;

public class OidcLoginController : BasePluginController
{
    #region Ctor

    public OidcLoginController(
        IExternalRegistrationContext externalRegistrationContext,
        IOidcCustomerResolver oidcCustomerResolver,
        OidcProfileSynchronizer profileSynchronizer,
        OidcLoginConfig loginConfig,
        IGrandAuthenticationService authenticationService,
        IOidcSessionService oidcSessionService,
        IMediator mediator,
        IExternalAuthenticationService externalAuthenticationService,
        IAuthenticationSchemeProvider authenticationSchemeProvider,
        IGroupService groupService,
        IContextAccessor contextAccessor,
        ILogger<OidcLoginController> logger)
    {
        _externalRegistrationContext = externalRegistrationContext;
        _oidcCustomerResolver = oidcCustomerResolver;
        _profileSynchronizer = profileSynchronizer;
        _loginConfig = loginConfig;
        _authenticationService = authenticationService;
        _oidcSessionService = oidcSessionService;
        _mediator = mediator;
        _externalAuthenticationService = externalAuthenticationService;
        _authenticationSchemeProvider = authenticationSchemeProvider;
        _groupService = groupService;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    #endregion

    #region Fields

    private const string LinkSubjectKey = "Grand.OidcLogin.LinkSubject";

    private readonly IExternalRegistrationContext _externalRegistrationContext;
    private readonly IOidcCustomerResolver _oidcCustomerResolver;
    private readonly OidcProfileSynchronizer _profileSynchronizer;
    private readonly OidcLoginConfig _loginConfig;
    private readonly IGrandAuthenticationService _authenticationService;
    private readonly IOidcSessionService _oidcSessionService;
    private readonly IMediator _mediator;
    private readonly IExternalAuthenticationService _externalAuthenticationService;
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;
    private readonly IGroupService _groupService;
    private readonly IContextAccessor _contextAccessor;
    private readonly ILogger<OidcLoginController> _logger;

    #endregion

    #region Methods

    public async Task<IActionResult> Login(string returnUrl)
    {
        if (!IsAvailable())
            return RedirectToRoute("OidcLoginFailed");

        //the scheme is registered at startup, only when both the provider and the client are configured
        if (await _authenticationSchemeProvider.GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme) == null)
        {
            _logger.LogWarning("OpenID Connect login has no sign-in scheme: OidcResourceServer:Authority or " +
                               "OidcLogin:ClientId is empty, or the application was not restarted after the plugin was installed");
            return RedirectToRoute("OidcLoginFailed");
        }

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

        //the method may have been switched off, or limited to other stores or groups, since the sign-in started
        if (!IsAvailable() || pending.ProviderSystemName != OidcLoginDefaults.ProviderSystemName)
        {
            await _externalRegistrationContext.Clear();
            return RedirectToRoute("OidcLoginFailed");
        }

        returnUrl = LocalOrNull(returnUrl);
        var result = await _oidcCustomerResolver.ResolveForLogin(pending.Subject, pending.Email, pending.EmailVerified,
            _contextAccessor.StoreContext.CurrentStore.Id);

        //a store session is never replaced by another account, and a signed-in visitor does not register; the same
        //account signing in again (for example right after "Connect your account") gets a session-backed cookie
        if (result.Outcome is OidcLoginOutcome.SignIn or OidcLoginOutcome.RegistrationRequired)
        {
            var current = _contextAccessor.WorkContext.CurrentCustomer;
            if (await _groupService.IsRegistered(current) &&
                (result.Outcome == OidcLoginOutcome.RegistrationRequired || result.Customer?.Id != current.Id))
            {
                await _externalRegistrationContext.Clear();
                return RedirectToRoute("OidcLoginFailed", new { signedIn = true });
            }
        }

        switch (result.Outcome)
        {
            case OidcLoginOutcome.SignIn:
            {
                //the profile never blocks the sign-in; the pending identity is still needed here
                try
                {
                    await _profileSynchronizer.Sync(result.Customer, pending, _contextAccessor.StoreContext.CurrentStore.Id);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "OpenID Connect profile sync failed for customer {CustomerId}", result.Customer.Id);
                }

                //read before the pending identity is cleared: the same cookie carries them
                var (refreshToken, sid) = await OidcLoginSessionData.Read(HttpContext);
                await _externalRegistrationContext.Clear();
                var sessionId = await _oidcSessionService.Start(result.Customer, pending.Subject, sid, refreshToken,
                    _contextAccessor.StoreContext.CurrentStore.Id);
                if (sessionId == null)
                    return RedirectToRoute("OidcLoginFailed", new { session = true });

                //the identity provider is the sign-in: its multi-factor policy applies, not the store's two-factor code
                await _mediator.Publish(new CustomerLoggedInEvent(result.Customer,
                    _contextAccessor.WorkContext.CurrentCustomer, _contextAccessor.StoreContext.CurrentStore.Id));
                await _authenticationService.SignIn(result.Customer, false, sessionId);
                return RedirectLocal(returnUrl);
            }
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
        if (await RefuseLinkInProviderOnly() is { } refused)
            return refused;

        var pending = await _externalRegistrationContext.GetPending();
        if (pending == null)
            return RedirectToRoute("OidcLoginFailed");

        if (!IsAvailable())
        {
            await _externalRegistrationContext.Clear();
            return RedirectToRoute("OidcLoginFailed");
        }

        //an existing session never connects an account: the password is always asked for again.
        //Only the sign-in cookie is ended; IGrandAuthenticationService.SignOut would also drop the
        //external cookie that holds the pending identity
        if (await _groupService.IsRegistered(_contextAccessor.WorkContext.CurrentCustomer))
            await HttpContext.SignOutAsync(GrandCookieAuthenticationDefaults.AuthenticationScheme);

        //when the link page was shown, in whole seconds like the sign-in ticket's issue time once stored in the cookie
        HttpContext.Session.SetString(LinkSubjectKey,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) + ":" + pending.Subject);

        returnUrl = LocalOrNull(returnUrl);
        var completeUrl = Url.RouteUrl("OidcLoginLinkComplete", new { returnUrl });
        return View(new LinkModel {
            Email = pending.Email,
            LoginUrl = Url.RouteUrl("Login", new { returnUrl = completeUrl })
        });
    }

    public async Task<IActionResult> LinkComplete(string returnUrl)
    {
        if (await RefuseLinkInProviderOnly() is { } refused)
            return refused;

        returnUrl = LocalOrNull(returnUrl);
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (!await _groupService.IsRegistered(customer))
            return RedirectToRoute("OidcLoginLink", new { returnUrl });

        var pending = await _externalRegistrationContext.GetPending();
        if (pending == null)
            return RedirectToRoute("OidcLoginFailed");

        //the marker proves the link page was shown for this identity, and the sign-in ticket that the
        //password sign-in issued after it
        var marker = HttpContext.Session.GetString(LinkSubjectKey);
        HttpContext.Session.Remove(LinkSubjectKey);
        if (!TryReadMarker(marker, out var shown, out var subject) || subject != pending.Subject ||
            !await SignedInAfter(shown))
            return RedirectToRoute("OidcLoginLink", new { returnUrl });

        if (!IsAvailable() || pending.ProviderSystemName != OidcLoginDefaults.ProviderSystemName)
        {
            await _externalRegistrationContext.Clear();
            return RedirectToRoute("OidcLoginFailed");
        }

        //only the account the provider identity names may be connected to it
        if (!string.Equals(customer.Email?.Trim(), pending.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            await _externalRegistrationContext.Clear();
            return RedirectToRoute("OidcLoginFailed", new { mismatch = true });
        }

        return await _externalRegistrationContext.Complete(customer, _contextAccessor.StoreContext.CurrentStore.Id) switch {
            //the password sign-in that connected the account is replaced at once: the provider session is still open,
            //so the provider answers without a prompt and the callback starts a session-backed cookie
            OidcLinkResult.Linked => RedirectToRoute("OidcLoginLogin", new { returnUrl }),
            //the provider identity, not this account, is the one already connected elsewhere
            OidcLinkResult.SubjectLinkedToOtherCustomer => RedirectToRoute("OidcLoginFailed", new { taken = true }),
            OidcLinkResult.ProviderChanged => RedirectToRoute("OidcLoginFailed", new { reason = (int)OidcLoginRefusal.ProviderChanged }),
            _ => RedirectToRoute("OidcLoginFailed", new { reason = (int)OidcLoginRefusal.LinkedToOtherIdentity })
        };
    }

    public IActionResult Failed(int? reason, bool mismatch = false, bool taken = false, bool signedIn = false,
        bool session = false) =>
        View(new FailedModel {
            Reason = (OidcLoginRefusal)(reason ?? 0), Mismatch = mismatch, IdentityTaken = taken,
            SignedInAsOtherAccount = signedIn, SessionUnavailable = session
        });

    #endregion

    #region Utilities

    //ProviderOnly has no password path: a stale link URL must not open one
    private async Task<IActionResult> RefuseLinkInProviderOnly()
    {
        if (!_loginConfig.ProviderOnly)
            return null;
        await _externalRegistrationContext.Clear();
        return RedirectToRoute("OidcLoginFailed");
    }

    private bool IsAvailable() =>
        _externalAuthenticationService.AuthenticationProviderIsAvailable(OidcLoginDefaults.ProviderSystemName);

    private static bool TryReadMarker(string marker, out long shown, out string subject)
    {
        shown = 0;
        subject = null;
        var separator = marker?.IndexOf(':') ?? -1;
        if (separator <= 0 ||
            !long.TryParse(marker.AsSpan(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out shown))
            return false;
        subject = marker[(separator + 1)..];
        return true;
    }

    //the main sign-in ticket must be issued after the link page was shown; no issue time means not fresh
    private async Task<bool> SignedInAfter(long shown)
    {
        var result = await HttpContext.AuthenticateAsync(GrandCookieAuthenticationDefaults.AuthenticationScheme);
        var issued = result.Succeeded ? result.Properties?.IssuedUtc : null;
        //the same second counts: the link page ended any session it found, so a ticket from that second is a new sign-in
        return issued.HasValue && issued.Value.ToUnixTimeSeconds() >= shown;
    }

    private string LocalOrNull(string returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    private IActionResult RedirectLocal(string returnUrl) =>
        returnUrl != null ? Redirect(returnUrl) : RedirectToRoute("HomePage");

    #endregion
}
