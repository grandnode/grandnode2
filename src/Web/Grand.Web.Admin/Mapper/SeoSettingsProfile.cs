using AutoMapper;
using Grand.Domain.Seo;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class SeoSettingsProfile : Profile, IAutoMapperProfile
    {
        public SeoSettingsProfile()
        {
            CreateMap<SeoSettings, GeneralCommonSettingsModel.SeoSettingsModel>()
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());
            CreateMap<GeneralCommonSettingsModel.SeoSettingsModel, SeoSettings>();
        }

        public int Order => 0;
    }
}