using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Customers
{
    /// <summary>
    /// Customer manager interface
    /// </summary>
    public partial interface ICustomerManagerService
    {
        /// <summary>
        /// Login customer
        /// </summary>
        /// <param name="usernameOrEmail">Username or email</param>
        /// <param name="password">Password</param>
        /// <returns>Result</returns>
        Task<CustomerLoginResults> LoginCustomer(string usernameOrEmail, string password);

        /// <summary>
        /// Login customer with E-mail Code
        /// </summary>
        /// <param name="userId">UserId of the record</param>
        /// <param name="loginCode">loginCode provided in e-mail</param>
        /// <returns>Result</returns>
        Task<CustomerLoginResults> LoginCustomerWithEmailCode(string userId, string loginCode);

        /// <summary>
        /// Register customer
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Result</returns>
        Task<RegistrationResult> RegisterCustomer(RegistrationRequest request);

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Result</returns>
        Task<ChangePasswordResult> ChangePassword(ChangePasswordRequest request);

        /// <summary>
        /// Sets a user email
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="newEmail">New email</param>
        Task SetEmail(Customer customer, string newEmail);

        /// <summary>
        /// Sets a customer username
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="newUsername">New Username</param>
        Task SetUsername(Customer customer, string newUsername);
    }
}