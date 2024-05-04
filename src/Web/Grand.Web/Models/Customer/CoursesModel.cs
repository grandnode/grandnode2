using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Customer;

public class CoursesModel : BaseModel
{
    public List<Course> CourseList { get; set; } = new();
    public string CustomerId { get; set; }

    public class Course : BaseEntityModel
    {
        public string Name { get; set; }
        public string SeName { get; set; }
        public string ShortDescription { get; set; }
        public string Level { get; set; }
        public bool Approved { get; set; }
    }
}