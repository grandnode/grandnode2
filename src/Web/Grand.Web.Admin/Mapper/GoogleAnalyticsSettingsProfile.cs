using AutoMapper;
using Grand.Domain.Seo;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class GoogleAnalyticsSettingsProfile : Profile, IAutoMapperProfile
    {
        public GoogleAnalyticsSettingsProfile()
        {
            CreateMap<GoogleAnalyticsSettings, GeneralCommonSettingsModel.GoogleAnalyticsSettingsModel>()
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());
            CreateMap<GeneralCommonSettingsModel.GoogleAnalyticsSettingsModel, GoogleAnalyticsSettings>();
        }

        public int Order => 0;
    }
}