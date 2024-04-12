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
    /// <returns>Result</returns>
    Task<CustomerLoginResults> LoginCustomer(string usernameOrEmail, string password);

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
    Task ChangePassword(ChangePasswordRequest request);
}