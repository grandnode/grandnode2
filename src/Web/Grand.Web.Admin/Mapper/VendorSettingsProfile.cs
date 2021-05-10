using AutoMapper;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class VendorSettingsProfile : Profile, IAutoMapperProfile
    {
        public VendorSettingsProfile()
        {
            CreateMap<VendorSettings, VendorSettingsModel>()
                .ForMember(dest => dest.ActiveStore, mo => mo.Ignore())
                .ForMember(dest => dest.AddressSettings, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());

            CreateMap<VendorSettingsModel, VendorSettings>()
                .ForMember(dest => dest.DefaultVendorPageSizeOptions, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}