using AutoMapper;
using Grand.Domain.Stores;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Stores;

namespace Grand.Web.Admin.Mapper
{
    public class StoreProfile : Profile, IAutoMapperProfile
    {
        public StoreProfile()
        {
            CreateMap<Store, StoreModel>()
                .ForMember(dest => dest.AvailableLanguages, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableWarehouses, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableCountries, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableCurrencies, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<StoreModel, Store>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}