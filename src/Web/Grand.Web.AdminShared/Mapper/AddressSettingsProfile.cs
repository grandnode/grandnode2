using AutoMapper;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Mapper;

public class AddressSettingsProfile : Profile, IAutoMapperProfile
{
    public AddressSettingsProfile()
    {
        CreateMap<AddressSettings, CustomerSettingsModel.AddressSettingsModel>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());
        CreateMap<CustomerSettingsModel.AddressSettingsModel, AddressSettings>();
    }

    public int Order => 0;
}