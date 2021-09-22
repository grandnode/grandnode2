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
                .ForPath(dest => dest.AddressSettings.CityEnabled, mo => mo.MapFrom(p => p.CityEnabled))
                .ForPath(dest => dest.AddressSettings.CityRequired, mo => mo.MapFrom(p => p.CityRequired))
                .ForPath(dest => dest.AddressSettings.CompanyEnabled, mo => mo.MapFrom(p => p.CompanyEnabled))
                .ForPath(dest => dest.AddressSettings.CompanyRequired, mo => mo.MapFrom(p => p.CompanyRequired))
                .ForPath(dest => dest.AddressSettings.CountryEnabled, mo => mo.MapFrom(p => p.CountryEnabled))
                .ForPath(dest => dest.AddressSettings.FaxEnabled, mo => mo.MapFrom(p => p.FaxEnabled))
                .ForPath(dest => dest.AddressSettings.FaxRequired, mo => mo.MapFrom(p => p.FaxRequired))
                .ForPath(dest => dest.AddressSettings.PhoneEnabled, mo => mo.MapFrom(p => p.PhoneEnabled))
                .ForPath(dest => dest.AddressSettings.PhoneRequired, mo => mo.MapFrom(p => p.PhoneRequired))
                .ForPath(dest => dest.AddressSettings.StateProvinceEnabled, mo => mo.MapFrom(p => p.StateProvinceEnabled))
                .ForPath(dest => dest.AddressSettings.StreetAddress2Enabled, mo => mo.MapFrom(p => p.StreetAddress2Enabled))
                .ForPath(dest => dest.AddressSettings.StreetAddress2Required, mo => mo.MapFrom(p => p.StreetAddress2Required))
                .ForPath(dest => dest.AddressSettings.StreetAddressEnabled, mo => mo.MapFrom(p => p.StreetAddressEnabled))
                .ForPath(dest => dest.AddressSettings.StreetAddressRequired, mo => mo.MapFrom(p => p.StreetAddressRequired))
                .ForPath(dest => dest.AddressSettings.ZipPostalCodeEnabled, mo => mo.MapFrom(p => p.ZipPostalCodeEnabled))
                .ForPath(dest => dest.AddressSettings.ZipPostalCodeRequired, mo => mo.MapFrom(p => p.ZipPostalCodeRequired))
                .ForPath(dest => dest.UserFields, mo => mo.Ignore());

            CreateMap<VendorSettingsModel, VendorSettings>()
                .ForPath(dest => dest.CityEnabled, mo => mo.MapFrom(p => p.AddressSettings.CityEnabled))
                .ForPath(dest => dest.CityRequired, mo => mo.MapFrom(p => p.AddressSettings.CityRequired))
                .ForPath(dest => dest.CompanyEnabled, mo => mo.MapFrom(p => p.AddressSettings.CompanyEnabled))
                .ForPath(dest => dest.CompanyRequired, mo => mo.MapFrom(p => p.AddressSettings.CompanyRequired))
                .ForPath(dest => dest.CountryEnabled, mo => mo.MapFrom(p => p.AddressSettings.CountryEnabled))
                .ForPath(dest => dest.FaxEnabled, mo => mo.MapFrom(p => p.AddressSettings.FaxEnabled))
                .ForPath(dest => dest.FaxRequired, mo => mo.MapFrom(p => p.AddressSettings.FaxRequired))
                .ForPath(dest => dest.PhoneEnabled, mo => mo.MapFrom(p => p.AddressSettings.PhoneEnabled))
                .ForPath(dest => dest.PhoneRequired, mo => mo.MapFrom(p => p.AddressSettings.PhoneRequired))
                .ForPath(dest => dest.StateProvinceEnabled, mo => mo.MapFrom(p => p.AddressSettings.StateProvinceEnabled))
                .ForPath(dest => dest.StreetAddress2Enabled, mo => mo.MapFrom(p => p.AddressSettings.StreetAddress2Enabled))
                .ForPath(dest => dest.StreetAddress2Required, mo => mo.MapFrom(p => p.AddressSettings.StreetAddress2Required))
                .ForPath(dest => dest.StreetAddressEnabled, mo => mo.MapFrom(p => p.AddressSettings.StreetAddressEnabled))
                .ForPath(dest => dest.StreetAddressRequired, mo => mo.MapFrom(p => p.AddressSettings.StreetAddressRequired))
                .ForPath(dest => dest.ZipPostalCodeEnabled, mo => mo.MapFrom(p => p.AddressSettings.ZipPostalCodeEnabled))
                .ForPath(dest => dest.ZipPostalCodeRequired, mo => mo.MapFrom(p => p.AddressSettings.ZipPostalCodeRequired));
        }

        public int Order => 0;
    }
}