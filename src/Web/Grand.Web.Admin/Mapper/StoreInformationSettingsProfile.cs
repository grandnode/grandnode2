using AutoMapper;
using Grand.Domain.Stores;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class StoreInformationSettingsProfile : Profile, IAutoMapperProfile
    {
        public StoreInformationSettingsProfile()
        {
            CreateMap<StoreInformationSettings, GeneralCommonSettingsModel.StoreInformationSettingsModel>()
                .ForMember(dest => dest.AvailableStoreThemes, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());
            CreateMap<GeneralCommonSettingsModel.StoreInformationSettingsModel, StoreInformationSettings>();
        }

        public int Order => 0;
    }
}