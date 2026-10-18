using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Common.Security.Authentication;

/// <summary>
///     Completes a sign-in by an external authentication method: signs the customer in and picks where to go next
/// </summary>
public interface IExternalSignInService
{
    /// <summary>
    ///     Use passed parameters to authenticate customer
    /// </summary>
    /// <param name="parameters">External authentication parameters</param>
    /// <param name="customer">Customer of the request (a guest when nobody is logged in)</param>
    /// <param name="store">Store of the request</param>
    /// <param name="language">Working language of the request</param>
    /// <param name="returnUrl">Defines the URL of user return, when successfully authenticated</param>
    /// <returns>Result of an authentication</returns>
    Task<IActionResult> Authenticate(ExternalAuthParam parameters, Customer customer, Store store, Language language,
        string returnUrl = null);
}
