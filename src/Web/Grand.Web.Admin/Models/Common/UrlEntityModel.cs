using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Common
{
    public partial class UrlEntityModel : BaseEntityModel
    {
        [GrandResourceDisplayName("admin.configuration.senames.Name")]
        
        public string Name { get; set; }

        [GrandResourceDisplayName("admin.configuration.senames.EntityId")]
        public string EntityId { get; set; }

        [GrandResourceDisplayName("admin.configuration.senames.EntityName")]
        public string EntityName { get; set; }

        [GrandResourceDisplayName("admin.configuration.senames.IsActive")]
        public bool IsActive { get; set; }

        [GrandResourceDisplayName("admin.configuration.senames.Language")]
        public string Language { get; set; }

        [GrandResourceDisplayName("admin.configuration.senames.Details")]
        public string DetailsUrl { get; set; }
    }
}