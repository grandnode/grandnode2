using AutoMapper;
using Grand.Domain.Courses;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Courses;

namespace Grand.Web.Admin.Mapper
{
    public class CourseLevelProfile : Profile, IAutoMapperProfile
    {
        public CourseLevelProfile()
        {
            CreateMap<CourseLevel, CourseLevelModel>();
            CreateMap<CourseLevelModel, CourseLevel>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}