using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Customers;

/// <summary>
///     Customer manager interface
/// </summary>
public interface ICustomerManagerService
{
    /// <summary>
    ///     Login customer
    /// </summary>
    /// <param name="usernameOrEmail">Username or email</param>
    /// <param name="password">Password</param>
    /// <param name="storeId">Store identifier - used to resolve the customer when per-store customer identity is enabled</param>
    /// <returns>Result</returns>
    Task<CustomerLoginResults> LoginCustomer(string usernameOrEmail, string password, string storeId = "");

    /// <summary>
    ///     Register customer
    /// </summary>
    /// <param name="request">Request</param>
    Task RegisterCustomer(RegistrationRequest request);

    /// <summary>
    ///     Password match
    /// </summary>
    /// <param name="passwordFormat"></param>
    /// <param name="oldPassword"></param>
    /// <param name="newPassword"></param>
    /// <param name="passwordSalt"></param>
    /// <returns></returns>
    bool PasswordMatch(PasswordFormat passwordFormat, string oldPassword, string newPassword, string passwordSalt);

    /// <summary>
    ///     Change password
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="storeId">Store identifier - used to resolve the customer when per-store customer identity is enabled</param>
    Task ChangePassword(ChangePasswordRequest request, string storeId = "");

    /// <summary>
    ///     Change password of this customer, without resolving the account by e-mail
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="passwordFormat">Password format</param>
    /// <param name="newPassword">New password</param>
    /// <remarks>The default body keeps implementations built for an earlier 2.4 release loading without a rebuild</remarks>
    Task ChangePassword(Customer customer, PasswordFormat passwordFormat, string newPassword) =>
        ChangePassword(new ChangePasswordRequest(customer.Email, passwordFormat, newPassword), customer.StoreId);
}