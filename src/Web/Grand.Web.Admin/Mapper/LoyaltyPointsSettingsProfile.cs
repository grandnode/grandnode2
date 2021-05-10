using AutoMapper;
using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class LoyaltyPointsSettingsProfile : Profile, IAutoMapperProfile
    {
        public LoyaltyPointsSettingsProfile()
        {
            CreateMap<LoyaltyPointsSettings, SalesSettingsModel.LoyaltyPointsSettingsModel>()
                .ForMember(dest => dest.PrimaryStoreCurrencyCode, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());
            CreateMap<SalesSettingsModel.LoyaltyPointsSettingsModel, LoyaltyPointsSettings>();
        }

        public int Order => 0;
    }
}