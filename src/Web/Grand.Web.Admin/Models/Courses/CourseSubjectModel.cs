using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Courses
{
    public partial class CourseSubjectModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Courses.Course.Subject.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Subject.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public string CourseId { get; set; }

    }
}
