using Grand.Business.Authentication.Utilities;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Authentication.Interfaces
{
    /// <summary>
    /// External authentication service
    /// </summary>
    public partial interface IExternalAuthenticationService
    {
        #region External authentication methods

        /// <summary>
        /// Load all active authentication providers
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="store">Store</param>
        /// <returns>External authentication providers</returns>
        IList<IExternalAuthenticationProvider> LoadActiveAuthenticationProviders(Customer customer = null, Store store = null);

        /// <summary>
        /// Load specified external authentication provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found external authentication method</returns>
        IExternalAuthenticationProvider LoadAuthenticationProviderBySystemName(string systemName);

        /// <summary>
        /// Load all external authentication providers independently from status
        /// </summary>
        /// <returns>External authentication providers</returns>
        IList<IExternalAuthenticationProvider> LoadAllAuthenticationProviders();

        /// <summary>
        /// Check whether authentication by the passed external authentication provider is available 
        /// </summary>
        /// <param name="systemName">System name of the external authentication provider</param>
        /// <returns>True if is available; otherwise false</returns>
        bool AuthenticationProviderIsAvailable(string systemName);


        #endregion

        #region Authentication

        /// <summary>
        /// Use passed parameters to authenticate customer
        /// </summary>
        /// <param name="parameters">External authentication parameters</param>
        /// <param name="returnUrl">Defines the URL of user return, when successfully authenticated</param>
        /// <returns>Result of an authentication</returns>
        Task<IActionResult> Authenticate(ExternalAuthParam parameters, string returnUrl = null);


        #endregion

        /// <summary>
        /// Associate the external account with customer
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="parameters">External authentication parameters</param>
        Task AssociateCustomer(Customer customer, ExternalAuthParam parameters);

        /// <summary>
        /// Get the particular user with specified parameters
        /// </summary>
        /// <param name="parameters">External authentication parameters</param>
        /// <returns>Customer</returns>
        Task<Customer> GetCustomer(ExternalAuthParam parameters);

        
        Task<IList<ExternalAuthentication>> GetExternalIdentifiers(Customer customer);
       
        /// <summary>
        /// Delete the external authentication
        /// </summary>
        /// <param name="externalAuthenticationRecord">External authentication</param>
        Task DeleteExternalAuthentication(ExternalAuthentication externalAuthentication);
    }
}