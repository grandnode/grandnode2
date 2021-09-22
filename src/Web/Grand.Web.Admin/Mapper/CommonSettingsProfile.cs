using AutoMapper;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class CommonSettingsProfile : Profile, IAutoMapperProfile
    {
        public CommonSettingsProfile()
        {
            CreateMap<CommonSettings, GeneralCommonSettingsModel.CommonSettingsModel>()
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());
            CreateMap<GeneralCommonSettingsModel.CommonSettingsModel, CommonSettings>();
        }

        public int Order => 0;
    }
}