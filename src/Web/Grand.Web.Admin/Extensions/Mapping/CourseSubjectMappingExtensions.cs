using Grand.Infrastructure.Mapper;
using Grand.Domain.Courses;
using Grand.Web.Admin.Models.Courses;

namespace Grand.Web.Admin.Extensions
{
    public static class CourseSubjectMappingExtensions
    {
        public static CourseSubjectModel ToModel(this CourseSubject entity)
        {
            return entity.MapTo<CourseSubject, CourseSubjectModel>();
        }

        public static CourseSubject ToEntity(this CourseSubjectModel model)
        {
            return model.MapTo<CourseSubjectModel, CourseSubject>();
        }

        public static CourseSubject ToEntity(this CourseSubjectModel model, CourseSubject destination)
        {
            return model.MapTo(destination);
        }
    }
}