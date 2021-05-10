using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Customer
{
    public partial class DeleteAccountModel : BaseModel
    {
        [DataType(DataType.Password)]
        [GrandResourceDisplayName("Account.DeleteAccount.Fields.Password")]
        public string Password { get; set; }

    }
}