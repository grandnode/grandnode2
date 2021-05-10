using AutoMapper;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
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
}