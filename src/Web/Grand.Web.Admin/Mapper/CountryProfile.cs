using AutoMapper;
using Grand.Domain.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Directory;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class CountryProfile : Profile, IAutoMapperProfile
    {
        public CountryProfile()
        {
            //countries
            CreateMap<Country, CountryModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.NumberOfStates, mo => mo.MapFrom(src => src.StateProvinces != null ? src.StateProvinces.Count : 0))
                .ForMember(dest => dest.Locales, mo => mo.Ignore());

            CreateMap<CountryModel, Country>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.StateProvinces, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()));
        }

        public int Order => 0;
    }
}
