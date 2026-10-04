using Grand.Domain.Courses;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Courses;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class CourseLessonMappingExtensions
{
    public static CourseLessonModel ToModel(this CourseLesson entity)
    {
        return entity.MapTo<CourseLesson, CourseLessonModel>();
    }

    public static CourseLesson ToEntity(this CourseLessonModel model)
    {
        return model.MapTo<CourseLessonModel, CourseLesson>();
    }

    public static CourseLesson ToEntity(this CourseLessonModel model, CourseLesson destination)
    {
        return model.MapTo(destination);
    }
}