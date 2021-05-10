using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.ExternalAuthentication
{
    public partial class AuthenticationMethodModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Plugins.ExternalAuthenticationMethods.Fields.FriendlyName")]
        public string FriendlyName { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.ExternalAuthenticationMethods.Fields.SystemName")]
        public string SystemName { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.ExternalAuthenticationMethods.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.ExternalAuthenticationMethods.Fields.IsActive")]
        public bool IsActive { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.ExternalAuthenticationMethods.Fields.Configure")]
        public string ConfigurationUrl { get; set; }
    }
}