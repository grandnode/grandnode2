using AutoMapper;
using Grand.Domain.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.Common.Extensions;

namespace Grand.Web.AdminShared.Mapper;

public class StateProvinceProfile : Profile, IAutoMapperProfile
{
    public StateProvinceProfile()
    {
        CreateMap<StateProvince, StateProvinceModel>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore());
        CreateMap<StateProvinceModel, StateProvince>()
            .ForMember(dest => dest.Id, mo => mo.Ignore())
            .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
    }

    public int Order => 0;
}