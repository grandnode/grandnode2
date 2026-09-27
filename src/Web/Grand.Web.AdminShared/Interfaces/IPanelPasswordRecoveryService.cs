namespace Grand.Web.AdminShared.Interfaces;

/// <summary>
///     Password recovery from the sign-in screen of a panel (Admin, Store, Vendor).
///     Only an account that can sign in to that panel gets a message, and the answer to the request never says
///     whether one does; the link in the message leads back to the same panel.
/// </summary>
public interface IPanelPasswordRecoveryService
{
    /// <summary>
    ///     Generates a recovery token and sends the recovery message, when the email belongs to an account with access
    ///     to the panel; does nothing otherwise.
    /// </summary>
    /// <param name="email">Email entered in the form</param>
    /// <param name="area">Area of the panel: Admin, Store or Vendor</param>
    Task SendRecoveryMessage(string email, string area);

    /// <summary>
    ///     Checks a recovery link
    /// </summary>
    /// <returns>null when the link can be used, otherwise the message to show</returns>
    Task<string> ValidateToken(string email, string token, string area);

    /// <summary>
    ///     Sets a new password through a recovery link and invalidates the link
    /// </summary>
    /// <returns>null when the password has been changed, otherwise the message to show</returns>
    Task<string> ResetPassword(string email, string token, string area, string newPassword);
}
