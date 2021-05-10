using AutoMapper;
using Grand.Domain.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Directory;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class CurrencyProfile : Profile, IAutoMapperProfile
    {
        public CurrencyProfile()
        {
            CreateMap<Currency, CurrencyModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOn, mo => mo.Ignore())
                .ForMember(dest => dest.IsPrimaryExchangeRateCurrency, mo => mo.Ignore())
                .ForMember(dest => dest.IsPrimaryStoreCurrency, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore());

            CreateMap<CurrencyModel, Currency>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}