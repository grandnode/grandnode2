using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class UserApiModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.System.UserApi.Email")]
        public string Email { get; set; }

        [GrandResourceDisplayName("Admin.System.UserApi.Password")]
        public string Password { get; set; }

        [GrandResourceDisplayName("Admin.System.UserApi.IsActive")]
        public bool IsActive { get; set; }

    }
}
