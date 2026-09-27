using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Common;

public class PasswordRecoveryConfirmModel : BaseModel
{
    public string Email { get; set; }
    public string Token { get; set; }

    [DataType(DataType.Password)]
    [GrandResourceDisplayName("Account.PasswordRecovery.NewPassword")]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [GrandResourceDisplayName("Account.PasswordRecovery.ConfirmNewPassword")]
    public string ConfirmNewPassword { get; set; }

    public bool DisablePasswordChanging { get; set; }
    public bool PasswordChanged { get; set; }
    public string Result { get; set; }
}
