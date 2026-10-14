using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Customer;

public class DeleteAccountModel : BaseModel
{
    [DataType(DataType.Password)]
    [GrandResourceDisplayName("Account.DeleteAccount.Fields.Password")]
    public string Password { get; set; }

    /// <summary>False when the identity provider is the only sign-in and no password exists to ask for</summary>
    public bool PasswordRequired { get; set; } = true;
}