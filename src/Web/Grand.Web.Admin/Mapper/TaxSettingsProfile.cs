using AutoMapper;
using Grand.Domain.Tax;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Tax;

namespace Grand.Web.Admin.Mapper
{
    public class TaxSettingsProfile : Profile, IAutoMapperProfile
    {
        public TaxSettingsProfile()
        {
            CreateMap<TaxSettings, TaxSettingsModel>()
                .ForMember(dest => dest.DefaultTaxAddress, mo => mo.Ignore())
                .ForMember(dest => dest.TaxDisplayTypeValues, mo => mo.Ignore())
                .ForMember(dest => dest.TaxBasedOnValues, mo => mo.Ignore())                
                .ForMember(dest => dest.TaxCategories, mo => mo.Ignore())
                .ForMember(dest => dest.EuVatShopCountries, mo => mo.Ignore())
                .ForMember(dest => dest.ActiveStore, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());
            CreateMap<TaxSettingsModel, TaxSettings>();
        }

        public int Order => 0;
    }
}