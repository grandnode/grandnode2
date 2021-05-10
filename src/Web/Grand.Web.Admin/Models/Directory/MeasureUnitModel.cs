using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Directory
{
    public partial class MeasureUnitModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Configuration.Measures.Units.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Measures.Units.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

    }
}