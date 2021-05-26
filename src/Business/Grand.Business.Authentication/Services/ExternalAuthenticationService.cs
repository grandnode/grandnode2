using Grand.Business.Authentication.Events;
using Grand.Business.Authentication.Extensions;
using Grand.Business.Authentication.Interfaces;
using Grand.Business.Authentication.Utilities;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Events;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Customers.Utilities;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Authentication.Services
{
    /// <summary>
    /// Determines the external authentication service implementation
    /// </summary>
    public partial class ExternalAuthenticationService : IExternalAuthenticationService
    {
        #region Fields

        private readonly IGrandAuthenticationService _authenticationService;
        private readonly ICustomerManagerService _customerManagerService;
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly IMediator _mediator;
        private readonly IRepository<ExternalAuthentication> _externalAuthenticationRecordRepository;
        private readonly IWorkContext _workContext;
        private readonly IEnumerable<IExternalAuthenticationProvider> _externalAuthenticationProviders;
        private readonly LanguageSettings _languageSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;

        #endregion

        #region Ctor

        public ExternalAuthenticationService(
            IGrandAuthenticationService authenticationService,
            ICustomerManagerService customerManagerService,
            ICustomerService customerService,
            IGroupService groupService,
            IMediator mediator,
            IRepository<ExternalAuthentication> externalAuthenticationRecordRepository,
            IWorkContext workContext,
            IEnumerable<IExternalAuthenticationProvider> externalAuthenticationProviders,
            LanguageSettings languageSettings,
            CustomerSettings customerSettings,
            ExternalAuthenticationSettings externalAuthenticationSettings)
        {
            _customerSettings = customerSettings;
            _externalAuthenticationSettings = externalAuthenticationSettings;
            _authenticationService = authenticationService;
            _customerManagerService = customerManagerService;
            _customerService = customerService;
            _groupService = groupService;
            _mediator = mediator;
            _externalAuthenticationRecordRepository = externalAuthenticationRecordRepository;
            _workContext = workContext;
            _externalAuthenticationProviders = externalAuthenticationProviders;
            _languageSettings = languageSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Authenticate user with existing associated external account
        /// </summary>
        /// <param name="associatedUser">Associated with passed external authentication parameters user</param>
        /// <param name="currentLoggedInUser">Current logged-in user</param>
        /// <param name="returnUrl">URL to which the user will return after authentication</param>
        /// <returns>Result of an authentication</returns>
        protected virtual async Task<IActionResult> AuthenticateExistingUser(Customer associatedUser, Customer currentLoggedInUser, string returnUrl)
        {
            //log in guest user
            if (currentLoggedInUser == null)
                return await LoginUser(associatedUser, returnUrl);

            //account is already assigned to another user
            if (currentLoggedInUser.Id != associatedUser.Id)
                return Error(new[] { "Account is already assigned" });

            if (string.IsNullOrEmpty(returnUrl))
                return new RedirectToRouteResult("HomePage", new { area = "" });
            return new RedirectResult(returnUrl);
        }

        /// <summary>
        /// Authenticate current user and associate new external account with user
        /// </summary>
        /// <param name="currentLoggedInUser">Current logged-in user</param>
        /// <param name="parameters">Authentication parameters received from external authentication method</param>
        /// <param name="returnUrl">URL to which the user will return after authentication</param>
        /// <returns>Result of an authentication</returns>
        protected virtual async Task<IActionResult> AuthenticateNewUser(Customer currentLoggedInUser, ExternalAuthParam parameters, string returnUrl)
        {
            //associate external account with logged-in user
            if (currentLoggedInUser != null)
            {
                await AssociateCustomer(currentLoggedInUser, parameters);
                if (string.IsNullOrEmpty(returnUrl))
                    return new RedirectToRouteResult("HomePage", new { area = "" });
                return new RedirectResult(returnUrl);
            }

            //or try to register new user
            if (_customerSettings.UserRegistrationType != UserRegistrationType.Disabled)
                return await RegisterNewUser(parameters, returnUrl);

            //registration is disabled
            return Error(new[] { "Registration is disabled" });
        }

        /// <summary>
        /// Register new user
        /// </summary>
        /// <param name="parameters">Authentication parameters received from external authentication method</param>
        /// <param name="returnUrl">URL to which the user will return after authentication</param>
        /// <returns>Result of an authentication</returns>
        protected virtual async Task<IActionResult> RegisterNewUser(ExternalAuthParam parameters, string returnUrl)
        {
            var approved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard ||
                _customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation;

            //create registration request
            var registrationRequest = new RegistrationRequest(_workContext.CurrentCustomer,
                parameters.Email, parameters.Email,
                CommonHelper.GenerateRandomDigitCode(20),
                PasswordFormat.Hashed,
                _workContext.CurrentStore.Id,
                approved);

            //whether registration request has been completed successfully
            var registrationResult = await _customerManagerService.RegisterCustomer(registrationRequest);
            if (!registrationResult.Success)
                return Error(registrationResult.Errors);

            //allow to save other customer values by consuming this event
            await _mediator.Publish(new RegisteredByExternalMethodEventHandler(_workContext.CurrentCustomer, parameters, registrationResult));

            //raise vustomer registered event
            await _mediator.Publish(new CustomerRegisteredEvent(_workContext.CurrentCustomer));

            //associate external account with registered user
            await AssociateCustomer(_workContext.CurrentCustomer, parameters);

            //authenticate
            if (approved)
            {
                await _authenticationService.SignIn(_workContext.CurrentCustomer, false);

                return new RedirectToRouteResult("RegisterResult", new { resultId = (int)UserRegistrationType.Standard });
            }

            //registration is succeeded but isn't approved by admin
            if (_customerSettings.UserRegistrationType == UserRegistrationType.AdminApproval)
                return new RedirectToRouteResult("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval });

            return Error(new[] { "Error on registration" });
        }

        /// <summary>
        /// Login passed user
        /// </summary>
        /// <param name="user">User to login</param>
        /// <param name="returnUrl">URL to which the user will return after authentication</param>
        /// <returns>Result of an authentication</returns>
        protected virtual async Task<IActionResult> LoginUser(Customer user, string returnUrl)
        {
            //raise event       
            await _mediator.Publish(new CustomerLoggedInEvent(user));

            //authenticate
            await _authenticationService.SignIn(user, false);

            if (string.IsNullOrEmpty(returnUrl))
                return new RedirectToRouteResult("HomePage", new { area = "" });

            return new RedirectResult(returnUrl);
        }

        /// <summary>
        /// Add errors that occurred during authentication
        /// </summary>
        /// <param name="errors">Collection of errors</param>
        /// <returns>Result of an authentication</returns>
        protected virtual IActionResult Error(IEnumerable<string> errors)
        {
            return new RedirectToActionResult("ExternalAuthenticationError", "Common", new { Errors = errors });
        }

        #endregion

        #region Methods

        #region External authentication methods

        /// <summary>
        /// Load active external authentication providers
        /// </summary>
        /// <returns>External Authentication Providers</returns>
        public virtual IList<IExternalAuthenticationProvider> LoadActiveAuthenticationProviders(Customer customer = null, Store store = null)
        {
            return LoadAllAuthenticationProviders()
                .Where(provider =>
                    provider.IsMethodActive(_externalAuthenticationSettings) &&
                    provider.IsAuthenticateGroup(_workContext.CurrentCustomer) &&
                    provider.IsAuthenticateStore(_workContext.CurrentStore)
                ).ToList();
        }

        /// <summary>
        /// Load external authentication provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found external authentication provider</returns>
        public virtual IExternalAuthenticationProvider LoadAuthenticationProviderBySystemName(string systemName)
        {
            return _externalAuthenticationProviders.FirstOrDefault(x => x.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Load all external authentication providers
        /// </summary>
        /// <returns>External authentication methods</returns>
        public virtual IList<IExternalAuthenticationProvider> LoadAllAuthenticationProviders()
        {
            return _externalAuthenticationProviders.OrderBy(x => x.Priority).ToList();
        }

        /// <summary>
        /// Check whether authentication by the passed external authentication provider is available
        /// </summary>
        /// <param name="systemName">System name of the external authentication provider</param>
        /// <returns>True if is available; otherwise false</returns>
        public virtual bool AuthenticationProviderIsAvailable(string systemName)
        {
            var authenticationMethod = LoadAuthenticationProviderBySystemName(systemName);

            return authenticationMethod != null &&
                authenticationMethod.IsMethodActive(_externalAuthenticationSettings) &&
                authenticationMethod.IsAuthenticateGroup(_workContext.CurrentCustomer) &&
                authenticationMethod.IsAuthenticateStore(_workContext.CurrentStore);
        }

        #endregion

        #region Authentication

        /// <summary>
        /// Authenticate user by passed parameters
        /// </summary>
        /// <param name="parameters">External authentication parameters</param>
        /// <param name="returnUrl">URL to which the user will return after authentication</param>
        /// <returns>Result of an authentication</returns>
        public virtual async Task<IActionResult> Authenticate(ExternalAuthParam parameters, string returnUrl = null)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (!AuthenticationProviderIsAvailable(parameters.ProviderSystemName))
                return Error(new[] { "External authentication method cannot be loaded" });

            //get current logged-in user
            var currentLoggedInUser = await _groupService.IsRegistered(_workContext.CurrentCustomer) ? _workContext.CurrentCustomer : null;

            //authenticate associated user if already exists
            var associatedUser = await GetCustomer(parameters);
            if (associatedUser != null)
                return await AuthenticateExistingUser(associatedUser, currentLoggedInUser, returnUrl);

            //or associate and authenticate new user
            return await AuthenticateNewUser(currentLoggedInUser, parameters, returnUrl);
        }

        #endregion

        /// <summary>
        /// Accociate external account with customer
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="parameters">External authentication parameters</param>
        public virtual async Task AssociateCustomer(Customer customer, ExternalAuthParam parameters)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var externalAuthenticationRecord = new ExternalAuthentication
            {
                CustomerId = customer.Id,
                Email = parameters.Email,
                ExternalIdentifier = parameters.Identifier,
                ExternalDisplayIdentifier = parameters.Name,
                OAuthAccessToken = parameters.AccessToken,
                ProviderSystemName = parameters.ProviderSystemName,
            };

            await _externalAuthenticationRecordRepository.InsertAsync(externalAuthenticationRecord);
        }

        /// <summary>
        /// Get the particular user with specified parameters
        /// </summary>
        /// <param name="parameters">External authentication parameters</param>
        /// <returns>Customer</returns>
        public virtual async Task<Customer> GetCustomer(ExternalAuthParam parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var associationRecord = await _externalAuthenticationRecordRepository.FirstOrDefaultAsync(record =>
                record.ExternalIdentifier.Equals(parameters.Identifier) && record.ProviderSystemName.Equals(parameters.ProviderSystemName));

            if (associationRecord == null)
                return null;

            return await _customerService.GetCustomerById(associationRecord.CustomerId);
        }

        public virtual async Task<IList<ExternalAuthentication>> GetExternalIdentifiers(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));
            var query = from p in _externalAuthenticationRecordRepository.Table
                        where p.CustomerId == customer.Id
                        select p;
            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Delete the external authentication 
        /// </summary>
        /// <param name="externalAuthenticationRecord">External authentication</param>
        public virtual async Task DeleteExternalAuthentication(ExternalAuthentication externalAuthentication)
        {
            if (externalAuthentication == null)
                throw new ArgumentNullException(nameof(externalAuthentication));

            await _externalAuthenticationRecordRepository.DeleteAsync(externalAuthentication);
        }

        #endregion
    }
}