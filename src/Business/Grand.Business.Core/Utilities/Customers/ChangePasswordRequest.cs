using Grand.Domain.Customers;

namespace Grand.Business.Core.Utilities.Customers
{
    /// <summary>
    /// Change password requst
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// A value indicating whether we should validate password
        /// </summary>
        public bool ValidOldPassword { get; set; }
        /// <summary>
        /// Password format
        /// </summary>
        public PasswordFormat PasswordFormat { get; set; }
        /// <summary>
        /// New password
        /// </summary>
        public string NewPassword { get; set; }
        
        /// <summary>
        /// Old password
        /// </summary>
        public string OldPassword { get; set; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="validOldPassword">A value indicating whether we should validate old password</param>
        /// <param name="newPasswordFormat">Password format</param>
        /// <param name="newPassword">New password</param>
        /// <param name="oldPassword">Old password</param>
        public ChangePasswordRequest(string email, bool validOldPassword, 
            PasswordFormat newPasswordFormat, string newPassword, string oldPassword = "")
        {
            Email = email;
            ValidOldPassword = validOldPassword;
            PasswordFormat = newPasswordFormat;
            NewPassword = newPassword;
            OldPassword = oldPassword;
        }
    }
}
