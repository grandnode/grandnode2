using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Layouts
{
    public partial class CollectionLayoutModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Configuration.Layouts.Collection.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Layouts.Collection.ViewPath")]

        public string ViewPath { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Layouts.Collection.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}