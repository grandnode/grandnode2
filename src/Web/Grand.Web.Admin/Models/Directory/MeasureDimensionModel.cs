using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Directory
{
    public partial class MeasureDimensionModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Configuration.Measures.Dimensions.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Measures.Dimensions.Fields.SystemKeyword")]

        public string SystemKeyword { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Measures.Dimensions.Fields.Ratio")]
        public double Ratio { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Measures.Dimensions.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Measures.Dimensions.Fields.IsPrimaryDimension")]
        public bool IsPrimaryDimension { get; set; }
    }
}