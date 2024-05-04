using AutoMapper;
using Grand.Domain.Localization;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Localization;

namespace Grand.Web.Admin.Mapper;

public class LanguageProfile : Profile, IAutoMapperProfile
{
    public LanguageProfile()
    {
        CreateMap<Language, LanguageModel>()
            .ForMember(dest => dest.AvailableCurrencies, mo => mo.Ignore())
            .ForMember(dest => dest.FlagFileNames, mo => mo.Ignore());

        CreateMap<LanguageModel, Language>()
            .ForMember(dest => dest.Id, mo => mo.Ignore())
            .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()));
    }

    public int Order => 0;
}