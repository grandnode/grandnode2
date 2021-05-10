using AutoMapper;
using Grand.Domain.Courses;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Courses;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class CourseProfile : Profile, IAutoMapperProfile
    {
        public CourseProfile()
        {
            CreateMap<Course, CourseModel>()
                .ForMember(dest => dest.AvailableLevels, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.ProductName, mo => mo.Ignore());

            CreateMap<CourseModel, Course>()
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToGroups, mo => mo.MapFrom(x => x.CustomerGroups != null && x.CustomerGroups.Any()))
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}