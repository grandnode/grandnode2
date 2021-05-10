using AutoMapper;
using Grand.Domain.Courses;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Courses;

namespace Grand.Web.Admin.Mapper
{
    public class CourseLessonProfile : Profile, IAutoMapperProfile
    {
        public CourseLessonProfile()
        {
            CreateMap<CourseLesson, CourseLessonModel>()
                .ForMember(dest => dest.AvailableSubjects, mo => mo.Ignore());
            CreateMap<CourseLessonModel, CourseLesson>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}