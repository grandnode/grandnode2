using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Layouts
{
    public partial class BrandLayoutModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Configuration.Layouts.Brand.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Layouts.Brand.ViewPath")]

        public string ViewPath { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Layouts.Brand.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}