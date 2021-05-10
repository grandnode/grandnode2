using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Layouts
{
    public partial class ProductLayoutModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Configuration.Layouts.Product.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Layouts.Product.ViewPath")]

        public string ViewPath { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Layouts.Product.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}