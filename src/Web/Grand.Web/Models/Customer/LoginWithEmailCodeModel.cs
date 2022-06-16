using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Customer
{
    public partial class LoginWithEmailCodeModel : BaseModel
    {
        [DataType(DataType.EmailAddress)]
        [GrandResourceDisplayName("Account.LoginWithEmailCode.Email")]
        public string Email { get; set; }
        public string Result { get; set; }
        public bool Send { get; set; }
        public bool DisplayCaptcha { get; set; }
    }
}