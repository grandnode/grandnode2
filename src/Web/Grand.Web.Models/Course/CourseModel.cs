using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Course;

public class CourseModel : BaseEntityModel
{
    public string Name { get; set; }
    public string Level { get; set; }
    public string ShortDescription { get; set; }
    public string Description { get; set; }
    public string MetaKeywords { get; set; }
    public string MetaDescription { get; set; }
    public string MetaTitle { get; set; }
    public string SeName { get; set; }
    public string PictureUrl { get; set; }
    public IList<Subject> Subjects { get; set; } = new List<Subject>();
    public IList<Lesson> Lessons { get; set; } = new List<Lesson>();
    public bool Approved { get; set; }

    public class Subject : BaseEntityModel
    {
        public string Name { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class Lesson : BaseEntityModel
    {
        public string SubjectId { get; set; }
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public int DisplayOrder { get; set; }
        public string PictureUrl { get; set; }
        public bool Approved { get; set; }
    }
}