using AutoMapper;
using Grand.Domain.Courses;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Courses;

namespace Grand.Web.Admin.Mapper
{
    public class CourseSubjectProfile : Profile, IAutoMapperProfile
    {
        public CourseSubjectProfile()
        {
            CreateMap<CourseSubject, CourseSubjectModel>();
            CreateMap<CourseSubjectModel, CourseSubject>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}