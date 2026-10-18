using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     External authentication service
/// </summary>
public interface IExternalAuthenticationService
{
    #region Authentication

    /// <summary>
    ///     Use passed parameters to find, associate or register the customer; the caller signs in the customer of a
    ///     LoggedIn or Registered result
    /// </summary>
    /// <param name="parameters">External authentication parameters</param>
    /// <param name="customer">Customer of the request (a guest when nobody is logged in)</param>
    /// <param name="store">Store of the request</param>
    /// <param name="language">Working language of the request</param>
    /// <returns>Result of an authentication</returns>
    Task<ExternalAuthResult> Authenticate(ExternalAuthParam parameters, Customer customer, Store store,
        Language language);

    #endregion

    /// <summary>
    ///     Associate the external account with customer
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="parameters">External authentication parameters</param>
    Task AssociateCustomer(Customer customer, ExternalAuthParam parameters);

    /// <summary>
    ///     Get the particular user with specified parameters
    /// </summary>
    /// <param name="parameters">External authentication parameters</param>
    /// <returns>Customer</returns>
    Task<Customer> GetCustomer(ExternalAuthParam parameters);


    Task<IList<ExternalAuthentication>> GetExternalIdentifiers(Customer customer);

    /// <summary>
    ///     Delete the external authentication
    /// </summary>
    /// <param name="externalAuthentication">External authentication</param>
    Task DeleteExternalAuthentication(ExternalAuthentication externalAuthentication);

    #region External authentication methods

    /// <summary>
    ///     Load all active authentication providers
    /// </summary>
    /// <param name="customer">Customer whose groups the providers may be limited to</param>
    /// <param name="store">Store the providers may be limited to</param>
    /// <returns>External authentication providers</returns>
    IList<IExternalAuthenticationProvider> LoadActiveAuthenticationProviders(Customer customer, Store store);

    /// <summary>
    ///     Load specified external authentication provider by system name
    /// </summary>
    /// <param name="systemName">System name</param>
    /// <returns>Found external authentication method</returns>
    IExternalAuthenticationProvider LoadAuthenticationProviderBySystemName(string systemName);

    /// <summary>
    ///     Load all external authentication providers independently from status
    /// </summary>
    /// <returns>External authentication providers</returns>
    IList<IExternalAuthenticationProvider> LoadAllAuthenticationProviders();

    /// <summary>
    ///     Check whether authentication by the passed external authentication provider is available
    /// </summary>
    /// <param name="systemName">System name of the external authentication provider</param>
    /// <param name="customer">Customer whose groups the provider may be limited to</param>
    /// <param name="store">Store the provider may be limited to</param>
    /// <returns>True if is available; otherwise false</returns>
    bool AuthenticationProviderIsAvailable(string systemName, Customer customer, Store store);

    #endregion
}