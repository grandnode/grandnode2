using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Directory
{
    public partial class MeasureWeightModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Configuration.Measures.Weights.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Measures.Weights.Fields.SystemKeyword")]
        public string SystemKeyword { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Measures.Weights.Fields.Ratio")]
        public double Ratio { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Measures.Weights.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Measures.Weights.Fields.IsPrimaryWeight")]
        public bool IsPrimaryWeight { get; set; }
    }
}