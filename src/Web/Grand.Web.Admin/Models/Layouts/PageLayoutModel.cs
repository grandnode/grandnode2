using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Layouts
{
    public partial class PageLayoutModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Configuration.Layouts.Page.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Layouts.Page.ViewPath")]

        public string ViewPath { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Layouts.Page.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}