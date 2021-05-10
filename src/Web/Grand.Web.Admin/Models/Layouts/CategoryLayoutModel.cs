using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Layouts
{
    public partial class CategoryLayoutModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Configuration.Layouts.Category.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Layouts.Category.ViewPath")]

        public string ViewPath { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Layouts.Category.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}