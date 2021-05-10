using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Courses
{
    public partial class CourseLevelModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Courses.Level.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Level.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

    }
}
