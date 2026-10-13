using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Common;

public class LoginModel : BaseModel
{
    [DataType(DataType.EmailAddress)]
    [GrandResourceDisplayName("Account.Login.Fields.Email")]
    public string Email { get; set; }

    public bool UsernamesEnabled { get; set; }

    [GrandResourceDisplayName("Account.Login.Fields.UserName")]
    public string Username { get; set; }

    [DataType(DataType.Password)]
    [GrandResourceDisplayName("Account.Login.Fields.Password")]
    public string Password { get; set; }

    [GrandResourceDisplayName("Account.Login.Fields.RememberMe")]
    public bool RememberMe { get; set; }

    public bool DisplayCaptcha { get; set; }
    public ICaptchaValidModel Captcha { get; set; } = new CaptchaModel();

    /// <summary>Set by the server only: the password fields are shown (not in ProviderOnly, except for the Admin break-glass)</summary>
    [BindNever]
    public bool PasswordFormShown { get; set; }

    /// <summary>Set by the server only: ProviderOnly and no provider can sign in</summary>
    [BindNever]
    public bool ExternalSignInUnavailable { get; set; }

    /// <summary>Set by the server only: a registered account is signed in but has no access to this panel</summary>
    [BindNever]
    public bool SignedInWithoutAccess { get; set; }

    public IList<ExternalSignInButton> ExternalSignIn { get; set; } = new List<ExternalSignInButton>();

    public class ExternalSignInButton
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}