using Grand.Domain.Customers;

namespace Grand.Business.Core.Utilities.Customers;

/// <summary>
///     Change password request
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="email">Email</param>
    /// <param name="newPasswordFormat">Password format</param>
    /// <param name="newPassword">New password</param>
    /// <param name="oldPassword">Old password</param>
    public ChangePasswordRequest(string email,
        PasswordFormat newPasswordFormat, string newPassword, string oldPassword = "")
    {
        Email = email;
        PasswordFormat = newPasswordFormat;
        NewPassword = newPassword;
        OldPassword = oldPassword;
    }

    /// <summary>
    ///     Email
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    ///     Password format
    /// </summary>
    public PasswordFormat PasswordFormat { get; set; }

    /// <summary>
    ///     New password
    /// </summary>
    public string NewPassword { get; set; }

    /// <summary>
    ///     Old password
    /// </summary>
    public string OldPassword { get; set; }
}